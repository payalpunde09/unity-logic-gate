using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DigitalLogicLab.UI;

namespace DigitalLogicLab.Quiz
{
    /// <summary>
    /// Controls Screen 4 (Quiz & Results).
    /// Manages the quiz lifecycle: start screen, 5-question delivery, option selection,
    /// answer locking, scoring, and comprehensive results breakdown.
    /// Results are strictly a sub-panel inside Screen 4.
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        [Header("Views inside Screen 4")]
        [SerializeField] private GameObject startView;
        [SerializeField] private GameObject questionView;
        [SerializeField] private GameObject resultsView;

        [Header("Start View Elements")]
        [SerializeField] private Button startQuizButton;

        [Header("Question View Elements")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI questionPromptText;
        [SerializeField] private Button[] optionButtons; // 4 buttons
        [SerializeField] private TextMeshProUGUI[] optionTexts; // 4 texts
        [SerializeField] private Image[] optionBackgrounds;
        [SerializeField] private Button submitAnswerButton;
        [SerializeField] private Button nextQuestionButton;
        [SerializeField] private TextMeshProUGUI feedbackNoticeText;

        [Header("Results View Elements")]
        [SerializeField] private TextMeshProUGUI resultsScoreText;
        [SerializeField] private TextMeshProUGUI resultsPercentageText;
        [SerializeField] private TextMeshProUGUI resultsBreakdownText;
        [SerializeField] private Transform resultsReviewContent;
        [SerializeField] private Button retryQuizButton;
        [SerializeField] private Button resultsBackToLabButton;
        [SerializeField] private Button resultsMainMenuButton;

        [Header("Top Navigation (When inside quiz)")]
        [SerializeField] private Button navBackToMenuButton;
        [SerializeField] private Button navBackToLabButton;

        private QuizAttempt currentAttempt;
        private int currentlySelectedOptionIndex = -1;
        private HashSet<int> lastAttemptQuestionIds = new HashSet<int>();
        private List<GameObject> spawnedReviewItems = new List<GameObject>();

        private readonly Color normalOptionColor = new Color(0.12f, 0.16f, 0.22f, 0.85f);
        private readonly Color selectedOptionColor = new Color(0.0f, 0.45f, 0.65f, 1f);
        private readonly Color correctOptionColor = new Color(0.1f, 0.65f, 0.35f, 0.95f);
        private readonly Color incorrectOptionColor = new Color(0.75f, 0.2f, 0.2f, 0.95f);

        public void Initialize(
            GameObject sView, GameObject qView, GameObject rView,
            Button startBtn,
            TextMeshProUGUI progTxt, TextMeshProUGUI qTxt,
            Button[] optBtns, TextMeshProUGUI[] optTxts, Image[] optBgs,
            Button subBtn, Button nextBtn, TextMeshProUGUI feedTxt,
            TextMeshProUGUI scoreTxt, TextMeshProUGUI percTxt, TextMeshProUGUI breakTxt,
            Transform reviewCont, Button retryBtn, Button backLabBtn, Button menuBtn,
            Button topMenuBtn, Button topLabBtn)
        {
            startView = sView;
            questionView = qView;
            resultsView = rView;

            startQuizButton = startBtn;
            progressText = progTxt;
            questionPromptText = qTxt;
            optionButtons = optBtns;
            optionTexts = optTxts;
            optionBackgrounds = optBgs;
            submitAnswerButton = subBtn;
            nextQuestionButton = nextBtn;
            feedbackNoticeText = feedTxt;

            resultsScoreText = scoreTxt;
            resultsPercentageText = percTxt;
            resultsBreakdownText = breakTxt;
            resultsReviewContent = reviewCont;
            retryQuizButton = retryBtn;
            resultsBackToLabButton = backLabBtn;
            resultsMainMenuButton = menuBtn;

            navBackToMenuButton = topMenuBtn;
            navBackToLabButton = topLabBtn;

            if (startQuizButton != null)
                startQuizButton.onClick.AddListener(StartNewQuiz);

            if (submitAnswerButton != null)
                submitAnswerButton.onClick.AddListener(SubmitAnswer);

            if (nextQuestionButton != null)
                nextQuestionButton.onClick.AddListener(ProceedToNextQuestion);

            if (retryQuizButton != null)
                retryQuizButton.onClick.AddListener(StartNewQuiz);

            if (resultsBackToLabButton != null)
                resultsBackToLabButton.onClick.AddListener(() => ScreenManager.Instance.OpenLab());

            if (resultsMainMenuButton != null)
                resultsMainMenuButton.onClick.AddListener(() => ScreenManager.Instance.OpenMenu());

            if (navBackToMenuButton != null)
                navBackToMenuButton.onClick.AddListener(() => ScreenManager.Instance.OpenMenu());

            if (navBackToLabButton != null)
                navBackToLabButton.onClick.AddListener(() => ScreenManager.Instance.OpenLab());

            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    int index = i;
                    optionButtons[i].onClick.AddListener(() => SelectOption(index));
                }
            }

