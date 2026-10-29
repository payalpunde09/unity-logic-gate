using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DigitalLogicLab.Logic;
using DigitalLogicLab.UI;
using DigitalLogicLab.Input;

namespace DigitalLogicLab.Lab
{
    /// <summary>
    /// Coordinates the practical lab: routes switch events to LogicEngine,
    /// drives physical lamps and wires, and keeps the truth table, info panels,
    /// and on-screen lamp indicators dynamically updated.
    /// </summary>
    public class LabController : MonoBehaviour
    {
        [Header("3D Lab References")]
        [SerializeField] private InputSwitch switchA;
        [SerializeField] private InputSwitch switchB;
        [SerializeField] private InputSwitch switchS;
        [SerializeField] private OutputLamp lampY;
        [SerializeField] private OutputLamp lampY0;
        [SerializeField] private OutputLamp lampY1;
        [SerializeField] private CircuitSymbolRenderer symbolRenderer;

        [Header("Lab UI Panels")]
        [SerializeField] private TextMeshProUGUI circuitTitleText;
        [SerializeField] private TextMeshProUGUI booleanExprText;
        [SerializeField] private TextMeshProUGUI explanationText;
        [SerializeField] private TextMeshProUGUI statusSummaryText;

        [Header("On-Screen Lamp Indicators")]
        [SerializeField] private GameObject uiLampYContainer;
        [SerializeField] private Image uiLampYBg;
        [SerializeField] private TextMeshProUGUI uiLampYText;

        [SerializeField] private GameObject uiLampDemuxContainer;
        [SerializeField] private Image uiLampY0Bg;
        [SerializeField] private TextMeshProUGUI uiLampY0Text;
        [SerializeField] private Image uiLampY1Bg;
        [SerializeField] private TextMeshProUGUI uiLampY1Text;

        [Header("Sidebar Input Toggle Buttons")]
        [SerializeField] private Button toggleBtnA;
        [SerializeField] private TextMeshProUGUI toggleTextA;
        [SerializeField] private Image toggleBgA;

        [SerializeField] private Button toggleBtnB;
        [SerializeField] private TextMeshProUGUI toggleTextB;
        [SerializeField] private Image toggleBgB;

        [SerializeField] private Button toggleBtnS;
        [SerializeField] private TextMeshProUGUI toggleTextS;
        [SerializeField] private Image toggleBgS;

        [Header("Truth Table UI")]
        [SerializeField] private Transform truthTableHeaderRow;
        [SerializeField] private Transform truthTableContent;

        [Header("Navigation Buttons")]
        [SerializeField] private Button btnResetInputs;
        [SerializeField] private Button btnBackToTheory;
        [SerializeField] private Button btnNextQuiz;
        [SerializeField] private Button btnMainMenu;

        [Header("Circuit Selector Buttons")]
        [SerializeField] private Button[] circuitSelectorButtons;

        private CircuitType currentCircuit = CircuitType.AND;
        private List<GameObject> spawnedTableRows = new List<GameObject>();
        private List<TruthTableRow> currentTableRows = new List<TruthTableRow>();

        private readonly Color buttonActiveColor = new Color(0.0f, 0.75f, 0.95f);
        private readonly Color buttonInactiveColor = new Color(0.12f, 0.18f, 0.28f);

        private readonly Color inputLowColor = new Color(0.15f, 0.22f, 0.32f);
        private readonly Color inputHighColor = new Color(0.0f, 0.85f, 1.0f);

        private readonly Color lampOffBgColor = new Color(0.12f, 0.16f, 0.22f);
        private readonly Color lampOnBgColor = new Color(0.0f, 1.0f, 0.45f); // Vibrant glowing neon green

        public CircuitType CurrentCircuit => currentCircuit;

