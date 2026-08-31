using UnityEngine;

namespace UnknownTechnology.Core.Settings
{
    public enum InteractionMode
    {
        Hold = 0,
        Toggle = 1
    }

    public readonly struct GameSettingsSnapshot
    {
        public const float MinimumMouseSensitivity = 0.02f;
        public const float MaximumMouseSensitivity = 1f;
        public const float MinimumGamepadSensitivity = 30f;
        public const float MaximumGamepadSensitivity = 360f;

        public GameSettingsSnapshot(
            float mouseSensitivity,
            float gamepadSensitivity,
            bool invertY,
            InteractionMode interactionMode,
            float uiScale,
            bool reducedMotion,
            float masterVolume,
            float musicVolume,
            float sfxVolume,
            float uiVolume,
            int qualityLevel,
            bool fullscreen)
        {
            MouseSensitivity = Mathf.Clamp(mouseSensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity);
            GamepadSensitivity = Mathf.Clamp(gamepadSensitivity, MinimumGamepadSensitivity, MaximumGamepadSensitivity);
            InvertY = invertY;
            InteractionMode = interactionMode;
            UiScale = Mathf.Clamp(uiScale, 1f, 1.5f);
            ReducedMotion = reducedMotion;
            MasterVolume = Mathf.Clamp01(masterVolume);
            MusicVolume = Mathf.Clamp01(musicVolume);
            SfxVolume = Mathf.Clamp01(sfxVolume);
            UiVolume = Mathf.Clamp01(uiVolume);
            QualityLevel = Mathf.Max(0, qualityLevel);
            Fullscreen = fullscreen;
        }

        public float MouseSensitivity { get; }
        public float GamepadSensitivity { get; }
        public bool InvertY { get; }
        public InteractionMode InteractionMode { get; }
        public float UiScale { get; }
        public bool ReducedMotion { get; }
        public float MasterVolume { get; }
        public float MusicVolume { get; }
        public float SfxVolume { get; }
        public float UiVolume { get; }
        public int QualityLevel { get; }
        public bool Fullscreen { get; }

        public static GameSettingsSnapshot Defaults => new(
            0.12f,
            150f,
            false,
            InteractionMode.Hold,
            1f,
            false,
            1f,
            0.8f,
            1f,
            1f,
            QualitySettings.GetQualityLevel(),
            Screen.fullScreen);
    }
}
