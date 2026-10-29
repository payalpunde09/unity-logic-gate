using System;
using System.Collections.Generic;

namespace DigitalLogicLab.Logic
{
    public enum CircuitType
    {
        AND = 0,
        OR = 1,
        NOT = 2,
        NAND = 3,
        NOR = 4,
        MUX = 5,
        DEMUX = 6
    }

    public struct TruthTableRow
    {
        public int[] Inputs;
        public int[] Outputs;

        public TruthTableRow(int[] inputs, int[] outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
        }
    }

    public class TruthTable
    {
        public CircuitType Circuit { get; set; }
        public string[] InputNames { get; set; }
        public string[] OutputNames { get; set; }
        public List<TruthTableRow> Rows { get; set; }

        public TruthTable(CircuitType circuit, string[] inputNames, string[] outputNames)
        {
            Circuit = circuit;
            InputNames = inputNames;
            OutputNames = outputNames;
            Rows = new List<TruthTableRow>();
        }
    }

    /// <summary>
    /// Pure static C# logic evaluation engine without any UnityEngine dependencies.
    /// Strictly unit-testable.
    /// </summary>
    public static class LogicEngine
    {
        public static string GetCircuitName(CircuitType type)
        {
            switch (type)
            {
                case CircuitType.AND: return "AND Gate";
                case CircuitType.OR: return "OR Gate";
                case CircuitType.NOT: return "NOT Gate (Inverter)";
                case CircuitType.NAND: return "NAND Gate (Universal)";
                case CircuitType.NOR: return "NOR Gate (Universal)";
                case CircuitType.MUX: return "2:1 Multiplexer (MUX)";
                case CircuitType.DEMUX: return "1:2 Demultiplexer (DEMUX)";
                default: return type.ToString();
            }
        }

        public static string GetBooleanExpression(CircuitType type)
        {
            switch (type)
            {
                case CircuitType.AND: return "Y = A · B";
                case CircuitType.OR: return "Y = A + B";
                case CircuitType.NOT: return "Y = ¬A";
                case CircuitType.NAND: return "Y = ¬(A · B)";
                case CircuitType.NOR: return "Y = ¬(A + B)";
                case CircuitType.MUX: return "Y = ¬S · I0 + S · I1";
                case CircuitType.DEMUX: return "Y0 = D · ¬S,  Y1 = D · S";
                default: return "";
            }
        }

        public static string[] GetInputLabels(CircuitType type)
        {
            switch (type)
            {
                case CircuitType.AND:
                case CircuitType.OR:
                case CircuitType.NAND:
                case CircuitType.NOR:
                    return new string[] { "A", "B" };
                case CircuitType.NOT:
                    return new string[] { "A" };
                case CircuitType.MUX:
                    return new string[] { "L1", "L2", "S" };
                case CircuitType.DEMUX:
                    return new string[] { "D", "S" };
                default:
                    return Array.Empty<string>();
            }
        }

        public static string[] GetOutputLabels(CircuitType type)
        {
            switch (type)
            {
                case CircuitType.DEMUX:
                    return new string[] { "Y1", "Y2" };
                default:
                    return new string[] { "Y" };
            }
        }

        /// <summary>
        /// Evaluates logic function according to circuit type and inputs.
        /// </summary>
        public static int[] Evaluate(CircuitType type, int[] inputs)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            switch (type)
            {
                case CircuitType.AND:
                {
                    int a = inputs.Length > 0 ? inputs[0] : 0;
                    int b = inputs.Length > 1 ? inputs[1] : 0;
                    int y = (a == 1 && b == 1) ? 1 : 0;
                    return new int[] { y };
                }

                case CircuitType.OR:
                {
                    int a = inputs.Length > 0 ? inputs[0] : 0;
                    int b = inputs.Length > 1 ? inputs[1] : 0;
                    int y = (a == 1 || b == 1) ? 1 : 0;
                    return new int[] { y };
                }

                case CircuitType.NOT:
                {
                    int a = inputs.Length > 0 ? inputs[0] : 0;
                    int y = (a == 0) ? 1 : 0;
                    return new int[] { y };
                }

                case CircuitType.NAND:
                {
                    int a = inputs.Length > 0 ? inputs[0] : 0;
                    int b = inputs.Length > 1 ? inputs[1] : 0;
                    int y = !(a == 1 && b == 1) ? 1 : 0;
                    return new int[] { y };
                }

                case CircuitType.NOR:
                {
                    int a = inputs.Length > 0 ? inputs[0] : 0;
                    int b = inputs.Length > 1 ? inputs[1] : 0;
                    int y = !(a == 1 || b == 1) ? 1 : 0;
                    return new int[] { y };
                }

                case CircuitType.MUX:
                {
                    // Inputs: [I0, I1, S]
                    int i0 = inputs.Length > 0 ? inputs[0] : 0;
                    int i1 = inputs.Length > 1 ? inputs[1] : 0;
                    int s = inputs.Length > 2 ? inputs[2] : 0;
                    int y = (s == 1) ? i1 : i0;
                    return new int[] { y };
                }

                case CircuitType.DEMUX:
                {
                    // Inputs: [D, S] -> Outputs: [Y0, Y1]
                    // DEMUX: Y0 = D && !S, Y1 = D && S
                    int d = inputs.Length > 0 ? inputs[0] : 0;
                    int s = inputs.Length > 1 ? inputs[1] : 0;
                    int y0 = (d == 1 && s == 0) ? 1 : 0;
                    int y1 = (d == 1 && s == 1) ? 1 : 0;
                    return new int[] { y0, y1 };
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported circuit type: {type}");
            }
        }

