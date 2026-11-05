using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnknownTechnology;
using UnknownTechnology.Audio;

public class QuizManager : MonoBehaviour
{
    private const int MaximumChances = 3;

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

    private int currentQuestionIndex;
    private bool questionAnswered;
    private int chancesUsed;
    private TMP_Text chancesText;

    private void Start()
    {
        audioManager = FindAnyObjectByType<AudioManager>();
        if (completionPanel != null)
            completionPanel.SetActive(false);

        originalColours = new Color[answerButtons.Length];
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int answerIndex = i;
            answerButtons[i].onClick.AddListener(() => CheckAnswer(answerIndex));
            originalColours[i] = answerButtons[i].image.color;
        }

        CreateChancesText();
        DisplayQuestion();
    }

    private void CreateChancesText()
    {
        chancesText = RuntimeUI.CreateText("Quiz Chances", quizPanel.transform, feedbackText.font, 36, TextAlignmentOptions.Right);
        chancesText.fontStyle = FontStyles.Bold;

        RectTransform rect = chancesText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-32f, 10f);
        rect.sizeDelta = new Vector2(340f, 48f);
    }

    private void UpdateChancesText()
    {
        chancesText.text = $"{chancesUsed}/{MaximumChances} Chances";
        chancesText.color = chancesUsed > 0 ? incorrectColour : feedbackText.color;
    }

    private void DisplayQuestion()
    {
        questionAnswered = false;

        Question question = questions[currentQuestionIndex];

        questionText.text = question.questionText;
        feedbackText.text = "";
        UpdateChancesText();

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

        bool correct = selectedAnswerIndex == questions[currentQuestionIndex].correctAnswerIndex;
        audioManager?.PlayUI(correct ? correctAnswerSound : incorrectAnswerSound);
        answerButtons[selectedAnswerIndex].image.color = correct ? correctColour : incorrectColour;

        if (correct)
        {
            feedbackText.text = "Correct!";
            LockAnswers();
            Invoke(nameof(NextQuestion), 1f);
            return;
        }

        chancesUsed++;
        UpdateChancesText();
        if (chancesUsed < MaximumChances)
        {
            feedbackText.text = "Incorrect! Try again.";
            return;
        }

        feedbackText.text = "Out of chances! Talk to Jimmy to try again.";
        LockAnswers();
        Invoke(nameof(EndFailedAttempt), 1.5f);
    }

    private void LockAnswers()
    {
        questionAnswered = true;
        foreach (Button button in answerButtons)
            button.interactable = false;
    }

    private void EndFailedAttempt()
    {
        FindAnyObjectByType<CanvasQuizUI>()?.CloseQuiz();
        ResetQuiz();
    }

    private void ResetQuiz()
    {
        CancelInvoke();
        currentQuestionIndex = 0;
        chancesUsed = 0;
        DisplayQuestion();
    }

    private void NextQuestion()
    {
        currentQuestionIndex++;

        if (currentQuestionIndex < questions.Length)
        {
            DisplayQuestion();
            return;
        }

        if (quizPanel != null)
            quizPanel.SetActive(false);

        if (completionPanel != null)
        {
            // Closing during answer feedback must not reopen the completion panel.
            completionPanel.SetActive(CanvasQuizUI.IsQuizOpen);
            if (completionPanel.activeInHierarchy)
                completionPanel.GetComponentInChildren<Button>()?.Select();
        }
        else
            FindAnyObjectByType<CanvasQuizUI>()?.CloseQuiz();

        ResetQuiz();
    }
}