        public void Initialize(
            InputSwitch swA, InputSwitch swB, InputSwitch swS,
            OutputLamp lY, OutputLamp lY0, OutputLamp lY1,
            CircuitSymbolRenderer symRenderer,
            TextMeshProUGUI title, TextMeshProUGUI expr, TextMeshProUGUI expl, TextMeshProUGUI status,
            Transform tableHeader, Transform tableContent,
            Button resetBtn, Button theoryBtn, Button quizBtn, Button menuBtn,
            Button[] selectorButtons,
            Button tBtnA, TextMeshProUGUI tTxtA, Image tBgA,
            Button tBtnB, TextMeshProUGUI tTxtB, Image tBgB,
            Button tBtnS, TextMeshProUGUI tTxtS, Image tBgS,
            GameObject lampYBox, Image lYBg, TextMeshProUGUI lYTxt,
            GameObject lampDemuxBox, Image lY0Bg, TextMeshProUGUI lY0Txt, Image lY1Bg, TextMeshProUGUI lY1Txt)
        {
            switchA = swA;
            switchB = swB;
            switchS = swS;
            lampY = lY;
            lampY0 = lY0;
            lampY1 = lY1;
            symbolRenderer = symRenderer;

            circuitTitleText = title;
            booleanExprText = expr;
            explanationText = expl;
            statusSummaryText = status;

            truthTableHeaderRow = tableHeader;
            truthTableContent = tableContent;

            btnResetInputs = resetBtn;
            btnBackToTheory = theoryBtn;
            btnNextQuiz = quizBtn;
            btnMainMenu = menuBtn;
            circuitSelectorButtons = selectorButtons;

            toggleBtnA = tBtnA;
            toggleTextA = tTxtA;
            toggleBgA = tBgA;

            toggleBtnB = tBtnB;
            toggleTextB = tTxtB;
            toggleBgB = tBgB;

            toggleBtnS = tBtnS;
            toggleTextS = tTxtS;
            toggleBgS = tBgS;

            uiLampYContainer = lampYBox;
            uiLampYBg = lYBg;
            uiLampYText = lYTxt;

            uiLampDemuxContainer = lampDemuxBox;
            uiLampY0Bg = lY0Bg;
            uiLampY0Text = lY0Txt;
            uiLampY1Bg = lY1Bg;
            uiLampY1Text = lY1Txt;

            // Subscribe switch change callbacks
            if (switchA != null) switchA.OnValueChanged += OnSwitchToggled;
            if (switchB != null) switchB.OnValueChanged += OnSwitchToggled;
            if (switchS != null) switchS.OnValueChanged += OnSwitchToggled;

            // UI Button binds
            if (btnResetInputs != null) btnResetInputs.onClick.AddListener(ResetInputs);
            if (btnBackToTheory != null) btnBackToTheory.onClick.AddListener(() => ScreenManager.Instance.OpenTheory());
            if (btnNextQuiz != null) btnNextQuiz.onClick.AddListener(() => ScreenManager.Instance.OpenQuiz());
            if (btnMainMenu != null) btnMainMenu.onClick.AddListener(() => ScreenManager.Instance.OpenMenu());

            // Sidebar toggle buttons
            if (toggleBtnA != null) toggleBtnA.onClick.AddListener(() => { if (switchA != null) switchA.Toggle(); });
            if (toggleBtnB != null) toggleBtnB.onClick.AddListener(() => { if (switchB != null) switchB.Toggle(); });
            if (toggleBtnS != null) toggleBtnS.onClick.AddListener(() => { if (switchS != null) switchS.Toggle(); });

            // Circuit Selector Buttons
            if (circuitSelectorButtons != null && circuitSelectorButtons.Length == 7)
            {
                circuitSelectorButtons[0].onClick.AddListener(() => SelectCircuit(CircuitType.AND));
                circuitSelectorButtons[1].onClick.AddListener(() => SelectCircuit(CircuitType.OR));
                circuitSelectorButtons[2].onClick.AddListener(() => SelectCircuit(CircuitType.NOT));
                circuitSelectorButtons[3].onClick.AddListener(() => SelectCircuit(CircuitType.NAND));
                circuitSelectorButtons[4].onClick.AddListener(() => SelectCircuit(CircuitType.NOR));
                circuitSelectorButtons[5].onClick.AddListener(() => SelectCircuit(CircuitType.MUX));
                circuitSelectorButtons[6].onClick.AddListener(() => SelectCircuit(CircuitType.DEMUX));
            }

            SelectCircuit(CircuitType.AND);
        }

        private void Update()
        {
            if (ScreenManager.Instance == null || ScreenManager.Instance.CurrentScreen != ScreenType.Lab) return;

            IInputProvider input = DesktopInputProvider.Instance;
            if (input == null) return;

            // Shortcut keys 1-7 for circuits
            int shortcut = input.GetNumericShortcut();
            if (shortcut >= 1 && shortcut <= 7)
            {
                SelectCircuit((CircuitType)(shortcut - 1));
            }

            // Raycast switch interaction with mouse click (only when NOT clicking a UI button)
            if (input.GetSelectDown())
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return; // Ignore mouse click if pointer is over UI element
                }

