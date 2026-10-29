using UnityEngine;
using DigitalLogicLab.Input;

namespace DigitalLogicLab.UI
{
    public enum ScreenType
    {
        Menu = 0,
        Theory = 1,
        Lab = 2,
        Quiz = 3
    }

    /// <summary>
    /// Manages the four primary screens: Menu, Theory, Lab, and Quiz+Results.
    /// Strictly limits the application to exactly 4 screens with results hosted in Screen 4.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("Screen Panels")]
        [SerializeField] private GameObject menuScreen;
        [SerializeField] private GameObject theoryScreen;
        [SerializeField] private GameObject labScreen;
        [SerializeField] private GameObject quizScreen;

        [Header("3D Lab Root")]
        [SerializeField] private GameObject lab3DRoot;

        public ScreenType CurrentScreen { get; private set; } = ScreenType.Menu;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            ShowScreen(ScreenType.Menu);
        }

        private void Update()
        {
            IInputProvider input = DesktopInputProvider.Instance;
            if (input != null && input.GetMenuRequested())
            {
                if (CurrentScreen != ScreenType.Menu)
                {
                    ShowScreen(ScreenType.Menu);
                }
            }
        }

        public void BindScreens(GameObject menu, GameObject theory, GameObject lab, GameObject quiz, GameObject lab3D)
        {
            menuScreen = menu;
            theoryScreen = theory;
            labScreen = lab;
            quizScreen = quiz;
            lab3DRoot = lab3D;

            ShowScreen(CurrentScreen);
        }

        public void ShowScreen(ScreenType screen)
        {
            CurrentScreen = screen;

            if (menuScreen != null) menuScreen.SetActive(screen == ScreenType.Menu);
            if (theoryScreen != null) theoryScreen.SetActive(screen == ScreenType.Theory);
            if (labScreen != null) labScreen.SetActive(screen == ScreenType.Lab);
            if (quizScreen != null) quizScreen.SetActive(screen == ScreenType.Quiz);

            // 3D lab environment remains permanently active as the core visual anchor
            if (lab3DRoot != null)
            {
                lab3DRoot.SetActive(true);
            }
        }

        public void OpenMenu() => ShowScreen(ScreenType.Menu);
        public void OpenTheory() => ShowScreen(ScreenType.Theory);
        public void OpenLab() => ShowScreen(ScreenType.Lab);
        public void OpenQuiz() => ShowScreen(ScreenType.Quiz);
    }
}
