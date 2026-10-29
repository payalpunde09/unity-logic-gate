using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DigitalLogicLab.UI;
using DigitalLogicLab.Lab;
using DigitalLogicLab.Theory;
using DigitalLogicLab.Quiz;
using DigitalLogicLab.Input;
using DigitalLogicLab.Logic;

namespace DigitalLogicLab.Core
{
    /// <summary>
    /// Runtime bootstrap that builds the entire UI hierarchy, 3D lab environment,
    /// and manages the exact 4 screens programmatically upon scene load.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        private static bool isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            isInitialized = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnRuntimeMethodLoad()
        {
            if (isInitialized) return;

#if UNITY_2023_1_OR_NEWER
            AppBootstrap existing = FindFirstObjectByType<AppBootstrap>();
#else
            AppBootstrap existing = FindObjectOfType<AppBootstrap>();
#endif
            if (existing == null)
            {
                GameObject bootObj = new GameObject("AppBootstrap");
                bootObj.AddComponent<AppBootstrap>();
            }
        }

        private void Awake()
        {
            if (isInitialized)
            {
                Destroy(gameObject);
                return;
            }
            isInitialized = true;
            BuildApplication();
        }

        private void OnDestroy()
        {
            isInitialized = false;
        }

        private void BuildApplication()
        {
            // 0. Align Main Camera directly at the 3D Workbench and Circuit Board
            Camera cam = Camera.main;
#if UNITY_2023_1_OR_NEWER
            if (cam == null) cam = FindFirstObjectByType<Camera>();
#else
            if (cam == null) cam = FindObjectOfType<Camera>();
#endif
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.75f, -2.5f);
                cam.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f);
            }

            // 1. Ensure EventSystem & Input Module exists
            EventSystem es = null;
#if UNITY_2023_1_OR_NEWER
            es = FindFirstObjectByType<EventSystem>();
#else
            es = FindObjectOfType<EventSystem>();
#endif
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
                var sim = esObj.AddComponent<StandaloneInputModule>();
                sim.forceModuleActive = true;
            }
            else
            {
                var sim = es.GetComponent<BaseInputModule>();
                if (sim == null)
                {
                    var standAlone = es.gameObject.AddComponent<StandaloneInputModule>();
                    standAlone.forceModuleActive = true;
                }
            }

            // 2. Ensure DesktopInputProvider & QuestHandInteractionProvider exist
#if UNITY_2023_1_OR_NEWER
            if (FindFirstObjectByType<DesktopInputProvider>() == null)
#else
            if (FindObjectOfType<DesktopInputProvider>() == null)
#endif
            {
                GameObject inputObj = new GameObject("DesktopInputProvider");
                inputObj.AddComponent<DesktopInputProvider>();
            }

#if UNITY_2023_1_OR_NEWER
            if (FindFirstObjectByType<QuestHandInteractionProvider>() == null)
#else
            if (FindObjectOfType<QuestHandInteractionProvider>() == null)
