
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace UnknownTechnology
{
    public class CanvasQuizUI : MonoBehaviour
    {
        [Header("Canvas Quiz")]
        [SerializeField] private GameObject quizPanel;

        [Header("ERA UI")]
        [Tooltip("The GameObject containing the ERA UI Toolkit UIDocument.")]
        [SerializeField] private GameObject eraUIObject;

        [Header("Input")]
        [SerializeField] private Key toggleQuizKey = Key.T;

        public static bool IsQuizOpen { get; private set; }

        private bool previousCursorVisible;
        private CursorLockMode previousCursorLockState;

        private void Awake()
        {
            IsQuizOpen = false;
            if (quizPanel != null)
                quizPanel.SetActive(false);

            
        }

        private void Update()
        {
            if (IsQuizOpen && Game.Runtime != null && Game.Phase != GamePhase.Exploring)
                CloseQuiz();

            if (Keyboard.current != null &&
                (Keyboard.current[toggleQuizKey].wasPressedThisFrame ||
                 (IsQuizOpen && Keyboard.current.escapeKey.wasPressedThisFrame)))
            {
                if (IsQuizOpen)
                    CloseQuiz();
                else
                    OpenQuiz();
            }
        }

        public void OpenQuiz()
        {
            if (quizPanel == null)
            {
                Debug.LogError("CanvasQuizUI: Quiz Panel is not assigned.", this);
                return;
            }

            if (IsQuizOpen || (Game.Runtime != null && Game.Phase != GamePhase.Exploring))
                return;

            previousCursorVisible = Cursor.visible;
            previousCursorLockState = Cursor.lockState;

            IsQuizOpen = true;

            // Hide the HUD without disabling its pause-menu bindings.
            if (eraUIObject != null)
                eraUIObject.GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;

            // Show the Canvas quiz.
            quizPanel.SetActive(true);

            // Allow the mouse to interact with Canvas buttons.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("CanvasQuizUI: Quiz opened.");
        }

        public void CloseQuiz()
        {
            if (!IsQuizOpen)
                return;

            if (quizPanel != null)
                quizPanel.SetActive(false);

            IsQuizOpen = false;

            // Restore the HUD.
            if (eraUIObject != null)
                eraUIObject.GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.Flex;

            // Restore the cursor state from before the quiz opened.
            if (Game.Runtime == null || Game.Phase == GamePhase.Exploring)
            {
                Cursor.lockState = previousCursorLockState;
                Cursor.visible = previousCursorVisible;
            }

            Debug.Log("CanvasQuizUI: Quiz closed.");
        }

        private void OnDisable()
        {
            // Avoid leaving the cursor unlocked if this controller is disabled.
            if (IsQuizOpen)
                CloseQuiz();
        }
    }
}
