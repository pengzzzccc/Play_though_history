using UnityEngine;

namespace UnknownTechnology
{
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class ExhibitionGuidance : MonoBehaviour
    {
        [SerializeField] private PapyrusWallPuzzle puzzle;
        [SerializeField] private CanvasQuizUI quizUI;

        private bool enteredRoom;
        private bool initialInteractionComplete;
        private bool completionGuidanceShown;
        private readonly GuidancePrompt guidance = new GuidancePrompt();

        private bool CanShowGuidance => enteredRoom && puzzle != null &&
            Game.Phase == GamePhase.Exploring && !CanvasQuizUI.IsQuizOpen && !puzzle.IsCheckingAnswers;

        private void OnEnable()
        {
            if (quizUI != null)
            {
                quizUI.Opened += JimmyInteracted;
                quizUI.IntroductionFinished += IntroductionFinished;
            }
        }

        private void OnDisable()
        {
            if (quizUI != null)
            {
                quizUI.Opened -= JimmyInteracted;
                quizUI.IntroductionFinished -= IntroductionFinished;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (enteredRoom || other.GetComponentInParent<FirstPersonPlayer>() == null) return;

            enteredRoom = true;
            if (!initialInteractionComplete && puzzle != null && !puzzle.IsUnlocked && !puzzle.IsComplete)
                guidance.Show("Interact with Jimmy");
        }

        private void JimmyInteracted()
        {
            guidance.Clear();
            if (puzzle != null && puzzle.IsComplete)
                completionGuidanceShown = true;
        }

        private void IntroductionFinished()
        {
            initialInteractionComplete = true;
            guidance.Clear();
            if (puzzle != null && !puzzle.IsUnlocked && !puzzle.IsStarted && !puzzle.IsComplete)
                guidance.Show("Examine papyrus");
        }

        private void Update()
        {
            if (puzzle == null) return;

            // The papyrus takes over guidance once its narration has unlocked the puzzle.
            if (!puzzle.IsComplete && (puzzle.IsUnlocked || puzzle.IsStarted))
            {
                guidance.Clear();
                return;
            }

            if (enteredRoom && puzzle.IsComplete && !puzzle.IsCheckingAnswers && !completionGuidanceShown)
            {
                completionGuidanceShown = true;
                guidance.Show("Interact with Jimmy");
            }

            // Prompts queued behind the quiz keep their full duration until it closes.
            if (CanShowGuidance)
                guidance.Tick(Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (CanShowGuidance) guidance.Draw();
        }
    }
}
