
using UnityEngine;
using UnityEngine.UIElements;
using UnknownTechnology.Audio;
using UnityEngine.InputSystem;

namespace UnknownTechnology
{
    /// <summary>
    /// Controls the pause menu and the Canvas-based quiz.
    /// The pause menu uses UI Toolkit; the quiz uses Unity Canvas UI.
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
        private VisualElement uiRoot;

        // Canvas quiz reference
        [Header("Quiz UI")]
        [SerializeField] private GameObject quizPanel;

        private bool bound;
        private bool quizIsOpen;

        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisibility;

        public static bool IsQuizOpen { get; private set; }

        private void Start()
        {
            // Make sure the quiz panel starts hidden.
            if (quizPanel != null)
            {
                quizPanel.SetActive(false);
            }

            IsQuizOpen = false;
            quizIsOpen = false;

            Bind();
        }

        private void OnDisable()
        {
            Unbind();

            // Avoid leaving the cursor unlocked if this component is disabled
            // while the quiz is open.
            if (quizIsOpen)
            {
                CloseQuiz();
            }
        }

        private void Bind()
        {
            if (bound)
                return;

            var root = GetComponent<UIDocument>().rootVisualElement;
            uiRoot = root;

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

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.tKey.wasPressedThisFrame)
                ToggleQuiz();
        }

        private void ToggleQuiz()
        {
            if (quizPanel == null)
                return;

            // Do not open the quiz while the pause menu is active.
            if (!quizIsOpen && Game.Phase == GamePhase.Paused)
                return;

            if (quizIsOpen)
                CloseQuiz();
            else
                OpenQuiz();
        }

        public void OpenQuiz()
        {
            if (uiRoot != null)
                uiRoot.style.display = DisplayStyle.None;

            if (quizPanel == null || quizIsOpen)
                return;

            if (Game.Phase == GamePhase.Paused)
                return;

            previousCursorLockState = UnityEngine.Cursor.lockState;
            previousCursorVisibility = UnityEngine.Cursor.visible;

            quizIsOpen = true;
            IsQuizOpen = true;

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            quizPanel.SetActive(true);
        }

        public void CloseQuiz()
        {
            if (uiRoot != null)
                uiRoot.style.display = DisplayStyle.Flex;
                
            if (!quizIsOpen)
            {
                // Also ensure the panel is hidden if it was opened accidentally
                // in the Inspector or by another script.
                if (quizPanel != null)
                    quizPanel.SetActive(false);

                IsQuizOpen = false;
                return;
            }

            quizIsOpen = false;
            IsQuizOpen = false;

            if (quizPanel != null)
                quizPanel.SetActive(false);

            UnityEngine.Cursor.lockState = previousCursorLockState;
            UnityEngine.Cursor.visible = previousCursorVisibility;
        }
    }
}
