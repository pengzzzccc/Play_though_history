using System;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology.Presentation
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseMenuController : MonoBehaviour
    {
        private const string HiddenClass = "hidden";
        private const float MinimumUiScale = 1f;
        private const float MaximumUiScale = 1.5f;

        private VisualElement pauseOverlay;
        private VisualElement settingsPanel;
        private Button resumeButton;
        private Button settingsButton;
        private Button settingsBackButton;
        private Slider mouseSensitivity;
        private Slider gamepadSensitivity;
        private Slider uiScale;
        private Toggle invertY;
        private Toggle reducedMotion;
        private Toggle fullscreen;
        private Label deviceMessage;

        private GameContext context;
        private IDisposable phaseSubscription;
        private IDisposable deviceLostSubscription;
        private IDisposable deviceRegainedSubscription;
        private bool bound;

        private void Start()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Bind()
        {
            if (bound)
            {
                return;
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null)
            {
                return;
            }

            pauseOverlay = root.Q<VisualElement>("pause-overlay");
            settingsPanel = root.Q<VisualElement>("settings-panel");
            resumeButton = root.Q<Button>("resume-button");
            settingsButton = root.Q<Button>("settings-button");
            settingsBackButton = root.Q<Button>("settings-back-button");
            mouseSensitivity = root.Q<Slider>("mouse-sensitivity");
            gamepadSensitivity = root.Q<Slider>("gamepad-sensitivity");
            uiScale = root.Q<Slider>("ui-scale");
            invertY = root.Q<Toggle>("invert-y-toggle");
            reducedMotion = root.Q<Toggle>("reduced-motion-toggle");
            fullscreen = root.Q<Toggle>("fullscreen-toggle");
            deviceMessage = root.Q<Label>("device-message");

            pauseOverlay.AddToClassList(HiddenClass);
            settingsPanel.AddToClassList(HiddenClass);
            if (!GameContextProvider.IsReady)
            {
                return;
            }

            bound = true;
            context = GameContextProvider.Current;
            ConfigureRanges();
            RefreshControls(context.Settings.Current);
            resumeButton.clicked += Resume;
            settingsButton.clicked += OpenSettings;
            settingsBackButton.clicked += CloseSettings;
            mouseSensitivity.RegisterValueChangedCallback(OnSliderChanged);
            gamepadSensitivity.RegisterValueChangedCallback(OnSliderChanged);
            uiScale.RegisterValueChangedCallback(OnSliderChanged);
            invertY.RegisterValueChangedCallback(OnToggleChanged);
            reducedMotion.RegisterValueChangedCallback(OnToggleChanged);
            fullscreen.RegisterValueChangedCallback(OnToggleChanged);
            phaseSubscription = context.EventBus.Subscribe<GamePhaseChanged>(message => ApplyPhase(message.Current));
            deviceLostSubscription = context.EventBus.Subscribe<InputDeviceLost>(message => deviceMessage.text = $"{message.DisplayName} disconnected. Reconnect it or use keyboard and mouse.");
            deviceRegainedSubscription = context.EventBus.Subscribe<InputDeviceRegained>(message => deviceMessage.text = $"{message.DisplayName} connected.");
            ApplyPhase(context.State.Current.Phase);
        }

        private void Unbind()
        {
            if (!bound)
            {
                return;
            }

            resumeButton.clicked -= Resume;
            settingsButton.clicked -= OpenSettings;
            settingsBackButton.clicked -= CloseSettings;
            mouseSensitivity.UnregisterValueChangedCallback(OnSliderChanged);
            gamepadSensitivity.UnregisterValueChangedCallback(OnSliderChanged);
            uiScale.UnregisterValueChangedCallback(OnSliderChanged);
            invertY.UnregisterValueChangedCallback(OnToggleChanged);
            reducedMotion.UnregisterValueChangedCallback(OnToggleChanged);
            fullscreen.UnregisterValueChangedCallback(OnToggleChanged);
            phaseSubscription?.Dispose();
            deviceLostSubscription?.Dispose();
            deviceRegainedSubscription?.Dispose();
            phaseSubscription = null;
            deviceLostSubscription = null;
            deviceRegainedSubscription = null;
            bound = false;
        }

        private void ConfigureRanges()
        {
            mouseSensitivity.lowValue = GameSettingsSnapshot.MinimumMouseSensitivity;
            mouseSensitivity.highValue = GameSettingsSnapshot.MaximumMouseSensitivity;
            gamepadSensitivity.lowValue = GameSettingsSnapshot.MinimumGamepadSensitivity;
            gamepadSensitivity.highValue = GameSettingsSnapshot.MaximumGamepadSensitivity;
            uiScale.lowValue = MinimumUiScale;
            uiScale.highValue = MaximumUiScale;
            fullscreen.EnableInClassList(HiddenClass, !context.Settings.SupportsDisplaySettings);
        }

        private void RefreshControls(GameSettingsSnapshot settings)
        {
            mouseSensitivity.SetValueWithoutNotify(settings.MouseSensitivity);
            gamepadSensitivity.SetValueWithoutNotify(settings.GamepadSensitivity);
            uiScale.SetValueWithoutNotify(settings.UiScale);
            invertY.SetValueWithoutNotify(settings.InvertY);
            reducedMotion.SetValueWithoutNotify(settings.ReducedMotion);
            fullscreen.SetValueWithoutNotify(settings.Fullscreen);
        }

        private void OnSliderChanged(ChangeEvent<float> evt)
        {
            ApplySettings();
        }

        private void OnToggleChanged(ChangeEvent<bool> evt)
        {
            ApplySettings();
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
                invertY.value,
                current.InteractionMode,
                uiScale.value,
                reducedMotion.value,
                current.MasterVolume,
                current.MusicVolume,
                current.SfxVolume,
                current.UiVolume,
                current.QualityLevel,
                fullscreen.value));
        }

        private void ApplyPhase(GamePhase phase)
        {
            var paused = phase == GamePhase.Paused;
            pauseOverlay.EnableInClassList(HiddenClass, !paused);
            if (!paused)
            {
                settingsPanel.EnableInClassList(HiddenClass, true);
            }
            else
            {
                pauseOverlay.schedule.Execute(() => resumeButton.Focus());
            }
        }

        private void Resume()
        {
            context.EventBus.Publish(new ResumeRequested());
        }

        private void OpenSettings()
        {
            settingsPanel.EnableInClassList(HiddenClass, false);
            settingsPanel.schedule.Execute(() => mouseSensitivity.Focus());
        }

        private void CloseSettings()
        {
            settingsPanel.EnableInClassList(HiddenClass, true);
            settingsButton.Focus();
        }
    }
}