                Ray ray = input.GetPointerRay(Camera.main);
                if (Physics.Raycast(ray, out RaycastHit hit, 50f))
                {
                    var interactable = hit.collider.GetComponentInParent<IInteractable>();
                    if (interactable != null && interactable.IsInteractable())
                    {
                        interactable.Interact();
                    }
                }
            }
        }

        public void SelectCircuit(CircuitType circuit)
        {
            currentCircuit = circuit;

            // Reset inputs to avoid stale states
            if (switchA != null) switchA.SetState(0, false);
            if (switchB != null) switchB.SetState(0, false);
            if (switchS != null) switchS.SetState(0, false);

            HighlightSelectorButtons();
            ConfigureSwitchesAndLamps();
            BuildTruthTableUI();
            Recalculate();
        }

        private void HighlightSelectorButtons()
        {
            if (circuitSelectorButtons == null) return;

            for (int i = 0; i < circuitSelectorButtons.Length; i++)
            {
                var img = circuitSelectorButtons[i].GetComponent<Image>();
                var txt = circuitSelectorButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                bool isSelected = (i == (int)currentCircuit);

                if (img != null) img.color = isSelected ? buttonActiveColor : buttonInactiveColor;
                if (txt != null) txt.color = isSelected ? Color.white : new Color(0.75f, 0.85f, 0.95f);
            }
        }

        private void ConfigureSwitchesAndLamps()
        {
            bool isDemux = currentCircuit == CircuitType.DEMUX;

            if (uiLampYContainer != null) uiLampYContainer.SetActive(!isDemux);
            if (uiLampDemuxContainer != null) uiLampDemuxContainer.SetActive(isDemux);

            switch (currentCircuit)
            {
                case CircuitType.AND:
                case CircuitType.OR:
                case CircuitType.NAND:
                case CircuitType.NOR:
                    if (switchA != null) switchA.transform.localPosition = new Vector3(-1.25f, 0.88f, -0.20f);
                    if (switchB != null) switchB.transform.localPosition = new Vector3(-0.85f, 0.88f, -0.20f);
                    SetSwitchActive(switchA, true, "A");
                    SetSwitchActive(switchB, true, "B");
                    SetSwitchActive(switchS, false, "");
                    SetLampActive(lampY, true, "Output");
                    SetLampActive(lampY0, false, "");
                    SetLampActive(lampY1, false, "");
                    if (toggleBtnA != null) toggleBtnA.gameObject.SetActive(true);
                    if (toggleBtnB != null) toggleBtnB.gameObject.SetActive(true);
                    if (toggleBtnS != null) toggleBtnS.gameObject.SetActive(false);
                    break;

                case CircuitType.NOT:
                    if (switchA != null) switchA.transform.localPosition = new Vector3(-1.05f, 0.88f, -0.20f);
                    SetSwitchActive(switchA, true, "A");
                    SetSwitchActive(switchB, false, "");
                    SetSwitchActive(switchS, false, "");
                    SetLampActive(lampY, true, "Output");
                    SetLampActive(lampY0, false, "");
                    SetLampActive(lampY1, false, "");
                    if (toggleBtnA != null) toggleBtnA.gameObject.SetActive(true);
                    if (toggleBtnB != null) toggleBtnB.gameObject.SetActive(false);
                    if (toggleBtnS != null) toggleBtnS.gameObject.SetActive(false);
                    break;

                case CircuitType.MUX:
                    if (switchA != null) switchA.transform.localPosition = new Vector3(-1.25f, 0.88f, -0.20f);
                    if (switchB != null) switchB.transform.localPosition = new Vector3(-0.85f, 0.88f, -0.20f);
                    if (switchS != null) switchS.transform.localPosition = new Vector3(-0.45f, 0.88f, -0.20f);
                    SetSwitchActive(switchA, true, "L1");
                    SetSwitchActive(switchB, true, "L2");
                    SetSwitchActive(switchS, true, "S");
                    SetLampActive(lampY, true, "Y");
                    SetLampActive(lampY0, false, "");
                    SetLampActive(lampY1, false, "");
                    if (toggleBtnA != null) toggleBtnA.gameObject.SetActive(true);
                    if (toggleBtnB != null) toggleBtnB.gameObject.SetActive(true);
                    if (toggleBtnS != null) toggleBtnS.gameObject.SetActive(true);
                    break;

                case CircuitType.DEMUX:
                    if (switchA != null) switchA.transform.localPosition = new Vector3(-1.05f, 0.88f, -0.20f);
                    if (switchS != null) switchS.transform.localPosition = new Vector3(-0.65f, 0.88f, -0.20f);
                    SetSwitchActive(switchA, true, "D");
                    SetSwitchActive(switchB, false, "");
                    SetSwitchActive(switchS, true, "S");
                    SetLampActive(lampY, false, "");
                    SetLampActive(lampY0, true, "Y1");
                    SetLampActive(lampY1, true, "Y2");
                    if (toggleBtnA != null) toggleBtnA.gameObject.SetActive(true);
                    if (toggleBtnB != null) toggleBtnB.gameObject.SetActive(false);
                    if (toggleBtnS != null) toggleBtnS.gameObject.SetActive(true);
                    break;
            }
        }

        private void SetSwitchActive(InputSwitch sw, bool active, string label)
        {
            if (sw == null) return;
            sw.gameObject.SetActive(active);
            sw.SetEnabled(active);
            if (active)
            {
                sw.SetLabel(label);
            }
        }

        private void SetLampActive(OutputLamp lamp, bool active, string label)
        {
            if (lamp == null) return;
            lamp.gameObject.SetActive(active);
            lamp.SetState(0);
            if (active)
            {
                lamp.SetLabel(label);
            }
        }

        public void ResetInputs()
        {
            if (switchA != null) switchA.ResetToZero();
            if (switchB != null) switchB.ResetToZero();
            if (switchS != null) switchS.ResetToZero();
            Recalculate();
        }

        private void OnSwitchToggled(InputSwitch sw, int val)
        {
            Recalculate();
        }

        public void Recalculate()
        {
            int[] inputs = GetCurrentInputs();
            int[] outputs = LogicEngine.Evaluate(currentCircuit, inputs);

            // Drive 3D Physical Lamps
            if (currentCircuit == CircuitType.DEMUX)
            {
                if (lampY0 != null) lampY0.SetState(outputs[0]);
                if (lampY1 != null) lampY1.SetState(outputs[1]);
            }
            else
            {
                if (lampY != null) lampY.SetState(outputs[0]);
            }

            // Drive 3D Circuit Symbol & Animated Wires
            if (symbolRenderer != null)
            {
                symbolRenderer.RenderCircuit(currentCircuit, inputs, outputs);
            }

            // Update On-Screen Lamp Indicators
            UpdateOnScreenLampIndicators(outputs);

            // Update Sidebar Input Toggle Buttons
            UpdateSidebarInputButtons();

            // Update Technical Info Panel
            UpdateInfoPanel(inputs, outputs);

            // Update Truth Table Row Highlighting
            UpdateTableHighlight(inputs);
        }

        private void UpdateOnScreenLampIndicators(int[] outputs)
        {
            if (currentCircuit == CircuitType.DEMUX)
            {
                bool y0On = outputs != null && outputs.Length > 0 && outputs[0] == 1;
                bool y1On = outputs != null && outputs.Length > 1 && outputs[1] == 1;

                if (uiLampY0Bg != null) uiLampY0Bg.color = y0On ? lampOnBgColor : lampOffBgColor;
                if (uiLampY0Text != null)
                {
                    uiLampY0Text.text = y0On ? "<b>LIGHT Y1: 1</b>" : "LIGHT Y1: 0";
                    uiLampY0Text.color = Color.white;
                }

                if (uiLampY1Bg != null) uiLampY1Bg.color = y1On ? lampOnBgColor : lampOffBgColor;
                if (uiLampY1Text != null)
                {
                    uiLampY1Text.text = y1On ? "<b>LIGHT Y2: 1</b>" : "LIGHT Y2: 0";
                    uiLampY1Text.color = Color.white;
                }
            }
            else
            {
                bool yOn = outputs != null && outputs.Length > 0 && outputs[0] == 1;

                if (uiLampYBg != null) uiLampYBg.color = yOn ? lampOnBgColor : lampOffBgColor;
                if (uiLampYText != null)
                {
                    string outName = currentCircuit == CircuitType.MUX ? "Y" : "OUTPUT";
                    uiLampYText.text = yOn ? $"<b>LIGHT {outName}: 1</b>" : $"LIGHT {outName}: 0";
                    uiLampYText.color = Color.white;
                }
            }
        }

        private void UpdateSidebarInputButtons()
        {
            int valA = switchA != null ? switchA.CurrentState : 0;
            int valB = switchB != null ? switchB.CurrentState : 0;
            int valS = switchS != null ? switchS.CurrentState : 0;

            string nameA = currentCircuit == CircuitType.DEMUX ? "D" : (currentCircuit == CircuitType.MUX ? "L1" : "A");
            string nameB = currentCircuit == CircuitType.MUX ? "L2" : "B";

            if (toggleTextA != null)
            {
                toggleTextA.text = $"TOGGLE {nameA} : <b>{(valA == 1 ? "1 [ON]" : "0 [OFF]")}</b>";
                toggleTextA.color = valA == 1 ? Color.black : Color.white;
            }
            if (toggleBgA != null) toggleBgA.color = valA == 1 ? inputHighColor : inputLowColor;

            if (toggleTextB != null)
            {
                toggleTextB.text = $"TOGGLE {nameB} : <b>{(valB == 1 ? "1 [ON]" : "0 [OFF]")}</b>";
                toggleTextB.color = valB == 1 ? Color.black : Color.white;
            }
            if (toggleBgB != null) toggleBgB.color = valB == 1 ? inputHighColor : inputLowColor;

            if (toggleTextS != null)
            {
                toggleTextS.text = $"SELECT S : <b>{(valS == 1 ? "1 [L2]" : "0 [L1]")}</b>";
                toggleTextS.color = valS == 1 ? Color.black : Color.white;
            }
            if (toggleBgS != null) toggleBgS.color = valS == 1 ? new Color(1.0f, 0.8f, 0.2f) : inputLowColor;
        }

        private int[] GetCurrentInputs()
        {
            switch (currentCircuit)
            {
                case CircuitType.AND:
                case CircuitType.OR:
                case CircuitType.NAND:
                case CircuitType.NOR:
                    return new int[] { switchA.CurrentState, switchB.CurrentState };

                case CircuitType.NOT:
                    return new int[] { switchA.CurrentState };

                case CircuitType.MUX:
                    return new int[] { switchA.CurrentState, switchB.CurrentState, switchS.CurrentState };

                case CircuitType.DEMUX:
                    return new int[] { switchA.CurrentState, switchS.CurrentState };

                default:
                    return new int[] { 0 };
            }
        }

        private void UpdateInfoPanel(int[] inputs, int[] outputs)
        {
            if (circuitTitleText != null)
                circuitTitleText.text = LogicEngine.GetCircuitName(currentCircuit);

            if (booleanExprText != null)
                booleanExprText.text = $"Expression: <color=#00E5FF>{LogicEngine.GetBooleanExpression(currentCircuit)}</color>";

            if (explanationText != null)
                explanationText.text = LogicEngine.GetExplanation(currentCircuit);

            if (statusSummaryText != null)
            {
                string[] inLabels = LogicEngine.GetInputLabels(currentCircuit);
                string[] outLabels = LogicEngine.GetOutputLabels(currentCircuit);

                string inStr = "";
                for (int i = 0; i < inLabels.Length; i++)
                {
                    inStr += $"{inLabels[i]}={inputs[i]} ";
                }

                string outStr = "";
                for (int i = 0; i < outLabels.Length; i++)
                {
                    outStr += $"{outLabels[i]}=<color={(outputs[i] == 1 ? "#00FFCC" : "#8899AA")}>{outputs[i]}</color> ";
                }

                statusSummaryText.text = $"<b>Inputs:</b> {inStr.Trim()}\n<b>Outputs:</b> {outStr.Trim()}";
            }
        }

        private void BuildTruthTableUI()
        {
            foreach (var rowObj in spawnedTableRows)
            {
                if (rowObj != null) Destroy(rowObj);
            }
            spawnedTableRows.Clear();
            currentTableRows.Clear();

            TruthTable table = LogicEngine.GetTruthTable(currentCircuit);
            currentTableRows = table.Rows;

            // Build Header
            if (truthTableHeaderRow != null)
            {
                var headerTmp = truthTableHeaderRow.GetComponentInChildren<TextMeshProUGUI>();
                if (headerTmp != null)
                {
                    headerTmp.fontSize = 20f;
                    headerTmp.fontStyle = FontStyles.Bold;
                    headerTmp.enableWordWrapping = false;

                    string h = "";
                    if (currentCircuit == CircuitType.MUX)
                    {
                        h = "L1   |   L2   |   S   |   Y";
                    }
                    else if (currentCircuit == CircuitType.DEMUX)
                    {
                        h = "D   |   S   |  Y1  |  Y2";
                    }
                    else if (currentCircuit == CircuitType.NOT)
                    {
                        h = "A   |   Y";
                    }
                    else
                    {
                        h = "A   |   B   |   Y";
                    }
                    headerTmp.text = h;
                }
            }

            // Build Rows
            if (truthTableContent != null)
            {
                for (int r = 0; r < table.Rows.Count; r++)
                {
                    var rowData = table.Rows[r];
                    GameObject rowObj = new GameObject($"Row_{r}");
                    rowObj.transform.SetParent(truthTableContent, false);
                    spawnedTableRows.Add(rowObj);

                    var bg = rowObj.AddComponent<Image>();
                    bg.color = new Color(0.12f, 0.16f, 0.22f, 0.5f);

                    var layoutElement = rowObj.AddComponent<LayoutElement>();
                    layoutElement.minHeight = 36f;
                    layoutElement.preferredHeight = 38f;

                    var rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
                    rowLayout.childAlignment = TextAnchor.MiddleCenter;
                    rowLayout.padding = new RectOffset(6, 6, 2, 2);

                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(rowObj.transform, false);
                    var tmp = textObj.AddComponent<TextMeshProUGUI>();
                    var f = DigitalLogicLab.Core.AppBootstrap.GetDefaultFont();
                    if (f != null) tmp.font = f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 20f;
                    tmp.enableWordWrapping = false;

                    string rowStr = "";
                    if (currentCircuit == CircuitType.MUX)
                    {
                        rowStr = $" {rowData.Inputs[0]}    |    {rowData.Inputs[1]}    |   {rowData.Inputs[2]}   |   {rowData.Outputs[0]}";
                    }
                    else if (currentCircuit == CircuitType.DEMUX)
                    {
                        rowStr = $" {rowData.Inputs[0]}   |   {rowData.Inputs[1]}   |   {rowData.Outputs[0]}  |   {rowData.Outputs[1]}";
                    }
                    else if (currentCircuit == CircuitType.NOT)
                    {
                        rowStr = $" {rowData.Inputs[0]}    |    {rowData.Outputs[0]}";
                    }
                    else
                    {
                        rowStr = $" {rowData.Inputs[0]}    |    {rowData.Inputs[1]}    |    {rowData.Outputs[0]}";
                    }
                    tmp.text = rowStr;
                    tmp.color = new Color(0.85f, 0.9f, 0.95f);
                }
            }
        }

        private void UpdateTableHighlight(int[] currentInputs)
        {
            if (currentInputs == null || spawnedTableRows.Count != currentTableRows.Count) return;

            int matchIdx = -1;
            for (int r = 0; r < currentTableRows.Count; r++)
            {
                bool matches = true;
                for (int i = 0; i < currentInputs.Length; i++)
                {
                    if (currentTableRows[r].Inputs[i] != currentInputs[i])
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                {
                    matchIdx = r;
                    break;
                }
            }

            for (int r = 0; r < spawnedTableRows.Count; r++)
            {
                var bg = spawnedTableRows[r].GetComponent<Image>();
                var text = spawnedTableRows[r].GetComponentInChildren<TextMeshProUGUI>();

                if (r == matchIdx)
                {
                    if (bg != null) bg.color = new Color(0.0f, 0.65f, 0.85f, 0.9f);
                    if (text != null)
                    {
                        text.color = Color.white;
                        text.fontStyle = FontStyles.Bold;
                    }
                }
                else
                {
                    if (bg != null) bg.color = new Color(0.12f, 0.16f, 0.22f, 0.5f);
                    if (text != null)
                    {
                        text.color = new Color(0.75f, 0.8f, 0.88f);
                        text.fontStyle = FontStyles.Normal;
                    }
                }
            }
        }
    }
}