            ShowStartView();
        }

        public void ShowStartView()
        {
            if (startView != null) startView.SetActive(true);
            if (questionView != null) questionView.SetActive(false);
            if (resultsView != null) resultsView.SetActive(false);
        }

        public void StartNewQuiz()
        {
            currentAttempt = QuizEngine.CreateAttempt(lastAttemptQuestionIds);

            // Record this attempt's question IDs so retry prefers an alternative set
            lastAttemptQuestionIds.Clear();
            foreach (var q in currentAttempt.Questions)
            {
                lastAttemptQuestionIds.Add(q.OriginalId);
            }

            if (startView != null) startView.SetActive(false);
            if (questionView != null) questionView.SetActive(true);
            if (resultsView != null) resultsView.SetActive(false);

            DisplayCurrentQuestion();
        }

        private void DisplayCurrentQuestion()
        {
            if (currentAttempt == null || currentAttempt.CurrentQuestionIndex >= currentAttempt.Questions.Count)
            {
                ShowResultsView();
                return;
            }

            ShuffledQuestion q = currentAttempt.Questions[currentAttempt.CurrentQuestionIndex];
            currentlySelectedOptionIndex = -1;

            if (progressText != null)
            {
                progressText.text = $"Question {currentAttempt.CurrentQuestionIndex + 1} of {currentAttempt.Questions.Count}";
            }

            if (questionPromptText != null)
            {
                questionPromptText.text = q.Text;
            }

            char[] optionLetters = new char[] { 'A', 'B', 'C', 'D' };
            for (int i = 0; i < 4; i++)
            {
                if (i < q.ShuffledOptions.Length)
                {
                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                    {
                        optionTexts[i].text = $"<b>{optionLetters[i]}.</b>  {q.ShuffledOptions[i]}";
                        optionTexts[i].color = Color.white;
                    }
                    if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
                    {
                        optionBackgrounds[i].color = normalOptionColor;
                    }
                    if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                    {
                        optionButtons[i].interactable = true;
                    }
                }
            }

            if (submitAnswerButton != null)
            {
                submitAnswerButton.gameObject.SetActive(true);
                submitAnswerButton.interactable = false; // Disabled until option chosen
            }

            if (nextQuestionButton != null)
            {
                nextQuestionButton.gameObject.SetActive(false);
            }

            if (feedbackNoticeText != null)
            {
                feedbackNoticeText.text = "Select an option and press Submit.";
                feedbackNoticeText.color = new Color(0.7f, 0.8f, 0.9f);
            }
        }

        public void SelectOption(int optionIndex)
        {
            ShuffledQuestion q = currentAttempt.Questions[currentAttempt.CurrentQuestionIndex];
            if (q.IsAnswered) return; // Locked

            currentlySelectedOptionIndex = optionIndex;

            for (int i = 0; i < 4; i++)
            {
                if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
                {
                    optionBackgrounds[i].color = (i == currentlySelectedOptionIndex) ? selectedOptionColor : normalOptionColor;
                }
            }

            if (submitAnswerButton != null)
            {
                submitAnswerButton.interactable = true;
            }
        }

        public void SubmitAnswer()
        {
            if (currentlySelectedOptionIndex < 0 || currentAttempt == null) return;

            ShuffledQuestion q = currentAttempt.Questions[currentAttempt.CurrentQuestionIndex];
            if (q.IsAnswered) return; // Prevent double submit

            q.SelectedIndex = currentlySelectedOptionIndex;
            q.IsAnswered = true;

            // Lock all option buttons (no double scoring or changing)
            for (int i = 0; i < 4; i++)
            {
                if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                {
                    optionButtons[i].interactable = false;
                }
            }

            bool isCorrect = q.IsCorrect;
            if (isCorrect)
            {
                currentAttempt.Score++;
            }

            // Highlight chosen option and reveal correct one
            for (int i = 0; i < 4; i++)
            {
                if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
                {
                    if (i == q.NewCorrectIndex)
                    {
                        optionBackgrounds[i].color = correctOptionColor;
                    }
                    else if (i == currentlySelectedOptionIndex && !isCorrect)
                    {
                        optionBackgrounds[i].color = incorrectOptionColor;
                    }
                    else
                    {
                        optionBackgrounds[i].color = normalOptionColor;
                    }
                }
            }

            if (submitAnswerButton != null)
            {
                submitAnswerButton.gameObject.SetActive(false);
            }

            if (nextQuestionButton != null)
            {
                nextQuestionButton.gameObject.SetActive(true);
                var nextText = nextQuestionButton.GetComponentInChildren<TextMeshProUGUI>();
                if (nextText != null)
                {
                    bool isLast = currentAttempt.CurrentQuestionIndex >= currentAttempt.Questions.Count - 1;
                    nextText.text = isLast ? "View Results" : "Next Question →";
                }
            }

            if (feedbackNoticeText != null)
            {
                if (isCorrect)
                {
                    feedbackNoticeText.text = "<color=#00FF88>✓ Correct!</color> " + q.Explanation;
                }
                else
                {
                    feedbackNoticeText.text = "<color=#FF4444>✗ Incorrect.</color> " + q.Explanation;
                }
            }
        }

        public void ProceedToNextQuestion()
        {
            if (currentAttempt == null) return;

            currentAttempt.CurrentQuestionIndex++;
            if (currentAttempt.CurrentQuestionIndex < currentAttempt.Questions.Count)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                ShowResultsView();
            }
        }

        public void ShowResultsView()
        {
            if (startView != null) startView.SetActive(false);
            if (questionView != null) questionView.SetActive(false);
            if (resultsView != null) resultsView.SetActive(true);

            if (currentAttempt == null) return;
            currentAttempt.IsCompleted = true;

            if (resultsScoreText != null)
            {
                resultsScoreText.text = $"{currentAttempt.Score} / {currentAttempt.TotalQuestions}";
            }

            if (resultsPercentageText != null)
            {
                resultsPercentageText.text = $"{currentAttempt.Percentage:F0}%";
                resultsPercentageText.color = currentAttempt.Score >= 4 ? new Color(0.0f, 1f, 0.7f) :
                                              currentAttempt.Score >= 3 ? new Color(1f, 0.85f, 0.2f) :
                                              new Color(1f, 0.4f, 0.4f);
            }

            if (resultsBreakdownText != null)
            {
                resultsBreakdownText.text = $"<b>Correct:</b> <color=#00FF88>{currentAttempt.CorrectCount}</color>    |    <b>Incorrect:</b> <color=#FF4444>{currentAttempt.IncorrectCount}</color>";
            }

            // Populate comprehensive review list of the 5 attempted questions ONLY
            foreach (var item in spawnedReviewItems)
            {
                if (item != null) Destroy(item);
            }
            spawnedReviewItems.Clear();

            if (resultsReviewContent != null)
            {
                for (int i = 0; i < currentAttempt.Questions.Count; i++)
                {
                    var q = currentAttempt.Questions[i];
                    GameObject card = new GameObject($"Review_Q{i + 1}");
                    card.transform.SetParent(resultsReviewContent, false);
                    spawnedReviewItems.Add(card);

                    var cardBg = card.AddComponent<Image>();
                    cardBg.color = q.IsCorrect ? new Color(0.08f, 0.2f, 0.16f, 0.85f) : new Color(0.25f, 0.1f, 0.12f, 0.85f);

                    var layout = card.AddComponent<VerticalLayoutGroup>();
                    layout.padding = new RectOffset(12, 12, 8, 8);
                    layout.spacing = 4f;

                    GameObject textObj = new GameObject("ReviewText");
                    textObj.transform.SetParent(card.transform, false);
                    var tmp = textObj.AddComponent<TextMeshProUGUI>();
                    tmp.fontSize = 16f;

                    string userAns = (q.SelectedIndex >= 0 && q.SelectedIndex < q.ShuffledOptions.Length)
                        ? q.ShuffledOptions[q.SelectedIndex] : "None";
                    string correctAns = q.ShuffledOptions[q.NewCorrectIndex];

                    string statusText = q.IsCorrect ? "<color=#00FF88>Correct</color>" : "<color=#FF4444>Incorrect</color>";
                    tmp.fontSize = 17f;
                    tmp.text = $"<b>Question {i + 1}</b>\n" +
                               $"<b>Your Answer:</b> {userAns} — <b>[{statusText}]</b>\n" +
                               $"<b>Explanation:</b> <color=#E0E6ED>{q.Explanation}</color>";
                }
            }
        }
    }
}
