using System;
using UnknownTechnology.Core.Events;
using UnityEngine;

namespace UnknownTechnology.Core.Settings
{
    public interface ISettingsStore
    {
        bool TryLoad(out GameSettingsSnapshot settings);
        void Save(GameSettingsSnapshot settings);
    }

    public interface ISettingsService
    {
        GameSettingsSnapshot Current { get; }
        bool SupportsDisplaySettings { get; }
        void Apply(GameSettingsSnapshot settings);
        void ResetToDefaults();
    }

    public sealed class SettingsService : ISettingsService
    {
        private readonly ISettingsStore store;
        private readonly IEventBus eventBus;
        private readonly bool applyPlatformSettings;

        public SettingsService(ISettingsStore store, IEventBus eventBus, bool applyPlatformSettings = true)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.applyPlatformSettings = applyPlatformSettings;
            Current = store.TryLoad(out var loaded) ? ClampQuality(loaded) : ClampQuality(GameSettingsSnapshot.Defaults);
            ApplyPlatformValues(Current);
        }

        public GameSettingsSnapshot Current { get; private set; }

        public bool SupportsDisplaySettings => Application.platform != RuntimePlatform.WebGLPlayer;

        public void Apply(GameSettingsSnapshot settings)
        {
            Current = ClampQuality(settings);
            ApplyPlatformValues(Current);
            store.Save(Current);
            eventBus.Publish(new SettingsChanged(Current));
        }

        public void ResetToDefaults()
        {
            Apply(GameSettingsSnapshot.Defaults);
        }

        private static GameSettingsSnapshot ClampQuality(GameSettingsSnapshot settings)
        {
            var maximumQuality = Mathf.Max(0, QualitySettings.names.Length - 1);
            return new GameSettingsSnapshot(
                settings.MouseSensitivity,
                settings.GamepadSensitivity,
                settings.InvertY,
                settings.InteractionMode,
                settings.UiScale,
                settings.ReducedMotion,
                settings.MasterVolume,
                settings.MusicVolume,
                settings.SfxVolume,
                settings.UiVolume,
                Mathf.Clamp(settings.QualityLevel, 0, maximumQuality),
                settings.Fullscreen);
        }

        private void ApplyPlatformValues(GameSettingsSnapshot settings)
        {
            if (!applyPlatformSettings)
            {
                return;
            }

            QualitySettings.SetQualityLevel(settings.QualityLevel, true);
            if (SupportsDisplaySettings)
            {
                Screen.fullScreen = settings.Fullscreen;
            }
        }
    }

    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public const string StorageKey = "unknowntechnology.settings.v1";

        public bool TryLoad(out GameSettingsSnapshot settings)
        {
            if (!PlayerPrefs.HasKey(StorageKey))
            {
                settings = default;
                return false;
            }

            try
            {
                var data = JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(StorageKey));
                if (data == null || data.schemaVersion != 1)
                {
                    settings = default;
                    return false;
                }

                settings = data.ToSnapshot();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Settings could not be loaded: {exception.Message}");
                settings = default;
                return false;
            }
        }

        public void Save(GameSettingsSnapshot settings)
        {
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(new SettingsData(settings)));
            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class SettingsData
        {
            public int schemaVersion = 1;
            public float mouseSensitivity;
            public float gamepadSensitivity;
            public bool invertY;
            public int interactionMode;
            public float uiScale;
            public bool reducedMotion;
            public float masterVolume;
            public float musicVolume;
            public float sfxVolume;
            public float uiVolume;
            public int qualityLevel;
            public bool fullscreen;

            public SettingsData()
            {
            }

            public SettingsData(GameSettingsSnapshot snapshot)
            {
                mouseSensitivity = snapshot.MouseSensitivity;
                gamepadSensitivity = snapshot.GamepadSensitivity;
                invertY = snapshot.InvertY;
                interactionMode = (int)snapshot.InteractionMode;
                uiScale = snapshot.UiScale;
                reducedMotion = snapshot.ReducedMotion;
                masterVolume = snapshot.MasterVolume;
                musicVolume = snapshot.MusicVolume;
                sfxVolume = snapshot.SfxVolume;
                uiVolume = snapshot.UiVolume;
                qualityLevel = snapshot.QualityLevel;
                fullscreen = snapshot.Fullscreen;
            }

            public GameSettingsSnapshot ToSnapshot()
            {
                var parsedMode = Enum.IsDefined(typeof(InteractionMode), interactionMode)
                    ? (InteractionMode)interactionMode
                    : InteractionMode.Hold;
                return new GameSettingsSnapshot(
                    mouseSensitivity,
                    gamepadSensitivity,
                    invertY,
                    parsedMode,
                    uiScale,
                    reducedMotion,
                    masterVolume,
                    musicVolume,
                    sfxVolume,
                    uiVolume,
                    qualityLevel,
                    fullscreen);
            }
        }
    }
}
