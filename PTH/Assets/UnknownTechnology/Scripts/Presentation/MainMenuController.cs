using System;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.SceneFlow;
using UnknownTechnology.Core.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology.Presentation
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string StatusReady = "Investigate the missing technology relics.";
        private const string StatusUnavailable = "Game services are unavailable.";
        private const string StatusLoading = "Loading Ancient Exhibition...";
        private const string ContinueDisabledLabel = "Continue (No save yet)";

        private Button newGameButton;
        private Button continueButton;
        private Button quitButton;
        private Label statusLabel;

        private GameContext context;
        private IDisposable rejectionSubscription;
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

            newGameButton = root.Q<Button>("new-game-button");
            continueButton = root.Q<Button>("continue-button");
            quitButton = root.Q<Button>("quit-button");
            statusLabel = root.Q<Label>("status-label");

            if (!GameContextProvider.IsReady)
            {
                statusLabel.text = StatusUnavailable;
                SetInteractable(false);
                return;
            }

            bound = true;
            context = GameContextProvider.Current;
            newGameButton.clicked += StartNewGame;
            quitButton.clicked += QuitGame;
            continueButton.SetEnabled(false);
            continueButton.text = ContinueDisabledLabel;
            statusLabel.text = StatusReady;
            rejectionSubscription = context.EventBus.Subscribe<SceneLoadRejected>(ShowRejection);
            newGameButton.Focus();
        }

        private void Unbind()
        {
            if (!bound)
            {
                return;
            }

            newGameButton.clicked -= StartNewGame;
            quitButton.clicked -= QuitGame;
            rejectionSubscription?.Dispose();
            rejectionSubscription = null;
            bound = false;
        }

        private void StartNewGame()
        {
            SetInteractable(false);
            statusLabel.text = StatusLoading;
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
            statusLabel.text = rejection.Reason;
            SetInteractable(true);
        }

        private void SetInteractable(bool value)
        {
            newGameButton.SetEnabled(value);
            quitButton.SetEnabled(value);
        }
    }
}
