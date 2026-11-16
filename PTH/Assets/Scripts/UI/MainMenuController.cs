using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnknownTechnology.Audio;

namespace UnknownTechnology
{
    /// <summary>
    /// Drives the main menu's three mutually exclusive panels: the main menu,
    /// the save-slot picker and the shared settings panel. Cancel steps back one
    /// panel at a time.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private const string HiddenClass = "hidden";
        private const string ConfirmClass = "slot-delete--confirm";
        private const string StatusReady = "Investigate the missing technology relics.";
        private const string StatusUnavailable = "Game services are unavailable.";
        private const long DeleteConfirmTimeoutMs = 3000;

        private static bool openCreditsOnNextLoad;

        private AudioManager audioManager;
        private VisualElement menuPanel;
        private VisualElement playPanel;
        private Button playButton;
        private Button settingsButton;
        private Button creditsButton;
        private Button quitButton;
        private Button playBackButton;
        private readonly Button[] slotButtons = new Button[SaveSystem.SlotCount];
        private readonly Button[] deleteButtons = new Button[SaveSystem.SlotCount];
        private readonly SaveData[] slotSaves = new SaveData[SaveSystem.SlotCount];
        private Label statusLabel;
        private SettingsPanelBinder settingsBinder;
        private CreditsPanelBinder creditsBinder;
        private Action showPlayPanel;
        private Action showSettingsPanel;
        private Action showCreditsPanel;
        private Action showMenuPanel;
        private Action closeCreditsPanel;
        private readonly Action[] slotClickHandlers = new Action[SaveSystem.SlotCount];
        private readonly Action[] deleteClickHandlers = new Action[SaveSystem.SlotCount];
        private readonly EventCallback<FocusOutEvent>[] deleteFocusOutHandlers = new EventCallback<FocusOutEvent>[SaveSystem.SlotCount];
        private int confirmingDelete = -1;
        private IVisualElementScheduledItem deleteConfirmTimeout;
        private string currentPanel = "menu";
        private bool bound;

        /// <summary>
        /// Requests the main menu to open straight into the End Credits screen on
        /// its next load (e.g. after completing the final era).
        /// </summary>
        public static void RequestCreditsOnLoad()
        {
            openCreditsOnNextLoad = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            openCreditsOnNextLoad = false;
        }

        private void Start()
        {   
            Bind();
            audioManager?.EnsureMainMenuMusic();
        }

        private void Update()
        {
            if (bound && currentPanel == "credits")
            {
                creditsBinder?.Tick(Time.unscaledDeltaTime);
            }
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

            menuPanel = root.Q<VisualElement>("menu-panel");
            playPanel = root.Q<VisualElement>("play-panel");
            playButton = root.Q<Button>("play-button");
            settingsButton = root.Q<Button>("settings-button");
            creditsButton = root.Q<Button>("credits-button");
            quitButton = root.Q<Button>("quit-button");
            playBackButton = root.Q<Button>("play-back-button");
            statusLabel = root.Q<Label>("status-label");
            for (var index = 0; index < slotButtons.Length; index++)
            {
                slotButtons[index] = root.Q<Button>($"slot-{index + 1}-button");
                deleteButtons[index] = root.Q<Button>($"slot-{index + 1}-delete-button");
            }

            settingsBinder = new SettingsPanelBinder(root);
            creditsBinder = new CreditsPanelBinder(root);
            audioManager = FindAnyObjectByType<AudioManager>();

            showPlayPanel = () => { PlayClickSound(); ShowPanel("play"); };
            showSettingsPanel = () => { PlayClickSound(); ShowPanel("settings"); };
            showCreditsPanel = () => { PlayClickSound(); ShowPanel("credits"); };
            showMenuPanel = () => { PlayClickSound(); ShowPanel("menu"); };
            closeCreditsPanel = () => ShowPanel("menu");
            for (var index = 0; index < slotButtons.Length; index++)
            {
                var slot = index;
                slotClickHandlers[index] = () => SelectSlot(slot);
                deleteClickHandlers[index] = () => HandleDeleteClicked(slot);
                deleteFocusOutHandlers[index] = _ => CancelDeleteConfirm(slot);
            }

            playPanel.AddToClassList(HiddenClass);

            if (Game.Runtime == null)
            {
                statusLabel.text = StatusUnavailable;
                SetInteractable(false);
                return;
            }

            bound = true;
            playButton.clicked += showPlayPanel;
            settingsButton.clicked += showSettingsPanel;
            if (creditsButton != null)
            {
                creditsButton.clicked += showCreditsPanel;
            }
            quitButton.clicked += QuitGame;
            playBackButton.clicked += showMenuPanel;
            settingsBinder.BackRequested += showMenuPanel;
            creditsBinder.CloseRequested += closeCreditsPanel;
            for (var index = 0; index < slotButtons.Length; index++)
            {
                slotButtons[index].clicked += slotClickHandlers[index];
                deleteButtons[index].clicked += deleteClickHandlers[index];
                deleteButtons[index].RegisterCallback(deleteFocusOutHandlers[index]);
            }

            GameEvents.CancelPressed += HandleCancel;
            RefreshSlots();
            statusLabel.text = StatusReady;

            var initialPanel = openCreditsOnNextLoad ? "credits" : "menu";
            openCreditsOnNextLoad = false;
            ShowPanel(initialPanel);
        }

