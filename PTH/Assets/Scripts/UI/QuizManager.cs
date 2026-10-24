
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnknownTechnology.Audio;
public class QuizManager : MonoBehaviour
{
    [SerializeField] private AudioClip correctAnswerSound;
    [SerializeField] private AudioClip incorrectAnswerSound;
    private AudioManager audioManager;
    [System.Serializable]

    public class Question
    {
        [TextArea]
        public string questionText;

        public string[] answers = new string[4];

        // The index of the correct answer:
        // 0 = A, 1 = B, 2 = C, 3 = D
        [Range(0, 3)]
        public int correctAnswerIndex;
    }

    [Header("Quiz Questions")]
    public Question[] questions;

    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text feedbackText;
    public Button[] answerButtons;
    public TMP_Text[] answerButtonTexts;
    public GameObject quizPanel;
    public GameObject completionPanel;

    [Header("Answer Colours")]
    public Color correctColour = new Color32(70, 125, 75, 255);
    public Color incorrectColour = new Color32(160, 65, 55, 255);

    private Color[] originalColours;

    private int currentQuestionIndex = 0;
    private bool questionAnswered = false;



    private void Start()
    {
        audioManager = FindAnyObjectByType<AudioManager>();
        if (completionPanel != null)
            completionPanel.SetActive(false);

        // CanvasQuizUI controls when the quiz is visible.

        // Connect each button to its answer index.
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int answerIndex = i;
            answerButtons[i].onClick.AddListener(
                () => CheckAnswer(answerIndex)
            );
        }

        originalColours = new Color[answerButtons.Length];

        for (int i = 0; i < answerButtons.Length; i++)
        {
            originalColours[i] = answerButtons[i].image.color;
        }

        DisplayQuestion();
    }

    private void DisplayQuestion()
    {
        questionAnswered = false;

        Question question = questions[currentQuestionIndex];

        questionText.text = question.questionText;
        feedbackText.text = "";

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(true);
            answerButtons[i].interactable = true;
            answerButtonTexts[i].text = question.answers[i];
            answerButtons[i].image.color = originalColours[i];
        }
    }

    private void CheckAnswer(int selectedAnswerIndex)
    {
        if (questionAnswered)
            return;

        Question question = questions[currentQuestionIndex];

        if (selectedAnswerIndex == question.correctAnswerIndex)
        {
            questionAnswered = true;
            feedbackText.text = "Correct!";
            audioManager?.PlayUI(correctAnswerSound);

            answerButtons[selectedAnswerIndex].image.color = correctColour;

            foreach (Button button in answerButtons)
                button.interactable = false;

            Invoke(nameof(NextQuestion), 1f);
        }
        else
        {
            feedbackText.text = "Incorrect! Try again.";
            audioManager?.PlayUI(incorrectAnswerSound);
            answerButtons[selectedAnswerIndex].image.color = incorrectColour;
        }
    }

    private void NextQuestion()
    {
        currentQuestionIndex++;

        if (currentQuestionIndex >= questions.Length)
        {
            // All questions have been answered.
            if (quizPanel != null)
                quizPanel.SetActive(false);

            if (completionPanel != null)
            {
                // Keep completion hidden if the player closed the quiz during answer feedback.
                completionPanel.SetActive(UnknownTechnology.CanvasQuizUI.IsQuizOpen);
                if (completionPanel.activeInHierarchy)
                    completionPanel.GetComponentInChildren<Button>()?.Select();
            }
            else
                FindAnyObjectByType<UnknownTechnology.CanvasQuizUI>()?.CloseQuiz();

            currentQuestionIndex = 0;
            DisplayQuestion();
            return;
        }

        DisplayQuestion();
    }
}
