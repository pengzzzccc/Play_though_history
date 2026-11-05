using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using TMPro;
using CanvasButton = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Cursor = UnityEngine.Cursor;

namespace UnknownTechnology
{
    public class CanvasQuizUI : MonoBehaviour
    {
        [Header("Canvas Quiz")]
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private PapyrusWallPuzzle requiredPuzzle;

        [Header("ERA UI")]
        [Tooltip("The GameObject containing the ERA UI Toolkit UIDocument.")]
        [SerializeField] private GameObject eraUIObject;

        public static bool IsQuizOpen { get; private set; }
        public bool HasSeenIntroduction { get; private set; }
        public bool CanStartQuiz => HasSeenIntroduction && requiredPuzzle != null &&
            requiredPuzzle.IsComplete && !requiredPuzzle.IsCheckingAnswers;
        public bool CanInteractWithJimmy => !HasSeenIntroduction || CanStartQuiz;
        public event System.Action Opened;
        public event System.Action IntroductionFinished;

        private bool previousCursorVisible;
        private CursorLockMode previousCursorLockState;
        private GameObject introductionPanel;
        private TMP_Text introductionText;
        private TMP_Text introductionButtonText;
        private CanvasButton introductionButton;
        private int introductionPage;

        private static readonly string[] IntroductionLines =
        {
            "Hi sir, do you work here??",
            "I'm really interested in this papyrus exhibit, but it's not making any sense!!\n" +
            "I think its missing some important information.\nCan you help??"
        };

        private void Awake()
        {
            IsQuizOpen = false;
            if (quizPanel != null)
                quizPanel.SetActive(false);
            if (completionPanel != null)
                completionPanel.SetActive(false);
        }

        private void Update()
        {
            if (!IsQuizOpen) return;
            if ((Game.Runtime != null && Game.Phase != GamePhase.Exploring) ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
                CloseQuiz();
        }

        public void InteractWithJimmy()
        {
            if (HasSeenIntroduction)
            {
                OpenQuiz();
                return;
            }

            if (!BeginInteraction()) return;
            CreateIntroductionPanel();
            introductionPage = 0;
            ShowIntroductionPage();
            introductionPanel.SetActive(true);
            introductionButton.Select();
        }

        public void OpenQuiz()
        {
            if (!CanStartQuiz || !BeginInteraction()) return;
            quizPanel.SetActive(true);
            Opened?.Invoke();
        }

        private bool BeginInteraction()
        {
            if (quizPanel == null)
            {
                Debug.LogError("CanvasQuizUI: Quiz Panel is not assigned.", this);
                return false;
            }

            if (IsQuizOpen || (Game.Runtime != null && Game.Phase != GamePhase.Exploring))
                return false;

            previousCursorVisible = Cursor.visible;
            previousCursorLockState = Cursor.lockState;

            IsQuizOpen = true;

            // Hide the HUD without disabling its pause-menu bindings.
            SetEraUIVisible(false);

            // Dialogue and quiz share the same cursor and player-control handling.
            if (completionPanel != null)
                completionPanel.SetActive(false);
            quizPanel.SetActive(false);

            // Allow the mouse to interact with Canvas buttons.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return true;
        }

        private void CreateIntroductionPanel()
        {
            if (introductionPanel != null) return;

            var overlay = RuntimeUI.Create<Image>("Timmy Introduction", quizPanel.transform.parent);
            introductionPanel = overlay.gameObject;
            RuntimeUI.Stretch(overlay.rectTransform);
            overlay.color = new Color(0f, 0f, 0f, 0.7f);

            var background = RuntimeUI.Create<Image>("Dialogue Card", overlay.transform);
            var card = background.rectTransform;
            card.anchorMin = new Vector2(0.1f, 0.2f);
            card.anchorMax = new Vector2(0.9f, 0.8f);
            card.offsetMin = card.offsetMax = Vector2.zero;
            background.color = new Color(0.12f, 0.10f, 0.08f, 1f);

            // Match the quiz's assigned font rather than depending on a default TMP font.
            var font = quizPanel.GetComponentInChildren<TMP_Text>(true)?.font ?? TMP_Settings.defaultFontAsset;
            var speaker = RuntimeUI.CreateText("Speaker", card, font, 36);
            speaker.text = "Timmy";
            speaker.fontStyle = FontStyles.Bold;
            speaker.color = new Color(1f, 0.85f, 0.5f);
            speaker.rectTransform.anchorMin = new Vector2(0f, 1f);
            speaker.rectTransform.anchorMax = Vector2.one;
            speaker.rectTransform.offsetMin = new Vector2(36f, -76f);
            speaker.rectTransform.offsetMax = new Vector2(-36f, -24f);

            introductionText = RuntimeUI.CreateText("Dialogue", card, font, 32);
            introductionText.enableAutoSizing = true;
            introductionText.fontSizeMin = 20;
            introductionText.fontSizeMax = 32;
            RuntimeUI.Stretch(introductionText.rectTransform, new Vector2(36f, 110f), new Vector2(-36f, -96f));
            CreateIntroductionButton(card, font);
        }

        private void CreateIntroductionButton(Transform parent, TMP_FontAsset font)
        {
            var image = RuntimeUI.Create<Image>("Continue", parent);
            image.color = new Color(0.33f, 0.27f, 0.17f);
            var buttonRect = image.rectTransform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-36f, 28f);
            buttonRect.sizeDelta = new Vector2(220f, 60f);
            introductionButton = image.gameObject.AddComponent<CanvasButton>();
            introductionButton.targetGraphic = image;
            introductionButton.onClick.AddListener(NextIntroductionPage);
            introductionButtonText = RuntimeUI.CreateText("Label", buttonRect, font, 28, TextAlignmentOptions.Center);
            RuntimeUI.Stretch(introductionButtonText.rectTransform);
        }

        private void ShowIntroductionPage()
        {
            introductionText.text = IntroductionLines[introductionPage];
            introductionButtonText.text = introductionPage == IntroductionLines.Length - 1 ? "Exit" : "Next";
        }

        private void NextIntroductionPage()
        {
            if (!IsQuizOpen || introductionPanel == null || !introductionPanel.activeSelf) return;
            if (introductionPage == IntroductionLines.Length - 1)
                CloseQuiz();
            else
            {
                introductionPage++;
                ShowIntroductionPage();
            }
        }

        public void CloseQuiz()
        {
            if (!IsQuizOpen)
                return;

            var wasIntroduction = introductionPanel != null && introductionPanel.activeSelf;
            if (introductionPanel != null)
                introductionPanel.SetActive(false);
            if (quizPanel != null)
                quizPanel.SetActive(false);
            if (completionPanel != null)
                completionPanel.SetActive(false);

            IsQuizOpen = false;

            // Restore the HUD.
            SetEraUIVisible(true);

            // Restore the cursor state from before the quiz opened.
            if (Game.Runtime == null || Game.Phase == GamePhase.Exploring)
            {
                Cursor.lockState = previousCursorLockState;
                Cursor.visible = previousCursorVisible;
            }

            if (wasIntroduction)
            {
                HasSeenIntroduction = true;
                IntroductionFinished?.Invoke();
            }
        }

        private void SetEraUIVisible(bool visible)
        {
            // The UI document can already be torn down during a scene change or
            // script reload, before this component receives OnDisable.
            var root = eraUIObject != null
                ? eraUIObject.GetComponent<UIDocument>()?.rootVisualElement
                : null;

            if (root != null)
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDisable()
        {
            // Avoid leaving the cursor unlocked if this controller is disabled.
            if (IsQuizOpen)
                CloseQuiz();
        }
    }
}
