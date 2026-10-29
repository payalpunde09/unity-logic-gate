using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DigitalLogicLab.Logic;
using DigitalLogicLab.UI;

namespace DigitalLogicLab.Theory
{
    /// <summary>
    /// Controls Screen 2 (Theory).
    /// Provides interactive tabs for all 7 circuits displaying expressions, dynamic truth tables,
    /// symbol previews, universal gate notes, and real-world engineering applications.
    /// </summary>
    public class TheoryPanel : MonoBehaviour
    {
        [Header("Card Content Displays")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI expressionText;
        [SerializeField] private TextMeshProUGUI ioDescriptionText;
        [SerializeField] private TextMeshProUGUI explanationText;
        [SerializeField] private TextMeshProUGUI applicationText;
        [SerializeField] private TextMeshProUGUI specialNoteText;
        [SerializeField] private TextMeshProUGUI truthTableText;

        [Header("Tab Buttons (7 Circuits)")]
        [SerializeField] private Button[] tabButtons;

        [Header("Navigation Buttons")]
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button openLabButton;
        [SerializeField] private Button openQuizButton;

        private CircuitType currentTab = CircuitType.AND;

        public void Initialize(
            TextMeshProUGUI title, TextMeshProUGUI expr, TextMeshProUGUI ioDesc,
            TextMeshProUGUI expl, TextMeshProUGUI app, TextMeshProUGUI note, TextMeshProUGUI table,
            Button[] tabs, Button backBtn, Button labBtn, Button quizBtn)
        {
            titleText = title;
            expressionText = expr;
            ioDescriptionText = ioDesc;
            explanationText = expl;
            applicationText = app;
            specialNoteText = note;
            truthTableText = table;

            tabButtons = tabs;
            backToMenuButton = backBtn;
            openLabButton = labBtn;
            openQuizButton = quizBtn;

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(() => ScreenManager.Instance.OpenMenu());

            if (openLabButton != null)
                openLabButton.onClick.AddListener(() => ScreenManager.Instance.OpenLab());

            if (openQuizButton != null)
                openQuizButton.onClick.AddListener(() => ScreenManager.Instance.OpenQuiz());

            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    int index = i;
                    tabButtons[i].onClick.AddListener(() => DisplayCircuit((CircuitType)index));
                }
            }

            DisplayCircuit(CircuitType.AND);
        }

        public void DisplayCircuit(CircuitType circuit)
        {
            currentTab = circuit;

            // Highlight active tab
            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    var img = tabButtons[i].GetComponent<Image>();
                    var txt = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    bool isSelected = (i == (int)circuit);
                    if (img != null) img.color = isSelected ? new Color(0.0f, 0.5f, 0.7f, 1f) : new Color(0.12f, 0.16f, 0.22f, 0.8f);
                    if (txt != null) txt.color = isSelected ? Color.white : new Color(0.7f, 0.75f, 0.85f);
                }
            }

            if (titleText != null)
                titleText.text = LogicEngine.GetCircuitName(circuit);

            if (expressionText != null)
                expressionText.text = $"<b>Boolean Expression:</b> <color=#00E5FF>{LogicEngine.GetBooleanExpression(circuit)}</color>";

            if (ioDescriptionText != null)
                ioDescriptionText.text = GetIODescription(circuit);

            if (explanationText != null)
                explanationText.text = LogicEngine.GetExplanation(circuit);

            if (applicationText != null)
                applicationText.text = $"<b>Practical Application:</b> {LogicEngine.GetPracticalApplication(circuit)}";

            if (specialNoteText != null)
                specialNoteText.text = GetSpecialNote(circuit);

            if (truthTableText != null)
                truthTableText.text = FormatTruthTableString(circuit);
        }

        private string GetIODescription(CircuitType circuit)
        {
            switch (circuit)
            {
                case CircuitType.AND:
                case CircuitType.OR:
                case CircuitType.NAND:
                case CircuitType.NOR:
                    return "<b>Inputs:</b> A, B (Binary 0 or 1)\n<b>Output:</b> Y (Binary 0 or 1)";
                case CircuitType.NOT:
                    return "<b>Input:</b> A (Single binary input)\n<b>Output:</b> Y (Inverted output)";
                case CircuitType.MUX:
                    return "<b>Inputs:</b> L1 (Input 1), L2 (Input 2), S (Select Line)\n<b>Output:</b> Y (Selected data line)\n<i>S=0 selects L1 (Y=L1); S=1 selects L2 (Y=L2).</i>";
                case CircuitType.DEMUX:
                    return "<b>Inputs:</b> D (Data Input), S (Select Line)\n<b>Outputs:</b> Y1, Y2\n<i>S=0 routes D to Y1; S=1 routes D to Y2. When D=0, both outputs are 0. Both outputs are never 1 together.</i>";
                default:
                    return "";
            }
        }

        private string GetSpecialNote(CircuitType circuit)
        {
            if (circuit == CircuitType.AND)
            {
                return "<color=#00E5FF><b>KEY PRINCIPLE:</b></color> Acts as a series circuit where all contacts must close for conduction. Used as an enable gate.";
            }
            if (circuit == CircuitType.OR)
            {
                return "<color=#00E5FF><b>KEY PRINCIPLE:</b></color> Acts as a parallel circuit where any single closed contact conducts. Used for multi-condition triggers.";
            }
            if (circuit == CircuitType.NOT)
            {
                return "<color=#00E5FF><b>KEY PRINCIPLE:</b></color> Fundamental building block for oscillator ring clocks and bistable flip-flop cross-coupled storage elements.";
            }
            if (circuit == CircuitType.NAND || circuit == CircuitType.NOR)
            {
                return "<color=#FFD700><b>★ UNIVERSAL GATE:</b></color> Universal because any logic circuit (AND, OR, NOT, XOR, Latches) can be created solely with this single gate type.";
            }
            if (circuit == CircuitType.MUX)
            {
                return "<color=#00FFCC><b>ℹ MULTIPLEXER (MUX):</b></color> Data selector directing multiple input channels onto a single output bus, saving physical wiring.";
            }
            if (circuit == CircuitType.DEMUX)
            {
                return "<color=#00FFCC><b>ℹ DEMULTIPLEXER (DEMUX):</b></color> Data distributor routing one signal to multiple destinations. Inverts the multiplexing process.";
            }
            return "";
        }

        private string FormatTruthTableString(CircuitType circuit)
        {
            var table = LogicEngine.GetTruthTable(circuit);
            var sb = new System.Text.StringBuilder();

            // Header
            sb.Append("  ");
            foreach (var inL in table.InputNames) sb.Append($"{inL}    ");
            sb.Append("|    ");
            foreach (var outL in table.OutputNames) sb.Append($"{outL}    ");
            sb.AppendLine();
            sb.AppendLine("  ---------------------------------");

            // Rows
            foreach (var row in table.Rows)
            {
                sb.Append("  ");
                foreach (var inV in row.Inputs) sb.Append($" {inV}    ");
                sb.Append("|    ");
                foreach (var outV in row.Outputs) sb.Append($" {outV}    ");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
