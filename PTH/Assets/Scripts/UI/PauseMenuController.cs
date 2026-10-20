
using UnityEngine;
using UnityEngine.UIElements;
using UnknownTechnology.Audio;

namespace UnknownTechnology
{
    /// <summary>
    /// Controls the UI Toolkit pause menu and its settings panel.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        private const string HiddenClass = "hidden";

        private AudioManager audioManager;

        // UI Toolkit pause menu references
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
                return;

            var root = GetComponent<UIDocument>().rootVisualElement;

            if (root == null)
                return;

            pauseOverlay = root.Q<VisualElement>("pause-overlay");
            resumeButton = root.Q<Button>("resume-button");
            settingsButton = root.Q<Button>("settings-button");
            saveQuitButton = root.Q<Button>("save-quit-button");
            deviceMessage = root.Q<Label>("device-message");

            settingsBinder = new SettingsPanelBinder(root);
            settingsBinder.BackRequested += CloseSettings;

            audioManager = FindAnyObjectByType<AudioManager>();

            if (pauseOverlay != null)
                pauseOverlay.AddToClassList(HiddenClass);

            bound = true;

            if (resumeButton != null)
                resumeButton.clicked += Resume;

            if (settingsButton != null)
                settingsButton.clicked += OpenSettings;

            if (saveQuitButton != null)
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
                return;

            if (resumeButton != null)
                resumeButton.clicked -= Resume;

            if (settingsButton != null)
                settingsButton.clicked -= OpenSettings;

            if (saveQuitButton != null)
                saveQuitButton.clicked -= SaveAndQuit;

            if (settingsBinder != null)
            {
                settingsBinder.Unbind();
                settingsBinder.BackRequested -= CloseSettings;
            }

            GameEvents.PhaseChanged -= ApplyPhase;
            GameEvents.CancelPressed -= HandleCancel;
            GameEvents.DeviceLost -= ShowDeviceLost;
            GameEvents.DeviceRegained -= ShowDeviceRegained;

            bound = false;
        }

        private void ApplyPhase(GamePhase phase)
        {
            bool paused = phase == GamePhase.Paused;

            if (pauseOverlay != null)
                pauseOverlay.EnableInClassList(HiddenClass, !paused);

            if (!paused)
            {
                if (settingsBinder != null)
                    settingsBinder.SetVisible(false);
            }
            else if (resumeButton != null)
            {
                pauseOverlay.schedule.Execute(() => resumeButton.Focus());
            }
        }

        private void HandleCancel()
        {
            if (Game.Phase != GamePhase.Paused)
                return;

            if (settingsBinder != null && settingsBinder.IsVisible)
                CloseSettings();
            else
                Resume();
        }

        private void PlayClickSound()
        {
            if (audioManager != null)
                audioManager.PlayButtonClickSound();
        }

        private void Resume()
        {
            PlayClickSound();
            Game.TryResume();
        }

        private void OpenSettings()
        {
            PlayClickSound();

            if (settingsBinder != null)
                settingsBinder.SetVisible(true);
        }

        private void CloseSettings()
        {
            PlayClickSound();

            if (settingsBinder != null)
                settingsBinder.SetVisible(false);

            if (settingsButton != null)
                settingsButton.Focus();
        }

        private void SaveAndQuit()
        {
            PlayClickSound();
            Game.LoadScene("MainMenu", GamePhase.MainMenu);
        }

        private void ShowDeviceLost(string displayName)
        {
            if (deviceMessage != null)
                deviceMessage.text =
                    $"{displayName} disconnected. Reconnect it or use keyboard and mouse.";
        }

        private void ShowDeviceRegained(string displayName)
        {
            if (deviceMessage != null)
                deviceMessage.text = $"{displayName} connected.";
        }

    }
}
