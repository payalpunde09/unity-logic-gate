using System;
using System.Collections.Generic;

namespace DigitalLogicLab.Quiz
{
    public static class QuizSimulationTests
    {
        public struct SimulationResult
        {
            public int TotalSimulatedAttempts;
            public int PassedAttempts;
            public int FailedAttempts;
            public string Log;
        }

        public static SimulationResult Run200AttemptsSimulation()
        {
            int totalAttempts = 200;
            int passedAttempts = 0;
            int failedAttempts = 0;
            var log = new System.Text.StringBuilder();

            HashSet<int> previousAttemptIds = new HashSet<int>();
            var masterMap = new Dictionary<int, QuizQuestion>();
            foreach (var q in QuizEngine.MasterBank)
            {
                masterMap[q.Id] = q;
            }

            Random simRng = new Random();

            for (int i = 0; i < totalAttempts; i++)
            {
                bool attemptValid = true;
                string attemptFailReason = "";

                QuizAttempt attempt = QuizEngine.CreateAttempt(previousAttemptIds);

                // 1. Assert exactly 5 questions
                if (attempt.Questions.Count != 5)
                {
                    attemptValid = false;
                    attemptFailReason += $"Expected 5 questions, got {attempt.Questions.Count}; ";
                }

                // 2. Assert all 5 are distinct IDs
                HashSet<int> distinctIds = new HashSet<int>();
                foreach (var q in attempt.Questions)
                {
                    if (distinctIds.Contains(q.OriginalId))
                    {
                        attemptValid = false;
                        attemptFailReason += $"Duplicate question ID {q.OriginalId}; ";
                    }
                    distinctIds.Add(q.OriginalId);

                    // 3. Assert correct-answer remapping
                    QuizQuestion original = masterMap[q.OriginalId];
                    string originalExpectedAnswer = original.Options[original.CorrectIndex];
                    string remappedAnswer = q.ShuffledOptions[q.NewCorrectIndex];

                    if (originalExpectedAnswer != remappedAnswer)
                    {
                        attemptValid = false;
                        attemptFailReason += $"Option remapping mismatch on Q{q.OriginalId}. Expected '{originalExpectedAnswer}' but got '{remappedAnswer}'; ";
                    }

                    // Simulate user answering either correctly or randomly
                    bool pickCorrect = simRng.Next(2) == 0;
                    if (pickCorrect)
                    {
                        q.SelectedIndex = q.NewCorrectIndex;
                    }
                    else
                    {
                        // pick an incorrect option
                        int wrong = (q.NewCorrectIndex + 1 + simRng.Next(3)) % 4;
                        q.SelectedIndex = wrong;
                    }

                    q.IsAnswered = true;
                    if (q.IsCorrect)
                    {
                        attempt.Score++;
                    }
                }

                // 4. Assert score never exceeds 5 and is >= 0
                if (attempt.Score < 0 || attempt.Score > 5)
                {
                    attemptValid = false;
                    attemptFailReason += $"Score out of bounds: {attempt.Score}; ";
                }

                // 5. Assert percentage matches score
                float expectedPercentage = (attempt.Score / 5.0f) * 100f;
                if (Math.Abs(attempt.Percentage - expectedPercentage) > 0.001f)
                {
                    attemptValid = false;
                    attemptFailReason += $"Percentage mismatch: expected {expectedPercentage}, got {attempt.Percentage}; ";
                }

                if (attemptValid)
                {
                    passedAttempts++;
                }
                else
                {
                    failedAttempts++;
                    log.AppendLine($"[FAIL] Attempt #{i + 1}: {attemptFailReason}");
                }

                // Update previous attempt IDs for next iteration
                previousAttemptIds = distinctIds;
            }

            log.AppendLine($"Simulation Completed: {passedAttempts}/{totalAttempts} attempts passed successfully (Failed: {failedAttempts}).");

            return new SimulationResult
            {
                TotalSimulatedAttempts = totalAttempts,
                PassedAttempts = passedAttempts,
                FailedAttempts = failedAttempts,
                Log = log.ToString()
            };
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Digital Logic Lab/Run 200 Quiz Simulation Tests")]
        public static void RunFromEditorMenu()
        {
            var res = Run200AttemptsSimulation();
            UnityEngine.Debug.Log($"[DigitalLogicLab] 200 Quiz Attempts Simulation:\n{res.Log}");
            if (res.FailedAttempts > 0)
            {
                UnityEngine.Debug.LogError($"[DigitalLogicLab] Quiz simulation failed on {res.FailedAttempts} attempts!");
            }
            else if (!UnityEngine.Application.isBatchMode)
            {
                UnityEditor.EditorUtility.DisplayDialog("Quiz Simulation Tests", $"All 200 Quiz Attempts PASSED successfully!\n\n- 5 distinct questions per attempt: 100%\n- Option remapping verified: 100%\n- Max score boundary (<= 5): 100%", "OK");
            }
        }
#endif
    }
}
