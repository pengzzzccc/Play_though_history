using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnknownTechnology
{

    public static class SaveSystem
    {
        public const int SlotCount = 3;
        public const string FirstScene = "Era_Ancient";
        private const string SaveableScenePrefix = "Era_";

        private static PlayerSaveState pendingPlayer;

        public static SaveData Current { get; private set; }
        public static int ActiveSlot => Current != null ? Current.slot : 0;
        public static bool HasSession => Current != null;
        public static bool IsRestoring { get; private set; }

        public static string PathFor(int slot) => Path.Combine(GameData.SavesPath, $"slot_{slot}.json");

        public static bool IsSaveableScene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && sceneName.StartsWith(SaveableScenePrefix, StringComparison.Ordinal);
        }

        
        public static SaveData ReadSummary(int slot)
        {
            var path = PathFor(slot);
            if (!GameData.TryReadText(path, out var json))
            {
                return null;
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data != null && data.version == SaveData.SchemaVersion)
                {
                    data.slot = slot;
                    return data;
                }

                Debug.LogWarning($"Save slot {slot} has an unsupported version and was set aside.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save slot {slot} could not be parsed: {exception.Message}");
            }

            GameData.TryMove(path, Path.Combine(GameData.SavesPath, $"slot_{slot}.corrupt.json"));
            return null;
        }

        public static void StartNewGame(int slot)
        {
            var now = Timestamp();
            Current = new SaveData
            {
                slot = slot,
                createdAt = now,
                savedAt = now,
                currentScene = FirstScene
            };
            pendingPlayer = null;
        }

        public static bool Continue(int slot)
        {
            var data = ReadSummary(slot);
            if (data == null)
            {
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(data.currentScene))
            {
                Debug.LogWarning($"Saved scene '{data.currentScene}' is not in Build Settings; starting at {FirstScene}.");
                data.currentScene = FirstScene;
                data.player = null;
            }

            Current = data;
            pendingPlayer = data.player;
            return true;
        }

        public static void EndSession()
        {
            Current = null;
            pendingPlayer = null;
            IsRestoring = false;
        }

        public static bool Delete(int slot)
        {
            if (ActiveSlot == slot)
            {
                EndSession();
            }

            return GameData.TryDelete(PathFor(slot));
        }

        public static void AddPlayTime(float seconds)
        {
            if (Current != null)
            {
                Current.playTimeSeconds += seconds;
            }
        }

        public static void CaptureActiveScene()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (Current == null || !IsSaveableScene(sceneName))
            {
                return;
            }

            var state = Current.FindScene(sceneName);
            if (state == null)
            {
                state = new SceneSaveState { sceneName = sceneName };
                Current.scenes.Add(state);
            }

            state.items.Clear();
            foreach (var item in Object.FindObjectsByType<CarryableItem>())
            {
                var itemTransform = item.transform;
                state.items.Add(new ItemSaveState
                {
                    id = item.SaveId,
                    position = itemTransform.position,
                    rotation = itemTransform.rotation,
                    slotId = item.CurrentSlot != null ? item.CurrentSlot.SaveId : string.Empty
                });
            }

            state.gates.Clear();
            foreach (var gate in Object.FindObjectsByType<TriggerGate>())
            {
                var gateTransform = gate.transform;
                state.gates.Add(new TransformSaveState
                {
                    id = gate.SaveId,
                    position = gateTransform.position,
                    rotation = gateTransform.rotation
                });
            }

            Current.currentScene = sceneName;
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            if (player != null)
            {
                Current.player ??= new PlayerSaveState();
                Current.player.position = player.transform.position;
                Current.player.yaw = player.transform.eulerAngles.y;
                Current.player.pitch = player.CameraPitch;
            }
        }

        public static bool SaveNow()
        {
            if (Current == null)
            {
                Debug.Log("SaveSystem: no active save slot, nothing was saved.");
                return false;
            }

            CaptureActiveScene();
            Current.version = SaveData.SchemaVersion;
            Current.savedAt = Timestamp();
            return GameData.TryWriteTextAtomic(PathFor(Current.slot), JsonUtility.ToJson(Current, true));
        }

        public static void RestoreActiveScene()
        {
            if (Current == null)
            {
                return;
            }

            var sceneName = SceneManager.GetActiveScene().name;
            IsRestoring = true;
            try
            {
                var state = Current.FindScene(sceneName);
                if (state != null)
                {
                    var gates = IndexBySaveId(Object.FindObjectsByType<TriggerGate>(), gate => gate.SaveId);
                    foreach (var record in state.gates)
                    {
                        if (gates.TryGetValue(record.id, out var gate))
                        {
                            gate.RestorePose(record.position, record.rotation);
                        }
                    }

                    var slots = IndexBySaveId(Object.FindObjectsByType<ItemSlot>(), slot => slot.SaveId);
                    var items = IndexBySaveId(Object.FindObjectsByType<CarryableItem>(), item => item.SaveId);
                    foreach (var record in state.items)
                    {
                        if (!items.TryGetValue(record.id, out var item))
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.slotId) && slots.TryGetValue(record.slotId, out var slot) && slot.Place(item))
                        {
                            continue;
                        }

                        item.RestoreFree(record.position, record.rotation);
                    }
                }

                if (pendingPlayer != null && sceneName == Current.currentScene)
                {
                    var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
                    if (player != null)
                    {
                        player.ApplySavedPose(pendingPlayer.position, pendingPlayer.yaw, pendingPlayer.pitch);
                    }
                }

                pendingPlayer = null;
            }
            finally
            {
                IsRestoring = false;
            }
        }

        public static string FormatSavedAt(SaveData data)
        {
            return DateTime.TryParse(data.savedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var savedAt)
                ? savedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : "Unknown time";
        }

        private static string Timestamp()
        {
            return DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, T> IndexBySaveId<T>(T[] objects, Func<T, string> getId) where T : Object
        {
            var index = new Dictionary<string, T>();
            foreach (var target in objects)
            {
                var id = getId(target);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!index.TryAdd(id, target))
                {
                    Debug.LogWarning($"SaveSystem: duplicate save id '{id}'; only the first object is restored.", target);
                }
            }

            return index;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            EndSession();
        }
    }
}
