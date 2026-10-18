
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
            if (Keyboard.current != null &&
                Keyboard.current[toggleQuizKey].wasPressedThisFrame)
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

            if (IsQuizOpen)
                return;

            previousCursorVisible = Cursor.visible;
            previousCursorLockState = Cursor.lockState;

            IsQuizOpen = true;

            // Disable the ERA UI Toolkit object.
            if (eraUIObject != null)
                eraUIObject.SetActive(false);

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

            // Restore the ERA UI object.
            if (eraUIObject != null)
                eraUIObject.SetActive(true);

            // Restore the cursor state from before the quiz opened.
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;

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