        private void Unbind()
        {
            if (!bound)
            {
                return;
            }

            playButton.clicked -= showPlayPanel;
            settingsButton.clicked -= showSettingsPanel;
            if (creditsButton != null)
            {
                creditsButton.clicked -= showCreditsPanel;
            }
            quitButton.clicked -= QuitGame;
            playBackButton.clicked -= showMenuPanel;
            settingsBinder.BackRequested -= showMenuPanel;
            creditsBinder.CloseRequested -= closeCreditsPanel;
            for (var index = 0; index < slotButtons.Length; index++)
            {
                slotButtons[index].clicked -= slotClickHandlers[index];
                deleteButtons[index].clicked -= deleteClickHandlers[index];
                deleteButtons[index].UnregisterCallback(deleteFocusOutHandlers[index]);
            }

            deleteConfirmTimeout?.Pause();
            GameEvents.CancelPressed -= HandleCancel;
            settingsBinder.Unbind();
            creditsBinder.Unbind();
            bound = false;
        }

        private void PlayClickSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayButtonClickSound();
            }
        }
        private void StopMusic()
        {
            if (audioManager != null)
            {
                audioManager.StopMusic();
            }
        }

        private void ShowPanel(string panel)
        {
            currentPanel = panel;
            menuPanel.EnableInClassList(HiddenClass, panel != "menu");
            playPanel.EnableInClassList(HiddenClass, panel != "play");
            settingsBinder.SetVisible(panel == "settings");
            creditsBinder.SetVisible(panel == "credits");

            switch (panel)
            {
                case "play":
                    RefreshSlots();
                    slotButtons[0].Focus();
                    break;
                case "menu":
                    playButton.Focus();
                    break;
            }
        }

        private void HandleCancel()
        {
            if (currentPanel != "menu")
            {
                ShowPanel("menu");
            }
        }

        private void RefreshSlots()
        {
            CancelDeleteConfirm(confirmingDelete);
            for (var index = 0; index < slotButtons.Length; index++)
            {
                var save = SaveSystem.ReadSummary(index + 1);
                slotSaves[index] = save;
                slotButtons[index].text = save == null
                    ? $"Slot {index + 1} — Empty"
                    : $"Slot {index + 1}\n{SceneDisplayName(save.currentScene)}\n{SaveSystem.FormatSavedAt(save)}\nPlay time {FormatPlayTime(save.playTimeSeconds)}";
                deleteButtons[index].EnableInClassList(HiddenClass, save == null);
            }
        }

        private void SelectSlot(int index)
        {
            PlayClickSound();
            var slot = index + 1;
            string sceneName;
            if (slotSaves[index] != null && SaveSystem.Continue(slot))
            {
                sceneName = SaveSystem.Current.currentScene;
            }
            else
            {
                SaveSystem.StartNewGame(slot);
                sceneName = SaveSystem.FirstScene;
            }

            StopMusic();
            SetInteractable(false);
            statusLabel.text = $"Loading {SceneDisplayName(sceneName)}...";
            Game.LoadScene(sceneName, GamePhase.Exploring);
        }

        private void HandleDeleteClicked(int index)
        {
            PlayClickSound();
            if (confirmingDelete != index)
            {
                CancelDeleteConfirm(confirmingDelete);
                confirmingDelete = index;
                deleteButtons[index].text = "Confirm?";
                deleteButtons[index].AddToClassList(ConfirmClass);
                deleteConfirmTimeout?.Pause();
                deleteConfirmTimeout = deleteButtons[index].schedule
                    .Execute(() => CancelDeleteConfirm(index))
                    .StartingIn(DeleteConfirmTimeoutMs);
                return;
            }

            SaveSystem.Delete(index + 1);
            RefreshSlots();
            slotButtons[index].Focus();
        }

        private void CancelDeleteConfirm(int index)
        {
            if (index < 0 || index != confirmingDelete)
            {
                return;
            }

            confirmingDelete = -1;
            deleteConfirmTimeout?.Pause();
            deleteButtons[index].text = "Delete";
            deleteButtons[index].RemoveFromClassList(ConfirmClass);
        }

        private static string SceneDisplayName(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && sceneName.StartsWith("Era_", StringComparison.Ordinal)
                ? sceneName.Substring(4) + " Exhibition"
                : sceneName;
        }

        private static string FormatPlayTime(float seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return time.TotalHours >= 1d
                ? $"{(int)time.TotalHours} h {time.Minutes} min"
                : $"{Math.Max(1, (int)time.TotalMinutes)} min";
        }

        private void QuitGame()
        {
            PlayClickSound();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetInteractable(bool value)
        {
            playButton.SetEnabled(value);
            settingsButton.SetEnabled(value);
            creditsButton?.SetEnabled(value);
            quitButton.SetEnabled(value);
            foreach (var button in slotButtons)
            {
                button?.SetEnabled(value);
            }

            foreach (var button in deleteButtons)
            {
                button?.SetEnabled(value);
            }
        }
    }
}