        /// <summary>
        /// Generates full truth table dynamically by calling Evaluate.
        /// Never hardcodes output values.
        /// </summary>
        public static TruthTable GetTruthTable(CircuitType type)
        {
            string[] inLabels = GetInputLabels(type);
            string[] outLabels = GetOutputLabels(type);
            TruthTable table = new TruthTable(type, inLabels, outLabels);

            int numInputs = inLabels.Length;
            int totalCombinations = 1 << numInputs; // 2^numInputs

            for (int i = 0; i < totalCombinations; i++)
            {
                int[] rowInputs = new int[numInputs];
                for (int bit = 0; bit < numInputs; bit++)
                {
                    // standard MSB-first ordering: (i >> (numInputs - 1 - bit)) & 1
                    rowInputs[bit] = (i >> (numInputs - 1 - bit)) & 1;
                }

                int[] rowOutputs = Evaluate(type, rowInputs);
                table.Rows.Add(new TruthTableRow(rowInputs, rowOutputs));
            }

            return table;
        }

        public static string GetExplanation(CircuitType type)
        {
            switch (type)
            {
                case CircuitType.AND:
                    return "<b>Operation Rule:</b> Output Y is 1 ONLY when both inputs A and B are 1. If either input is 0, output is 0.\n<b>Boolean Algebra:</b> Commutative: A·B = B·A  |  Identity: A·1 = A  |  Null: A·0 = 0  |  Idempotent: A·A = A.";
                case CircuitType.OR:
                    return "<b>Operation Rule:</b> Output Y is 1 when AT LEAST ONE input (A or B) is 1. Output is 0 only when both inputs are 0.\n<b>Boolean Algebra:</b> Commutative: A+B = B+A  |  Identity: A+0 = A  |  Null: A+1 = 1  |  Idempotent: A+A = A.";
                case CircuitType.NOT:
                    return "<b>Operation Rule:</b> Inverts the single input state: converts 0 to 1, and 1 to 0 (180° phase inversion).\n<b>Boolean Algebra:</b> Double Negation: ¬(¬A) = A  |  Complement: A·¬A = 0  and  A + ¬A = 1.";
                case CircuitType.NAND:
                    return "<b>Operation Rule:</b> Inverted AND gate. Output is 0 ONLY when both inputs are 1; otherwise output is 1.\n<b>Universal Property:</b> Any Boolean function can be built using only NAND gates (e.g. ¬(A·A) = ¬A).";
                case CircuitType.NOR:
                    return "<b>Operation Rule:</b> Inverted OR gate. Output is 1 ONLY when both inputs are 0; otherwise output is 0.\n<b>Universal Property:</b> Any Boolean function can be built using only NOR gates. De Morgan: ¬(A+B) = ¬A · ¬B.";
                case CircuitType.MUX:
                    return "<b>Operation Rule:</b> Data selector routing 1 of 2 data lines to output Y using select line S: S=0 selects L1 (Y=L1); S=1 selects L2 (Y=L2).\n<b>Logic Function:</b> Realizes Y = (¬S·L1) + (S·L2) without requiring separate discrete gates.";
                case CircuitType.DEMUX:
                    return "<b>Operation Rule:</b> Data distributor routing single input D to either Y1 (when S=0) or Y2 (when S=1). Inactive lines remain 0.\n<b>Logic Function:</b> Realizes Y1 = D·¬S and Y2 = D·S. Functions as a 1-to-2 decoder when D=1.";
                default:
                    return "";
            }
        }

        public static string GetPracticalApplication(CircuitType type)
        {
            switch (type)
            {
                case CircuitType.AND:
                    return "Safety interlock circuits (machine runs only if guard is closed AND start pressed), address enable decoding in CPUs.";
                case CircuitType.OR:
                    return "Alarm annunciators (triggers on smoke sensor OR manual pull), redundant dual-power source failover circuits.";
                case CircuitType.NOT:
                    return "Waveform clock pulse generators, active-low enable signal inversion, CMOS SRAM bit storage cells.";
                case CircuitType.NAND:
                    return "High-density NAND Flash memory storage (SSDs, USB drives, smartphones), ASIC low-cost logic synthesis.";
                case CircuitType.NOR:
                    return "NOR Flash firmware/bootloader memory in microcontrollers, SR bistable latches, industrial motor trip controls.";
                case CircuitType.MUX:
                    return "Time-division telecom transmission, ALU operation operand routing, multi-channel analog/digital acquisition.";
                case CircuitType.DEMUX:
                    return "Microprocessor memory chip-select decoding, serial-to-parallel data demultiplexing, multi-display segment drivers.";
                default:
                    return "";
            }
        }
    }
}
