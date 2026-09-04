using System;
using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Player-facing settings as a plain mutable class. Change fields, then call Save();
    /// values are clamped, persisted to PlayerPrefs, applied to the engine and broadcast.
    /// </summary>
    public class GameSettings
    {
        public const string StorageKey = "unknowntechnology.settings.v2";
        public const float MinimumMouseSensitivity = 0.02f;
        public const float MaximumMouseSensitivity = 1f;
        public const float MinimumGamepadSensitivity = 30f;
        public const float MaximumGamepadSensitivity = 360f;
        public const float MinimumUiScale = 1f;
        public const float MaximumUiScale = 1.5f;

        public int version = 2;
        public float mouseSensitivity = 0.12f;
        public float gamepadSensitivity = 150f;
        public bool invertY;
        public bool toggleInteraction;
        public float uiScale = 1f;
        public bool reducedMotion;
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public float uiVolume = 1f;
        public int qualityLevel = -1;
        public bool fullscreen = true;

        public event Action<GameSettings> Changed;

        public static bool SupportsDisplaySettings => Application.platform != RuntimePlatform.WebGLPlayer;

        public void Save()
        {
            Sanitize();
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
            ApplyToEngine();
            Changed?.Invoke(this);
        }

        public static GameSettings Load()
        {
            if (PlayerPrefs.HasKey(StorageKey))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<GameSettings>(PlayerPrefs.GetString(StorageKey));
                    if (loaded != null && loaded.version == 2)
                    {
                        loaded.Sanitize();
                        loaded.ApplyToEngine();
                        return loaded;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Settings could not be loaded: {exception.Message}");
                }
            }

            var settings = new GameSettings
            {
                qualityLevel = QualitySettings.GetQualityLevel(),
                fullscreen = Screen.fullScreen
            };
            return settings;
        }

        private void Sanitize()
        {
            mouseSensitivity = Mathf.Clamp(mouseSensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity);
            gamepadSensitivity = Mathf.Clamp(gamepadSensitivity, MinimumGamepadSensitivity, MaximumGamepadSensitivity);
            uiScale = Mathf.Clamp(uiScale, MinimumUiScale, MaximumUiScale);
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            uiVolume = Mathf.Clamp01(uiVolume);
            if (qualityLevel < 0)
            {
                qualityLevel = QualitySettings.GetQualityLevel();
            }

            qualityLevel = Mathf.Clamp(qualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            if (!SupportsDisplaySettings)
            {
                fullscreen = true;
            }
        }

        private void ApplyToEngine()
        {
            QualitySettings.SetQualityLevel(qualityLevel, true);
            if (SupportsDisplaySettings)
            {
                Screen.fullScreen = fullscreen;
            }
        }
    }
}
