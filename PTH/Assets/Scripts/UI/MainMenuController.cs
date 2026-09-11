using System;
using UnityEngine;
using UnityEngine.UIElements;

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
        private const string EraSceneName = "Era_Ancient";
        private const string HiddenClass = "hidden";
        private const string StatusReady = "Investigate the missing technology relics.";
        private const string StatusUnavailable = "Game services are unavailable.";
        private const string StatusLoading = "Loading Ancient Exhibition...";

        private VisualElement menuPanel;
        private VisualElement playPanel;
        private Button playButton;
        private Button settingsButton;
        private Button quitButton;
        private Button playBackButton;
        private readonly Button[] slotButtons = new Button[3];
        private Label statusLabel;
        private SettingsPanelBinder settingsBinder;
        private Action showPlayPanel;
        private Action showSettingsPanel;
        private Action showMenuPanel;
        private readonly Action[] slotClickHandlers = new Action[3];
        private string currentPanel = "menu";
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

            menuPanel = root.Q<VisualElement>("menu-panel");
            playPanel = root.Q<VisualElement>("play-panel");
            playButton = root.Q<Button>("play-button");
            settingsButton = root.Q<Button>("settings-button");
            quitButton = root.Q<Button>("quit-button");
            playBackButton = root.Q<Button>("play-back-button");
            statusLabel = root.Q<Label>("status-label");
            slotButtons[0] = root.Q<Button>("slot-1-button");
            slotButtons[1] = root.Q<Button>("slot-2-button");
            slotButtons[2] = root.Q<Button>("slot-3-button");
            settingsBinder = new SettingsPanelBinder(root);

            showPlayPanel = () => ShowPanel("play");
            showSettingsPanel = () => ShowPanel("settings");
            showMenuPanel = () => ShowPanel("menu");
            for (var index = 0; index < slotButtons.Length; index++)
            {
                var slot = index;
                slotClickHandlers[index] = () => StartNewGame(slot);
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
            quitButton.clicked += QuitGame;
            playBackButton.clicked += showMenuPanel;
            settingsBinder.BackRequested += showMenuPanel;
            for (var index = 0; index < slotButtons.Length; index++)
            {
                slotButtons[index].clicked += slotClickHandlers[index];
            }

            GameEvents.CancelPressed += HandleCancel;
            statusLabel.text = StatusReady;
            ShowPanel("menu");
        }

        private void Unbind()
        {
            if (!bound)
            {
                return;
            }

            playButton.clicked -= showPlayPanel;
            settingsButton.clicked -= showSettingsPanel;
            quitButton.clicked -= QuitGame;
            playBackButton.clicked -= showMenuPanel;
            settingsBinder.BackRequested -= showMenuPanel;
            for (var index = 0; index < slotButtons.Length; index++)
            {
                slotButtons[index].clicked -= slotClickHandlers[index];
            }

            GameEvents.CancelPressed -= HandleCancel;
            settingsBinder.Unbind();
            bound = false;
        }

        private void ShowPanel(string panel)
        {
            currentPanel = panel;
            menuPanel.EnableInClassList(HiddenClass, panel != "menu");
            playPanel.EnableInClassList(HiddenClass, panel != "play");
            settingsBinder.SetVisible(panel == "settings");

            switch (panel)
            {
                case "play":
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

        private void StartNewGame(int slot)
        {
            SetInteractable(false);
            statusLabel.text = StatusLoading;
            // Save slots are recorded here once the save system exists (M13);
            // every slot starts the same greybox exhibition for now.
            Game.LoadScene(EraSceneName, GamePhase.Exploring);
        }

        private void QuitGame()
        {
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
            quitButton.SetEnabled(value);
        }
    }
}
