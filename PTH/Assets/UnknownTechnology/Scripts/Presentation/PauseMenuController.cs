using System;
using TMPro;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;
using UnityEngine;
using UnityEngine.UI;

namespace UnknownTechnology.Presentation
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Slider mouseSensitivity;
        [SerializeField] private Slider gamepadSensitivity;
        [SerializeField] private Slider uiScale;
        [SerializeField] private Toggle invertY;
        [SerializeField] private Toggle reducedMotion;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private TMP_Text deviceMessage;

        private GameContext context;
        private IDisposable phaseSubscription;
        private IDisposable deviceLostSubscription;
        private IDisposable deviceRegainedSubscription;

        private void Start()
        {
            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
            if (!GameContextProvider.IsReady)
            {
                enabled = false;
                return;
            }

            context = GameContextProvider.Current;
            resumeButton.onClick.AddListener(Resume);
            settingsButton.onClick.AddListener(OpenSettings);
            settingsBackButton.onClick.AddListener(CloseSettings);
            mouseSensitivity.onValueChanged.AddListener(_ => ApplySettings());
            gamepadSensitivity.onValueChanged.AddListener(_ => ApplySettings());
            uiScale.onValueChanged.AddListener(_ => ApplySettings());
            invertY.onValueChanged.AddListener(_ => ApplySettings());
            reducedMotion.onValueChanged.AddListener(_ => ApplySettings());
            fullscreen.onValueChanged.AddListener(_ => ApplySettings());

            ConfigureRanges();
            RefreshControls(context.Settings.Current);
            phaseSubscription = context.EventBus.Subscribe<GamePhaseChanged>(message => ApplyPhase(message.Current));
            deviceLostSubscription = context.EventBus.Subscribe<InputDeviceLost>(message => deviceMessage.text = $"{message.DisplayName} disconnected. Reconnect it or use keyboard and mouse.");
            deviceRegainedSubscription = context.EventBus.Subscribe<InputDeviceRegained>(message => deviceMessage.text = $"{message.DisplayName} connected.");
            ApplyPhase(context.State.Current.Phase);
        }

        private void ConfigureRanges()
        {
            mouseSensitivity.minValue = GameSettingsSnapshot.MinimumMouseSensitivity;
            mouseSensitivity.maxValue = GameSettingsSnapshot.MaximumMouseSensitivity;
            gamepadSensitivity.minValue = GameSettingsSnapshot.MinimumGamepadSensitivity;
            gamepadSensitivity.maxValue = GameSettingsSnapshot.MaximumGamepadSensitivity;
            uiScale.minValue = 1f;
            uiScale.maxValue = 1.5f;
            fullscreen.gameObject.SetActive(context.Settings.SupportsDisplaySettings);
        }

        private void RefreshControls(GameSettingsSnapshot settings)
        {
            mouseSensitivity.SetValueWithoutNotify(settings.MouseSensitivity);
            gamepadSensitivity.SetValueWithoutNotify(settings.GamepadSensitivity);
            uiScale.SetValueWithoutNotify(settings.UiScale);
            invertY.SetIsOnWithoutNotify(settings.InvertY);
            reducedMotion.SetIsOnWithoutNotify(settings.ReducedMotion);
            fullscreen.SetIsOnWithoutNotify(settings.Fullscreen);
        }

        private void ApplySettings()
        {
            if (context == null)
            {
                return;
            }

            var current = context.Settings.Current;
            context.Settings.Apply(new GameSettingsSnapshot(
                mouseSensitivity.value,
                gamepadSensitivity.value,
                invertY.isOn,
                current.InteractionMode,
                uiScale.value,
                reducedMotion.isOn,
                current.MasterVolume,
                current.MusicVolume,
                current.SfxVolume,
                current.UiVolume,
                current.QualityLevel,
                fullscreen.isOn));
        }

        private void ApplyPhase(GamePhase phase)
        {
            var paused = phase == GamePhase.Paused;
            pausePanel.SetActive(paused);
            if (!paused)
            {
                settingsPanel.SetActive(false);
            }
            else
            {
                resumeButton.Select();
            }
        }

        private void Resume()
        {
            context.EventBus.Publish(new ResumeRequested());
        }

        private void OpenSettings()
        {
            settingsPanel.SetActive(true);
            mouseSensitivity.Select();
        }

        private void CloseSettings()
        {
            settingsPanel.SetActive(false);
            settingsButton.Select();
        }

        private void OnDestroy()
        {
            phaseSubscription?.Dispose();
            deviceLostSubscription?.Dispose();
            deviceRegainedSubscription?.Dispose();
        }

#if UNITY_EDITOR
        public void Configure(
            GameObject configuredPausePanel,
            GameObject configuredSettingsPanel,
            Button configuredResumeButton,
            Button configuredSettingsButton,
            Button configuredSettingsBackButton,
            Slider configuredMouseSensitivity,
            Slider configuredGamepadSensitivity,
            Slider configuredUiScale,
            Toggle configuredInvertY,
            Toggle configuredReducedMotion,
            Toggle configuredFullscreen,
            TMP_Text configuredDeviceMessage)
        {
            pausePanel = configuredPausePanel;
            settingsPanel = configuredSettingsPanel;
            resumeButton = configuredResumeButton;
            settingsButton = configuredSettingsButton;
            settingsBackButton = configuredSettingsBackButton;
            mouseSensitivity = configuredMouseSensitivity;
            gamepadSensitivity = configuredGamepadSensitivity;
            uiScale = configuredUiScale;
            invertY = configuredInvertY;
            reducedMotion = configuredReducedMotion;
            fullscreen = configuredFullscreen;
            deviceMessage = configuredDeviceMessage;
        }
#endif
    }
}
