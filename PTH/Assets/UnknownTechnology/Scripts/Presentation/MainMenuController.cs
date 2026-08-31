using System;
using TMPro;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.SceneFlow;
using UnknownTechnology.Core.State;
using UnityEngine;
using UnityEngine.UI;

namespace UnknownTechnology.Presentation
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text statusText;

        private GameContext context;
        private IDisposable rejectionSubscription;

        private void Start()
        {
            if (!GameContextProvider.IsReady)
            {
                statusText.text = "Game services are unavailable.";
                SetInteractable(false);
                return;
            }

            context = GameContextProvider.Current;
            newGameButton.onClick.AddListener(StartNewGame);
            quitButton.onClick.AddListener(QuitGame);
            continueButton.interactable = false;
            continueButton.GetComponentInChildren<TMP_Text>().text = "Continue (No save yet)";
            statusText.text = "Investigate the missing technology relics.";
            rejectionSubscription = context.EventBus.Subscribe<SceneLoadRejected>(ShowRejection);
            newGameButton.Select();
        }

        private void StartNewGame()
        {
            SetInteractable(false);
            statusText.text = "Loading Ancient Exhibition...";
            if (!context.SceneFlow.RequestLoad(SceneFlowConfig.AncientRoute))
            {
                SetInteractable(true);
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowRejection(SceneLoadRejected rejection)
        {
            statusText.text = rejection.Reason;
            SetInteractable(true);
        }

        private void SetInteractable(bool value)
        {
            newGameButton.interactable = value;
            quitButton.interactable = value;
        }

        private void OnDestroy()
        {
            rejectionSubscription?.Dispose();
        }

#if UNITY_EDITOR
        public void Configure(Button newGame, Button continueGame, Button quit, TMP_Text status)
        {
            newGameButton = newGame;
            continueButton = continueGame;
            quitButton = quit;
            statusText = status;
        }
#endif
    }
}
