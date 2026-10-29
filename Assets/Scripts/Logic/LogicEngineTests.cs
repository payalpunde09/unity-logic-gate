using System;
using System.Diagnostics;
using DigitalLogicLab.Logic;

namespace DigitalLogicLab.Logic
{
    public static class LogicEngineTests
    {
        public struct TestResult
        {
            public int TotalTests;
            public int PassedTests;
            public int FailedTests;
            public string Details;
        }

        public static TestResult RunAllTests()
        {
            int total = 0;
            int passed = 0;
            int failed = 0;
            var log = new System.Text.StringBuilder();

            void AssertTrue(bool condition, string testName)
            {
                total++;
                if (condition)
                {
                    passed++;
                    log.AppendLine($"[PASS] {testName}");
                }
                else
                {
                    failed++;
                    log.AppendLine($"[FAIL] {testName}");
                }
            }

            // 1. AND Gate (A, B -> Y)
            AssertTrue(LogicEngine.Evaluate(CircuitType.AND, new int[] { 0, 0 })[0] == 0, "AND(0,0) == 0");
            AssertTrue(LogicEngine.Evaluate(CircuitType.AND, new int[] { 0, 1 })[0] == 0, "AND(0,1) == 0");
            AssertTrue(LogicEngine.Evaluate(CircuitType.AND, new int[] { 1, 0 })[0] == 0, "AND(1,0) == 0");
            AssertTrue(LogicEngine.Evaluate(CircuitType.AND, new int[] { 1, 1 })[0] == 1, "AND(1,1) == 1");

            // 2. OR Gate (A, B -> Y)
            AssertTrue(LogicEngine.Evaluate(CircuitType.OR, new int[] { 0, 0 })[0] == 0, "OR(0,0) == 0");
            AssertTrue(LogicEngine.Evaluate(CircuitType.OR, new int[] { 0, 1 })[0] == 1, "OR(0,1) == 1");
            AssertTrue(LogicEngine.Evaluate(CircuitType.OR, new int[] { 1, 0 })[0] == 1, "OR(1,0) == 1");
            AssertTrue(LogicEngine.Evaluate(CircuitType.OR, new int[] { 1, 1 })[0] == 1, "OR(1,1) == 1");

            // 3. NOT Gate (A -> Y)
            AssertTrue(LogicEngine.Evaluate(CircuitType.NOT, new int[] { 0 })[0] == 1, "NOT(0) == 1");
            AssertTrue(LogicEngine.Evaluate(CircuitType.NOT, new int[] { 1 })[0] == 0, "NOT(1) == 0");

            // 4. NAND Gate (A, B -> Y)
            AssertTrue(LogicEngine.Evaluate(CircuitType.NAND, new int[] { 0, 0 })[0] == 1, "NAND(0,0) == 1");
            AssertTrue(LogicEngine.Evaluate(CircuitType.NAND, new int[] { 0, 1 })[0] == 1, "NAND(0,1) == 1");
            AssertTrue(LogicEngine.Evaluate(CircuitType.NAND, new int[] { 1, 0 })[0] == 1, "NAND(1,0) == 1");
            AssertTrue(LogicEngine.Evaluate(CircuitType.NAND, new int[] { 1, 1 })[0] == 0, "NAND(1,1) == 0");

            // 5. NOR Gate (A, B -> Y)
            AssertTrue(LogicEngine.Evaluate(CircuitType.NOR, new int[] { 0, 0 })[0] == 1, "NOR(0,0) == 1");
            AssertTrue(LogicEngine.Evaluate(CircuitType.NOR, new int[] { 0, 1 })[0] == 0, "NOR(0,1) == 0");
            AssertTrue(LogicEngine.Evaluate(CircuitType.NOR, new int[] { 1, 0 })[0] == 0, "NOR(1,0) == 0");
            AssertTrue(LogicEngine.Evaluate(CircuitType.NOR, new int[] { 1, 1 })[0] == 0, "NOR(1,1) == 0");

            // 6. MUX (I0, I1, S -> Y)
            AssertTrue(LogicEngine.Evaluate(CircuitType.MUX, new int[] { 0, 0, 0 })[0] == 0, "MUX(0,0,0) == 0");
            AssertTrue(LogicEngine.Evaluate(CircuitType.MUX, new int[] { 0, 1, 0 })[0] == 0, "MUX(0,1,0) == 0 (selects I0=0)");
            AssertTrue(LogicEngine.Evaluate(CircuitType.MUX, new int[] { 1, 0, 0 })[0] == 1, "MUX(1,0,0) == 1 (selects I0=1)");
            AssertTrue(LogicEngine.Evaluate(CircuitType.MUX, new int[] { 1, 1, 0 })[0] == 1, "MUX(1,1,0) == 1 (selects I0=1)");
            AssertTrue(LogicEngine.Evaluate(CircuitType.MUX, new int[] { 0, 0, 1 })[0] == 0, "MUX(0,0,1) == 0");
            AssertTrue(LogicEngine.Evaluate(CircuitType.MUX, new int[] { 0, 1, 1 })[0] == 1, "MUX(0,1,1) == 1 (selects I1=1)");
            AssertTrue(LogicEngine.Evaluate(CircuitType.MUX, new int[] { 1, 0, 1 })[0] == 0, "MUX(1,0,1) == 0 (selects I1=0)");
            AssertTrue(LogicEngine.Evaluate(CircuitType.MUX, new int[] { 1, 1, 1 })[0] == 1, "MUX(1,1,1) == 1 (selects I1=1)");

            // 7. DEMUX (D, S -> Y0, Y1)
            var d00 = LogicEngine.Evaluate(CircuitType.DEMUX, new int[] { 0, 0 });
            AssertTrue(d00[0] == 0 && d00[1] == 0, "DEMUX(0,0) -> Y0=0, Y1=0");

            var d01 = LogicEngine.Evaluate(CircuitType.DEMUX, new int[] { 0, 1 });
            AssertTrue(d01[0] == 0 && d01[1] == 0, "DEMUX(0,1) -> Y0=0, Y1=0");

            var d10 = LogicEngine.Evaluate(CircuitType.DEMUX, new int[] { 1, 0 });
            AssertTrue(d10[0] == 1 && d10[1] == 0, "DEMUX(1,0) -> Y0=1, Y1=0");

            var d11 = LogicEngine.Evaluate(CircuitType.DEMUX, new int[] { 1, 1 });
            AssertTrue(d11[0] == 0 && d11[1] == 1, "DEMUX(1,1) -> Y0=0, Y1=1");

            // DEMUX Critical Invariant Assertion: DEMUX NEVER outputs Y0=1 and Y1=1 together
            bool demuxNeverSimultaneousHigh = true;
            for (int d = 0; d <= 1; d++)
            {
                for (int s = 0; s <= 1; s++)
                {
                    var res = LogicEngine.Evaluate(CircuitType.DEMUX, new int[] { d, s });
                    if (res[0] == 1 && res[1] == 1)
                    {
                        demuxNeverSimultaneousHigh = false;
                    }
                }
            }
            AssertTrue(demuxNeverSimultaneousHigh, "DEMUX never outputs Y0=1 AND Y1=1 simultaneously");

            // 8. Dynamic Truth Table Generation Verification
            foreach (CircuitType circuit in Enum.GetValues(typeof(CircuitType)))
            {
                var table = LogicEngine.GetTruthTable(circuit);
                int expectedRows = 1 << table.InputNames.Length;
                AssertTrue(table.Rows.Count == expectedRows, $"TruthTable for {circuit} has {expectedRows} rows");

                // Verify every row in the generated table matches direct evaluation
                bool allRowsMatch = true;
                foreach (var row in table.Rows)
                {
                    var direct = LogicEngine.Evaluate(circuit, row.Inputs);
                    for (int o = 0; o < direct.Length; o++)
                    {
                        if (row.Outputs[o] != direct[o])
                        {
                            allRowsMatch = false;
                        }
                    }
                }
                AssertTrue(allRowsMatch, $"TruthTable for {circuit} rows strictly match Evaluate()");
            }

            return new TestResult
            {
                TotalTests = total,
                PassedTests = passed,
                FailedTests = failed,
                Details = log.ToString()
            };
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Digital Logic Lab/Run Logic Engine Tests")]
        public static void RunFromEditorMenu()
        {
            var res = RunAllTests();
            UnityEngine.Debug.Log($"[DigitalLogicLab] LogicEngine Tests Result: {res.PassedTests}/{res.TotalTests} Passed.\n{res.Details}");
            if (res.FailedTests > 0)
            {
                UnityEngine.Debug.LogError($"[DigitalLogicLab] {res.FailedTests} Logic Tests FAILED!");
            }
            else if (!UnityEngine.Application.isBatchMode)
            {
                UnityEditor.EditorUtility.DisplayDialog("Logic Engine Tests", $"All {res.TotalTests} tests PASSED successfully!", "OK");
            }
        }
#endif
    }
}
