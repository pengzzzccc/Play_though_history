using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology
{
    /// <summary>
    /// Drives the pause overlay of era scenes: resume, settings (shared template
    /// panel) and save &amp; quit. Also owns the Esc semantics while paused —
    /// close the settings page first, only then resume the game.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        private const string HiddenClass = "hidden";

        private VisualElement pauseOverlay;
        private Button resumeButton;
        private Button settingsButton;
        private Button saveQuitButton;
        private Label deviceMessage;
        private SettingsPanelBinder settingsBinder;
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
            resumeButton = root.Q<Button>("resume-button");
            settingsButton = root.Q<Button>("settings-button");
            saveQuitButton = root.Q<Button>("save-quit-button");
            deviceMessage = root.Q<Label>("device-message");
            settingsBinder = new SettingsPanelBinder(root);
            settingsBinder.BackRequested += CloseSettings;

            pauseOverlay.AddToClassList(HiddenClass);
            bound = true;

            resumeButton.clicked += Resume;
            settingsButton.clicked += OpenSettings;
            saveQuitButton.clicked += SaveAndQuit;
            GameEvents.PhaseChanged += ApplyPhase;
            GameEvents.CancelPressed += HandleCancel;
            GameEvents.DeviceLost += ShowDeviceLost;
            GameEvents.DeviceRegained += ShowDeviceRegained;
            ApplyPhase(Game.Phase);
        }

        private void Unbind()
        {
            if (!bound)
            {
                return;
            }

            resumeButton.clicked -= Resume;
            settingsButton.clicked -= OpenSettings;
            saveQuitButton.clicked -= SaveAndQuit;
            settingsBinder.Unbind();
            settingsBinder.BackRequested -= CloseSettings;
            GameEvents.PhaseChanged -= ApplyPhase;
            GameEvents.CancelPressed -= HandleCancel;
            GameEvents.DeviceLost -= ShowDeviceLost;
            GameEvents.DeviceRegained -= ShowDeviceRegained;
            bound = false;
        }

        private void ApplyPhase(GamePhase phase)
        {
            var paused = phase == GamePhase.Paused;
            pauseOverlay.EnableInClassList(HiddenClass, !paused);
            if (!paused)
            {
                settingsBinder.SetVisible(false);
            }
            else
            {
                pauseOverlay.schedule.Execute(() => resumeButton.Focus());
            }
        }

        private void HandleCancel()
        {
            if (Game.Phase != GamePhase.Paused)
            {
                return;
            }

            if (settingsBinder.IsVisible)
            {
                CloseSettings();
            }
            else
            {
                Resume();
            }
        }

        private void Resume()
        {
            Game.TryResume();
        }

        private void OpenSettings()
        {
            settingsBinder.SetVisible(true);
        }

        private void CloseSettings()
        {
            settingsBinder.SetVisible(false);
            settingsButton.Focus();
        }

        private void SaveAndQuit()
        {
            // Save slot persistence arrives with the save system; until then this
            // simply returns to the main menu.
            Game.LoadScene("MainMenu", GamePhase.MainMenu);
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
