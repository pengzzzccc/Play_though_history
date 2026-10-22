using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownTechnology
{
    [Serializable]
    public class SaveData
    {
        public const int SchemaVersion = 1;

        public int version = SchemaVersion;
        public int slot;
        public string createdAt;
        public string savedAt;
        public float playTimeSeconds;
        public string currentScene;
        public PlayerSaveState player = new PlayerSaveState();
        public List<SceneSaveState> scenes = new List<SceneSaveState>();

        public SceneSaveState FindScene(string sceneName)
        {
            return scenes.Find(scene => scene.sceneName == sceneName);
        }
    }

    [Serializable]
    public class PlayerSaveState
    {
        public Vector3 position;
        public float yaw;
        public float pitch;
    }

    [Serializable]
    public class SceneSaveState
    {
        public string sceneName;
        public List<ItemSaveState> items = new List<ItemSaveState>();
        public List<TransformSaveState> gates = new List<TransformSaveState>();
    }

    [Serializable]
    public class ItemSaveState
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public string slotId;
    }

    [Serializable]
    public class TransformSaveState
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
    }
}
