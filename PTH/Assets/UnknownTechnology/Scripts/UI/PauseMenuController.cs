using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology
{
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        private const string HiddenClass = "hidden";

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
            bound = true;

            mouseSensitivity.lowValue = GameSettings.MinimumMouseSensitivity;
            mouseSensitivity.highValue = GameSettings.MaximumMouseSensitivity;
            gamepadSensitivity.lowValue = GameSettings.MinimumGamepadSensitivity;
            gamepadSensitivity.highValue = GameSettings.MaximumGamepadSensitivity;
            uiScale.lowValue = GameSettings.MinimumUiScale;
            uiScale.highValue = GameSettings.MaximumUiScale;
            fullscreen.EnableInClassList(HiddenClass, !GameSettings.SupportsDisplaySettings);
            RefreshControls(Game.Settings);

            resumeButton.clicked += Game.TryResume;
            settingsButton.clicked += OpenSettings;
            settingsBackButton.clicked += CloseSettings;
            mouseSensitivity.RegisterValueChangedCallback(OnControlChanged);
            gamepadSensitivity.RegisterValueChangedCallback(OnControlChanged);
            uiScale.RegisterValueChangedCallback(OnControlChanged);
            invertY.RegisterValueChangedCallback(OnToggleChanged);
            reducedMotion.RegisterValueChangedCallback(OnToggleChanged);
            fullscreen.RegisterValueChangedCallback(OnToggleChanged);
            Game.PhaseChanged += ApplyPhase;
            GameBootstrap.DeviceLost += ShowDeviceLost;
            GameBootstrap.DeviceRegained += ShowDeviceRegained;
            ApplyPhase(Game.Phase);
        }

        private void Unbind()
        {
            if (!bound)
            {
                return;
            }

            resumeButton.clicked -= Game.TryResume;
            settingsButton.clicked -= OpenSettings;
            settingsBackButton.clicked -= CloseSettings;
            mouseSensitivity.UnregisterValueChangedCallback(OnControlChanged);
            gamepadSensitivity.UnregisterValueChangedCallback(OnControlChanged);
            uiScale.UnregisterValueChangedCallback(OnControlChanged);
            invertY.UnregisterValueChangedCallback(OnToggleChanged);
            reducedMotion.UnregisterValueChangedCallback(OnToggleChanged);
            fullscreen.UnregisterValueChangedCallback(OnToggleChanged);
            Game.PhaseChanged -= ApplyPhase;
            GameBootstrap.DeviceLost -= ShowDeviceLost;
            GameBootstrap.DeviceRegained -= ShowDeviceRegained;
            bound = false;
        }

        private void RefreshControls(GameSettings settings)
        {
            mouseSensitivity.SetValueWithoutNotify(settings.mouseSensitivity);
            gamepadSensitivity.SetValueWithoutNotify(settings.gamepadSensitivity);
            uiScale.SetValueWithoutNotify(settings.uiScale);
            invertY.SetValueWithoutNotify(settings.invertY);
            reducedMotion.SetValueWithoutNotify(settings.reducedMotion);
            fullscreen.SetValueWithoutNotify(settings.fullscreen);
        }

        private void OnControlChanged(ChangeEvent<float> evt)
        {
            ApplySettings();
        }

        private void OnToggleChanged(ChangeEvent<bool> evt)
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            var settings = Game.Settings;
            settings.mouseSensitivity = mouseSensitivity.value;
            settings.gamepadSensitivity = gamepadSensitivity.value;
            settings.invertY = invertY.value;
            settings.uiScale = uiScale.value;
            settings.reducedMotion = reducedMotion.value;
            settings.fullscreen = fullscreen.value;
            settings.Save();
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

        private void ShowDeviceLost(string displayName)
        {
            deviceMessage.text = $"{displayName} disconnected. Reconnect it or use keyboard and mouse.";
        }

        private void ShowDeviceRegained(string displayName)
        {
            deviceMessage.text = $"{displayName} connected.";
        }
    }
}
