using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DigitalLogicLab.UI
{
    /// <summary>
    /// Controls Screen 1 (Welcome / Main Menu)
    /// Features: Title, Subtitle, Intro, Buttons for Theory, Practical Lab, Quiz, Start Simulation, How To Use popup, and Exit.
    /// Palette: Light blue, white, navy blue, with circuit accents.
    /// </summary>
    public class MenuPanel : MonoBehaviour
    {
        [Header("Modal Windows")]
        [SerializeField] private GameObject howToUseModal;

        [Header("Menu Buttons")]
        [SerializeField] private Button theoryButton;
        [SerializeField] private Button labButton;
        [SerializeField] private Button startSimButton;
        [SerializeField] private Button quizButton;
        [SerializeField] private Button howToUseButton;
        [SerializeField] private Button closeHowToUseButton;
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            BindButtons();
        }

        public void Initialize(GameObject modal, Button simBtn, Button lBtn, Button tBtn, Button qBtn, Button howBtn, Button closeHowBtn, Button exitBtn)
        {
            howToUseModal = modal;
            startSimButton = simBtn;
            labButton = lBtn;
            theoryButton = tBtn;
            quizButton = qBtn;
            howToUseButton = howBtn;
            closeHowToUseButton = closeHowBtn;
            exitButton = exitBtn;

            BindButtons();
        }

        private void BindButtons()
        {
            if (howToUseModal != null)
            {
                howToUseModal.SetActive(false);
            }

            if (theoryButton != null)
            {
                theoryButton.onClick.RemoveAllListeners();
                theoryButton.onClick.AddListener(() => ScreenManager.Instance.OpenTheory());
            }

            if (labButton != null)
            {
                labButton.onClick.RemoveAllListeners();
                labButton.onClick.AddListener(() => ScreenManager.Instance.OpenLab());
            }

            if (startSimButton != null)
            {
                startSimButton.onClick.RemoveAllListeners();
                startSimButton.onClick.AddListener(() => ScreenManager.Instance.OpenLab());
            }

            if (quizButton != null)
            {
                quizButton.onClick.RemoveAllListeners();
                quizButton.onClick.AddListener(() => ScreenManager.Instance.OpenQuiz());
            }

            if (howToUseButton != null)
            {
                howToUseButton.onClick.RemoveAllListeners();
                howToUseButton.onClick.AddListener(ShowHowToUse);
            }

            if (closeHowToUseButton != null)
            {
                closeHowToUseButton.onClick.RemoveAllListeners();
                closeHowToUseButton.onClick.AddListener(HideHowToUse);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(ExitApplication);
            }
        }

        public void ShowHowToUse()
        {
            if (howToUseModal != null)
            {
                howToUseModal.SetActive(true);
            }
        }

        public void HideHowToUse()
        {
            if (howToUseModal != null)
            {
                howToUseModal.SetActive(false);
            }
        }

        public void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