#endif
            {
                GameObject handObj = new GameObject("QuestHandInteractionProvider");
                handObj.AddComponent<QuestHandInteractionProvider>();
            }

            // 3. Setup Default Lit Material
            Shader defaultShader = Shader.Find("Standard");
            if (defaultShader == null) defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader == null) defaultShader = Shader.Find("Unlit/Color");
            Material defaultMat = new Material(defaultShader);

            // 4. Build 3D Lab Environment
            var lab3D = LabEnvironmentBuilder.BuildLab(defaultMat);

            // 5. Create Root Canvas
            GameObject canvasObj = new GameObject("MainCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
#if UNITY_ANDROID && !UNITY_EDITOR
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 2.0f;
#else
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
#endif
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 6. Build ScreenManager
            GameObject smObj = new GameObject("ScreenManager");
            ScreenManager screenManager = smObj.AddComponent<ScreenManager>();

            // 7. Build Screen 1: Main Menu
            GameObject menuScreen = BuildMenuScreen(canvasObj.transform);

            // 8. Build Screen 2: Theory Screen
            GameObject theoryScreen = BuildTheoryScreen(canvasObj.transform, out TheoryPanel theoryPanel);

            // 9. Build Screen 3: Lab UI (Sidebar layout around 3D lab)
            GameObject labScreen = BuildLabUIScreen(canvasObj.transform, lab3D, out LabController labController);

            // 10. Build Screen 4: Quiz & Results Screen
            GameObject quizScreen = BuildQuizScreen(canvasObj.transform, out QuizManager quizManager);

            // 11. Connect screens to ScreenManager
            screenManager.BindScreens(menuScreen, theoryScreen, labScreen, quizScreen, lab3D.Root);
        }

        // --- SCREEN 1: WELCOME / MAIN MENU ---
        private GameObject BuildMenuScreen(Transform parent)
        {
            GameObject screen = CreateUIPanel("Screen1_Menu", parent, new Color(0.02f, 0.04f, 0.08f, 0.55f));
            var screenImg = screen.GetComponent<Image>();
            if (screenImg != null) screenImg.raycastTarget = false;
            var menuPanel = screen.AddComponent<MenuPanel>();

            // Floating Centered Menu Card
            GameObject menuCard = CreateAnchoredPanel("MenuCard", screen.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 740f), new Color(0.04f, 0.08f, 0.14f, 0.94f));

            // Title & Subtitle Container inside Menu Card
            GameObject header = CreateUIObject("Header", menuCard.transform, new Vector2(0f, 210f), new Vector2(960f, 200f));
            var titleTMP = CreateText(header.transform, "DIGITAL LOGIC GATES VR", 40f, FontStyles.Bold, new Color(0.0f, 0.9f, 1f));
            titleTMP.rectTransform.anchoredPosition = new Vector2(0f, 55f);

            var subTMP = CreateText(header.transform, "Interactive Learning and Simulation of Digital Electronics", 20f, FontStyles.Normal, new Color(0.75f, 0.85f, 0.95f));
            subTMP.rectTransform.anchoredPosition = new Vector2(0f, 8f);

            var introTMP = CreateText(header.transform, "Learn digital logic gates, understand Boolean expressions, experiment with input combinations, and test your knowledge through interactive simulations and quizzes.", 16f, FontStyles.Italic, new Color(0.6f, 0.7f, 0.8f));
            introTMP.rectTransform.anchoredPosition = new Vector2(0f, -48f);
            introTMP.rectTransform.sizeDelta = new Vector2(900f, 60f);

            // Menu Buttons Container inside Menu Card
            GameObject btnContainer = CreateUIObject("Buttons", menuCard.transform, new Vector2(0f, -110f), new Vector2(440f, 420f));
            var layout = btnContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false; // NEVER squash child button heights!
            layout.childControlWidth = false;  // Keep explicit button widths!
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            Button btnStartSim = CreateStyledButton(btnContainer.transform, "START SIMULATION", new Color(0.0f, 0.65f, 0.85f));
            Button btnLab = CreateStyledButton(btnContainer.transform, "PRACTICAL LAB", new Color(0.1f, 0.45f, 0.65f));
            Button btnTheory = CreateStyledButton(btnContainer.transform, "THEORY", new Color(0.12f, 0.35f, 0.55f));
            Button btnQuiz = CreateStyledButton(btnContainer.transform, "QUIZ", new Color(0.15f, 0.4f, 0.5f));
            Button btnHowToUse = CreateStyledButton(btnContainer.transform, "HOW TO USE", new Color(0.18f, 0.25f, 0.35f));
            Button btnExit = CreateStyledButton(btnContainer.transform, "EXIT", new Color(0.55f, 0.15f, 0.15f));

            // How To Use Modal
            GameObject modal = CreateAnchoredPanel("HowToUseModal", screen.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 600f), new Color(0.03f, 0.06f, 0.12f, 0.98f));
            modal.SetActive(false);

            var modalTitle = CreateText(modal.transform, "HOW TO USE THE LAB", 28f, FontStyles.Bold, new Color(0.0f, 0.9f, 1f));
            modalTitle.rectTransform.anchoredPosition = new Vector2(0f, 248f);

            string howToText =
                "1. <b>Open Theory</b> to understand the circuit diagrams, expressions, and universal gate properties.\n\n" +
                "2. <b>Open Practical Lab</b> to select any of the 7 digital logic circuits (AND, OR, NOT, NAND, NOR, MUX, DEMUX).\n\n" +
                "3. <b>Change input switches</b> (A, B, S, D) between 0 and 1 by clicking on them with the mouse.\n\n" +
                "4. <b>Observe the output lamps and truth table</b>: watch the wires illuminate and active rows highlight in real-time.\n\n" +
                "5. <b>Read the circuit explanation</b> and dynamic input/output summaries in the lab info panel.\n\n" +
                "6. <b>Open Quiz</b> to test your knowledge with 5 randomized questions from the question bank.\n\n" +
                "7. <b>Use Retry</b> to begin a new shuffled quiz attempt with comprehensive review.";

            var modalBody = CreateText(modal.transform, howToText, 18.5f, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f));
            modalBody.alignment = TextAlignmentOptions.TopLeft;
            modalBody.rectTransform.pivot = new Vector2(0.5f, 1f);
            modalBody.rectTransform.anchoredPosition = new Vector2(0f, 215f);
            modalBody.rectTransform.sizeDelta = new Vector2(800f, 420f);
            modalBody.lineSpacing = -2f;
            modalBody.paragraphSpacing = 8f;

            Button btnCloseModal = CreateStyledButton(modal.transform, "CLOSE", new Color(0.0f, 0.5f, 0.7f), new Vector2(210f, 46f), 18f);
            btnCloseModal.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -250f);

            // Wire up MenuPanel cleanly
            menuPanel.Initialize(modal, btnStartSim, btnLab, btnTheory, btnQuiz, btnHowToUse, btnCloseModal, btnExit);

            return screen;
        }

        // --- SCREEN 2: THEORY ---
        private GameObject BuildTheoryScreen(Transform parent, out TheoryPanel theoryPanel)
        {
            GameObject screen = CreateUIPanel("Screen2_Theory", parent, new Color(0.05f, 0.08f, 0.14f, 0.98f));
            theoryPanel = screen.AddComponent<TheoryPanel>();

            // Header bar
            GameObject topBar = CreateUIObject("TopBar", screen.transform, new Vector2(0f, 480f), new Vector2(1800f, 70f));
            var topTitle = CreateText(topBar.transform, "LOGIC GATES THEORY & REFERENCE", 28f, FontStyles.Bold, Color.white);
            topTitle.alignment = TextAlignmentOptions.Left;
            topTitle.rectTransform.anchoredPosition = new Vector2(-380f, 0f);

            Button backBtn = CreateStyledButton(topBar.transform, "← MAIN MENU", new Color(0.2f, 0.28f, 0.38f), new Vector2(175f, 44f), 18.5f);
            backBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(380f, 0f);

            Button labBtn = CreateStyledButton(topBar.transform, "PRACTICAL LAB →", new Color(0.0f, 0.55f, 0.75f), new Vector2(195f, 44f), 18.5f);
            labBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(585f, 0f);

            Button quizBtn = CreateStyledButton(topBar.transform, "QUIZ →", new Color(0.12f, 0.45f, 0.6f), new Vector2(145f, 44f), 18.5f);
            quizBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(770f, 0f);

            // Tab Buttons across row
            GameObject tabRow = CreateUIObject("TabRow", screen.transform, new Vector2(0f, 400f), new Vector2(1700f, 50f));
            var tabLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 10f;
            tabLayout.childControlWidth = true;
            tabLayout.childForceExpandWidth = true;

            string[] names = new string[] { "AND", "OR", "NOT", "NAND", "NOR", "MUX", "DEMUX" };
            Button[] tabs = new Button[7];
            for (int i = 0; i < 7; i++)
            {
                tabs[i] = CreateStyledButton(tabRow.transform, names[i], new Color(0.12f, 0.16f, 0.24f), new Vector2(230f, 46f), 19f);
            }

            // Main Content Card (Left: Text details, Right: Truth Table)
            GameObject card = CreateAnchoredPanel("TheoryCard", screen.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1720f, 750f), new Color(0.06f, 0.1f, 0.16f, 0.95f));

            // Card Left Pane (Details)
            GameObject leftPane = CreateUIObject("LeftPane", card.transform, new Vector2(-410f, 0f), new Vector2(860f, 680f));
            var cardTitle = CreateText(leftPane.transform, "Circuit Name", 35f, FontStyles.Bold, new Color(0.0f, 0.95f, 1f));
            cardTitle.alignment = TextAlignmentOptions.TopLeft;
            cardTitle.rectTransform.anchoredPosition = new Vector2(0f, 310f);
            cardTitle.rectTransform.sizeDelta = new Vector2(860f, 42f);

            var exprText = CreateText(leftPane.transform, "Y = Expression", 24f, FontStyles.Normal, Color.white);
            exprText.alignment = TextAlignmentOptions.TopLeft;
            exprText.rectTransform.anchoredPosition = new Vector2(0f, 260f);
            exprText.rectTransform.sizeDelta = new Vector2(860f, 38f);

            var explText = CreateText(leftPane.transform, "Explanation", 21f, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f));
            explText.alignment = TextAlignmentOptions.TopLeft;
            explText.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            explText.rectTransform.sizeDelta = new Vector2(860f, 160f);

            var ioText = CreateText(leftPane.transform, "Inputs/Outputs description", 21f, FontStyles.Normal, new Color(0.8f, 0.85f, 0.9f));
            ioText.alignment = TextAlignmentOptions.TopLeft;
            ioText.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            ioText.rectTransform.sizeDelta = new Vector2(860f, 100f);

            var appText = CreateText(leftPane.transform, "Practical application", 21f, FontStyles.Normal, new Color(0.6f, 0.95f, 0.8f));
            appText.alignment = TextAlignmentOptions.TopLeft;
            appText.rectTransform.anchoredPosition = new Vector2(0f, -105f);
            appText.rectTransform.sizeDelta = new Vector2(860f, 115f);

            var noteText = CreateText(leftPane.transform, "Special Note", 21f, FontStyles.Normal, new Color(1f, 0.85f, 0.3f));
            noteText.alignment = TextAlignmentOptions.TopLeft;
            noteText.rectTransform.anchoredPosition = new Vector2(0f, -235f);
            noteText.rectTransform.sizeDelta = new Vector2(860f, 120f);

            // Card Right Pane (Truth Table & Symbol Info)
            GameObject rightPane = CreateUIObject("RightPane", card.transform, new Vector2(450f, 0f), new Vector2(720f, 680f));
            var tableHeader = CreateText(rightPane.transform, "TRUTH TABLE", 26f, FontStyles.Bold, new Color(0.0f, 0.95f, 1f));
            tableHeader.alignment = TextAlignmentOptions.TopLeft;
            tableHeader.rectTransform.anchoredPosition = new Vector2(0f, 310f);

            var tableBody = CreateText(rightPane.transform, "Table data", 22f, FontStyles.Normal, new Color(0.9f, 0.95f, 1f));
            tableBody.alignment = TextAlignmentOptions.TopLeft;
            tableBody.rectTransform.anchoredPosition = new Vector2(0f, 85f);
            tableBody.rectTransform.sizeDelta = new Vector2(700f, 450f);

            theoryPanel.Initialize(cardTitle, exprText, ioText, explText, appText, noteText, tableBody, tabs, backBtn, labBtn, quizBtn);
            return screen;
        }

        // --- SCREEN 3: PRACTICAL LAB UI (Unobtrusive sidebars around 3D lab) ---
        private GameObject BuildLabUIScreen(Transform parent, LabEnvironmentBuilder.LabStructure lab3D, out LabController labController)
        {
            GameObject screen = CreateUIPanel("Screen3_LabUI", parent, Color.clear);
            labController = screen.AddComponent<LabController>();

            // Top Navigation Bar (Positioned cleanly at top with center pivot)
            GameObject topBar = CreateAnchoredPanel("TopBar", screen.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(1880f, 64f), new Color(0.03f, 0.06f, 0.12f, 0.94f));

            var topTitle = CreateText(topBar.transform, "DIGITAL LOGIC GATES VR LAB", 24f, FontStyles.Bold, new Color(0.0f, 0.9f, 1f));
            topTitle.alignment = TextAlignmentOptions.Left;
            topTitle.rectTransform.anchoredPosition = new Vector2(-460f, 0f);

            Button menuBtn = CreateStyledButton(topBar.transform, "← MENU", new Color(0.18f, 0.25f, 0.35f), new Vector2(140f, 44f), 18f);
            menuBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(240f, 0f);

            Button theoryBtn = CreateStyledButton(topBar.transform, "THEORY", new Color(0.12f, 0.35f, 0.5f), new Vector2(140f, 44f), 18f);
            theoryBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(400f, 0f);

            Button quizBtn = CreateStyledButton(topBar.transform, "NEXT: QUIZ →", new Color(0.0f, 0.55f, 0.7f), new Vector2(170f, 44f), 18f);
            quizBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(580f, 0f);

            Button resetBtn = CreateStyledButton(topBar.transform, "RESET INPUTS", new Color(0.7f, 0.25f, 0.1f), new Vector2(170f, 44f), 18f);
            resetBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(770f, 0f);

            // Left Sidebar: Circuit Selector & Controls (Docked cleanly on the left)
            GameObject leftSidebar = CreateAnchoredPanel("LeftSidebar", screen.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -20f), new Vector2(340f, 950f), new Color(0.04f, 0.08f, 0.14f, 0.92f));

            var selHeader = CreateText(leftSidebar.transform, "SELECT CIRCUIT (1-7)", 18.5f, FontStyles.Bold, new Color(0.0f, 0.9f, 1f));
            selHeader.rectTransform.anchoredPosition = new Vector2(0f, 435f);

            string[] gateNames = new string[] { "1. AND Gate", "2. OR Gate", "3. NOT Gate", "4. NAND Gate", "5. NOR Gate", "6. 2:1 MUX", "7. 1:2 DEMUX" };
            Button[] selectorButtons = new Button[7];
            for (int i = 0; i < 7; i++)
            {
                Button btn = CreateStyledButton(leftSidebar.transform, gateNames[i], new Color(0.12f, 0.18f, 0.28f), new Vector2(290f, 46f), 18f);
                btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 385f - (i * 54f));
                selectorButtons[i] = btn;
            }

            // Quick Toggle UI in sidebar
            var togglesHeader = CreateText(leftSidebar.transform, "INTERACTIVE INPUTS", 18.5f, FontStyles.Bold, Color.white);
            togglesHeader.rectTransform.anchoredPosition = new Vector2(0f, -40f);

            var toggleInstructions = CreateText(leftSidebar.transform, "Click 3D switches directly or use buttons below:", 13f, FontStyles.Italic, new Color(0.7f, 0.75f, 0.85f));
            toggleInstructions.rectTransform.anchoredPosition = new Vector2(0f, -75f);
            toggleInstructions.rectTransform.sizeDelta = new Vector2(300f, 36f);

            Button toggleA = CreateStyledButton(leftSidebar.transform, "TOGGLE A: 0", new Color(0.15f, 0.22f, 0.32f), new Vector2(290f, 48f), 17.5f);
            toggleA.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -135f);
            var toggleTextA = toggleA.GetComponentInChildren<TextMeshProUGUI>();
            var toggleBgA = toggleA.GetComponent<Image>();

            Button toggleB = CreateStyledButton(leftSidebar.transform, "TOGGLE B: 0", new Color(0.15f, 0.22f, 0.32f), new Vector2(290f, 48f), 17.5f);
            toggleB.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -195f);
            var toggleTextB = toggleB.GetComponentInChildren<TextMeshProUGUI>();
            var toggleBgB = toggleB.GetComponent<Image>();

            Button toggleS = CreateStyledButton(leftSidebar.transform, "SELECT S: 0", new Color(0.15f, 0.22f, 0.32f), new Vector2(290f, 48f), 17.5f);
            toggleS.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -255f);
            var toggleTextS = toggleS.GetComponentInChildren<TextMeshProUGUI>();
            var toggleBgS = toggleS.GetComponent<Image>();

            // Right Sidebar: Truth Table, Technical Info & ON-SCREEN LAMP INDICATOR
            GameObject rightSidebar = CreateAnchoredPanel("RightSidebar", screen.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, -20f), new Vector2(380f, 950f), new Color(0.04f, 0.08f, 0.14f, 0.92f));

            var cTitle = CreateText(rightSidebar.transform, "AND Gate", 26f, FontStyles.Bold, new Color(0.0f, 0.95f, 1f));
            cTitle.rectTransform.anchoredPosition = new Vector2(0f, 430f);

            var cExpr = CreateText(rightSidebar.transform, "Expression: Y = A · B", 20f, FontStyles.Normal, Color.white);
            cExpr.rectTransform.anchoredPosition = new Vector2(0f, 385f);

            var cStatus = CreateText(rightSidebar.transform, "Inputs: A=0 B=0\nOutputs: Y=0", 18f, FontStyles.Bold, new Color(0.0f, 1f, 0.8f));
            cStatus.rectTransform.anchoredPosition = new Vector2(0f, 325f);
            cStatus.rectTransform.sizeDelta = new Vector2(345f, 55f);

            // Large On-Screen Output Lamp Indicators - White text without ON/OFF wording
            GameObject lampYBox = CreateAnchoredPanel("LampYIndicator", rightSidebar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 265f), new Vector2(345f, 48f), new Color(0.12f, 0.16f, 0.22f));
            var lYBg = lampYBox.GetComponent<Image>();
            var lYTxt = CreateText(lampYBox.transform, "OUTPUT LIGHT (Y): 0", 15.5f, FontStyles.Bold, Color.white);

            GameObject lampDemuxBox = CreateAnchoredPanel("LampDemuxIndicator", rightSidebar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 265f), new Vector2(345f, 82f), Color.clear);
            lampDemuxBox.SetActive(false);

            GameObject y0Box = CreateAnchoredPanel("Y0Box", lampDemuxBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(345f, 36f), new Color(0.12f, 0.16f, 0.22f));
            var lY0Bg = y0Box.GetComponent<Image>();
            var lY0Txt = CreateText(y0Box.transform, "LIGHT Y1: 0", 14.5f, FontStyles.Bold, Color.white);

            GameObject y1Box = CreateAnchoredPanel("Y1Box", lampDemuxBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(345f, 36f), new Color(0.12f, 0.16f, 0.22f));
            var lY1Bg = y1Box.GetComponent<Image>();
            var lY1Txt = CreateText(y1Box.transform, "LIGHT Y2: 0", 14.5f, FontStyles.Bold, Color.white);

            // Truth Table Header & Container - Spaced safely to prevent overlap and keep MUX table inside panel
            var ttTitle = CreateText(rightSidebar.transform, "LIVE TRUTH TABLE", 20f, FontStyles.Bold, Color.white);
            ttTitle.rectTransform.anchoredPosition = new Vector2(0f, 182f);

            GameObject tableHeaderObj = CreateAnchoredPanel("TableHeader", rightSidebar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 142f), new Vector2(350f, 38f), new Color(0.1f, 0.15f, 0.22f, 0.95f));
            var headerTmp = CreateText(tableHeaderObj.transform, "A   |   B   |   Y", 20f, FontStyles.Bold, new Color(0.0f, 0.95f, 1f));
            headerTmp.enableWordWrapping = false;

            GameObject tableContentObj = CreateUIObject("TableContent", rightSidebar.transform, Vector2.zero, new Vector2(350f, 380f));
            var tableRt = tableContentObj.GetComponent<RectTransform>();
            tableRt.anchorMin = new Vector2(0.5f, 0.5f);
            tableRt.anchorMax = new Vector2(0.5f, 0.5f);
            tableRt.pivot = new Vector2(0.5f, 1f);
            tableRt.anchoredPosition = new Vector2(0f, 118f);

            var tableLayout = tableContentObj.AddComponent<VerticalLayoutGroup>();
            tableLayout.spacing = 3f;
            tableLayout.childControlHeight = true;
            tableLayout.childForceExpandHeight = false;
            tableLayout.childControlWidth = true;
            tableLayout.childForceExpandWidth = true;

            labController.Initialize(
                lab3D.SwitchA, lab3D.SwitchB, lab3D.SwitchC,
                lab3D.LampY, lab3D.LampY0, lab3D.LampY1,
                lab3D.SymbolRenderer,
                cTitle, cExpr, null, cStatus,
                tableHeaderObj.transform, tableContentObj.transform,
                resetBtn, theoryBtn, quizBtn, menuBtn,
                selectorButtons,
                toggleA, toggleTextA, toggleBgA,
                toggleB, toggleTextB, toggleBgB,
                toggleS, toggleTextS, toggleBgS,
                lampYBox, lYBg, lYTxt,
                lampDemuxBox, lY0Bg, lY0Txt, lY1Bg, lY1Txt
            );

            return screen;
        }

        // --- SCREEN 4: QUIZ AND RESULTS ---
        private GameObject BuildQuizScreen(Transform parent, out QuizManager quizManager)
        {
            GameObject screen = CreateUIPanel("Screen4_Quiz", parent, new Color(0.04f, 0.07f, 0.12f, 0.98f));
            quizManager = screen.AddComponent<QuizManager>();

            // Top Bar
            GameObject topBar = CreateUIObject("TopBar", screen.transform, new Vector2(0f, 480f), new Vector2(1800f, 60f));
            var topTitle = CreateText(topBar.transform, "KNOWLEDGE CHECK QUIZ", 28f, FontStyles.Bold, new Color(0.0f, 0.9f, 1f));
            topTitle.alignment = TextAlignmentOptions.Left;
            topTitle.rectTransform.anchoredPosition = new Vector2(-450f, 0f);

            Button topMenuBtn = CreateStyledButton(topBar.transform, "← MAIN MENU", new Color(0.2f, 0.28f, 0.38f), new Vector2(175f, 44f), 18.5f);
            topMenuBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(540f, 0f);

            Button topLabBtn = CreateStyledButton(topBar.transform, "PRACTICAL LAB →", new Color(0.0f, 0.5f, 0.7f), new Vector2(195f, 44f), 18.5f);
            topLabBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(740f, 0f);

            // 1. Start View
            GameObject startView = CreateAnchoredPanel("StartView", screen.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(920f, 540f), new Color(0.06f, 0.1f, 0.16f, 0.95f));

            var sTitle = CreateText(startView.transform, "DIGITAL LOGIC KNOWLEDGE ASSESSMENT", 26f, FontStyles.Bold, Color.white);
            sTitle.rectTransform.anchoredPosition = new Vector2(0f, 195f);

            string sInstructions =
                "• Exactly <b>5 questions</b> will be selected at random from the 10-question master bank.\n\n" +
                "• Options are shuffled for every attempt.\n\n" +
                "• 1 mark per correct answer (Maximum score: 5 / 100%).\n\n" +
                "• Once an answer is submitted, the question is locked.\n\n" +
                "• Immediate explanations and a detailed performance breakdown will follow completion.";

            var sBody = CreateText(startView.transform, sInstructions, 18.5f, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f));
            sBody.alignment = TextAlignmentOptions.TopLeft;
            sBody.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            sBody.rectTransform.sizeDelta = new Vector2(800f, 260f);

            Button startQuizBtn = CreateStyledButton(startView.transform, "START QUIZ NOW", new Color(0.0f, 0.65f, 0.85f), new Vector2(260f, 50f), 19.5f);
            startQuizBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -190f);

            // 2. Question View
            GameObject questionView = CreateAnchoredPanel("QuestionView", screen.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1020f, 640f), new Color(0.06f, 0.1f, 0.16f, 0.95f));
            questionView.SetActive(false);

            var progText = CreateText(questionView.transform, "Question 1 of 5", 21f, FontStyles.Bold, new Color(0.0f, 0.9f, 1f));
            progText.rectTransform.anchoredPosition = new Vector2(0f, 260f);

            var qPrompt = CreateText(questionView.transform, "Question prompt goes here...", 23f, FontStyles.Normal, Color.white);
            qPrompt.rectTransform.anchoredPosition = new Vector2(0f, 195f);
            qPrompt.rectTransform.sizeDelta = new Vector2(920f, 85f);

            Button[] optBtns = new Button[4];
            TextMeshProUGUI[] optTxts = new TextMeshProUGUI[4];
            Image[] optBgs = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                Button btn = CreateStyledButton(questionView.transform, $"Option {i + 1}", new Color(0.12f, 0.16f, 0.22f, 0.85f), new Vector2(920f, 52f), 18.5f);
                btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 95f - (i * 62f));
                optBtns[i] = btn;
                optTxts[i] = btn.GetComponentInChildren<TextMeshProUGUI>();
                optTxts[i].alignment = TextAlignmentOptions.Left;
                optTxts[i].margin = new Vector4(20f, 0f, 0f, 0f);
                optBgs[i] = btn.GetComponent<Image>();
            }

            var feedbackNotice = CreateText(questionView.transform, "Select an option and submit.", 16.5f, FontStyles.Italic, new Color(0.7f, 0.75f, 0.85f));
            feedbackNotice.rectTransform.anchoredPosition = new Vector2(0f, -165f);
            feedbackNotice.rectTransform.sizeDelta = new Vector2(900f, 40f);

            Button submitBtn = CreateStyledButton(questionView.transform, "SUBMIT ANSWER", new Color(0.0f, 0.6f, 0.8f), new Vector2(260f, 48f), 18.5f);
            submitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -225f);

            Button nextBtn = CreateStyledButton(questionView.transform, "NEXT QUESTION →", new Color(0.1f, 0.65f, 0.35f), new Vector2(260f, 48f), 18.5f);
            nextBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -225f);
            nextBtn.gameObject.SetActive(false);

            // 3. Results View (Strictly inside Screen 4)
            GameObject resultsView = CreateAnchoredPanel("ResultsView", screen.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1100f, 750f), new Color(0.06f, 0.09f, 0.15f, 0.95f));
            resultsView.SetActive(false);

            var rTitle = CreateText(resultsView.transform, "QUIZ PERFORMANCE SUMMARY", 28f, FontStyles.Bold, Color.white);
            rTitle.rectTransform.anchoredPosition = new Vector2(0f, 325f);

            var rScore = CreateText(resultsView.transform, "5 / 5", 44f, FontStyles.Bold, new Color(0.0f, 0.95f, 1f));
            rScore.rectTransform.anchoredPosition = new Vector2(-250f, 255f);

            var rPerc = CreateText(resultsView.transform, "100%", 44f, FontStyles.Bold, new Color(0.0f, 1f, 0.7f));
            rPerc.rectTransform.anchoredPosition = new Vector2(250f, 255f);

            var rBreakdown = CreateText(resultsView.transform, "Correct: 5 | Incorrect: 0", 19f, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f));
            rBreakdown.rectTransform.anchoredPosition = new Vector2(0f, 195f);

            // Review Scroll Area
            GameObject reviewScrollObj = CreateUIObject("ReviewScroll", resultsView.transform, new Vector2(0f, -10f), new Vector2(1020f, 360f));
            var scrollLayout = reviewScrollObj.AddComponent<VerticalLayoutGroup>();
            scrollLayout.spacing = 8f;
            scrollLayout.childControlHeight = true;
            scrollLayout.childControlWidth = true;
            scrollLayout.childForceExpandHeight = false;

            // Action Buttons
            Button retryBtn = CreateStyledButton(resultsView.transform, "RETRY QUIZ", new Color(0.0f, 0.6f, 0.8f), new Vector2(220f, 46f), 18f);
            retryBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-260f, -320f);

            Button resLabBtn = CreateStyledButton(resultsView.transform, "BACK TO LAB", new Color(0.12f, 0.45f, 0.6f), new Vector2(220f, 46f), 18f);
            resLabBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -320f);

            Button resMenuBtn = CreateStyledButton(resultsView.transform, "MAIN MENU", new Color(0.2f, 0.28f, 0.38f), new Vector2(220f, 46f), 18f);
            resMenuBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(260f, -320f);

            quizManager.Initialize(
                startView, questionView, resultsView,
                startQuizBtn,
                progText, qPrompt,
                optBtns, optTxts, optBgs,
                submitBtn, nextBtn, feedbackNotice,
                rScore, rPerc, rBreakdown,
                reviewScrollObj.transform, retryBtn, resLabBtn, resMenuBtn,
                topMenuBtn, topLabBtn
            );

            return screen;
        }

        // --- UI Construction Utilities ---

        private static GameObject CreateUIPanel(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = (color.a > 0.01f); // Transparent panels must NEVER block raycasts!
            return obj;
        }

        private static GameObject CreateAnchoredPanel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = (color.a > 0.01f);
            return obj;
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return obj;
        }

        private static TMP_FontAsset runtimeFontAsset;

        public static TMP_FontAsset GetDefaultFont()
        {
            if (runtimeFontAsset != null) return runtimeFontAsset;

            try
            {
                if (TMP_Settings.defaultFontAsset != null)
                {
                    runtimeFontAsset = TMP_Settings.defaultFontAsset;
                    return runtimeFontAsset;
                }
            }
            catch { }

            try
            {
                runtimeFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (runtimeFontAsset != null) return runtimeFontAsset;
            }
            catch { }

            try
            {
                Font osFont = Font.CreateDynamicFontFromOSFont("Arial", 36);
                if (osFont != null)
                {
                    runtimeFontAsset = TMP_FontAsset.CreateFontAsset(osFont);
                    if (runtimeFontAsset != null) return runtimeFontAsset;
                }
            }
            catch { }

            return null;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string content, float fontSize, FontStyles style, Color color)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600f, 60f);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            var f = GetDefaultFont();
            if (f != null) tmp.font = f;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // CRITICAL: Labels must NEVER block button or 3D clicks!
            return tmp;
        }

        private static Button CreateStyledButton(Transform parent, string label, Color bgColor, Vector2? size = null, float fontSize = 20f)
        {
            Vector2 btnSize = size.HasValue ? size.Value : new Vector2(380f, 52f);
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "_")}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = btnSize;

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minWidth = btnSize.x;
            le.preferredWidth = btnSize.x;
            le.minHeight = btnSize.y;
            le.preferredHeight = btnSize.y;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 0f;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.35f;
            cb.pressedColor = bgColor * 0.75f;
            cb.selectedColor = bgColor * 1.2f;
            cb.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            var f = GetDefaultFont();
            if (f != null) tmp.font = f;
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // Never block click raycasts to button!

            return btn;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
