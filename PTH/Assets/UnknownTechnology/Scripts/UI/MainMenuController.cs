using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private const string EraSceneName = "Era_Ancient";
        private const string StatusReady = "Investigate the missing technology relics.";
        private const string StatusUnavailable = "Game services are unavailable.";
        private const string StatusLoading = "Loading Ancient Exhibition...";
        private const string ContinueDisabledLabel = "Continue (No save yet)";

        private Button newGameButton;
        private Button continueButton;
        private Button quitButton;
        private Label statusLabel;
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

            if (Game.Runtime == null)
            {
                statusLabel.text = StatusUnavailable;
                SetInteractable(false);
                return;
            }

            bound = true;
            newGameButton.clicked += StartNewGame;
            quitButton.clicked += QuitGame;
            continueButton.SetEnabled(false);
            continueButton.text = ContinueDisabledLabel;
            statusLabel.text = StatusReady;
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
            bound = false;
        }

        private void StartNewGame()
        {
            SetInteractable(false);
            statusLabel.text = StatusLoading;
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
            newGameButton.SetEnabled(value);
            quitButton.SetEnabled(value);
        }
    }
}
