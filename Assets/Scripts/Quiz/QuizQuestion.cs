using System;
using System.Collections.Generic;

namespace DigitalLogicLab.Quiz
{
    [System.Serializable]
    public class QuizQuestion
    {
        public int Id;
        public string Text;
        public string[] Options;
        public int CorrectIndex;
        public string Explanation;

        public QuizQuestion(int id, string text, string[] options, int correctIndex, string explanation)
        {
            Id = id;
            Text = text;
            Options = options;
            CorrectIndex = correctIndex;
            Explanation = explanation;
        }

        public QuizQuestion Clone()
        {
            string[] clonedOptions = (string[])Options.Clone();
            return new QuizQuestion(Id, Text, clonedOptions, CorrectIndex, Explanation);
        }
    }

    public class ShuffledQuestion
    {
        public int OriginalId;
        public string Text = "";
        public string[] ShuffledOptions = new string[4];
        public int NewCorrectIndex;
        public string Explanation = "";
        public int SelectedIndex = -1; // -1 = unselected
        public bool IsAnswered = false;

        public bool IsCorrect => SelectedIndex == NewCorrectIndex;
    }

    public class QuizAttempt
    {
        public List<ShuffledQuestion> Questions = new List<ShuffledQuestion>();
        public int CurrentQuestionIndex = 0;
        public int Score = 0;
        public bool IsCompleted = false;

        public int TotalQuestions => Questions.Count;
        public int CorrectCount => Score;
        public int IncorrectCount => TotalQuestions - Score;
        public float Percentage => TotalQuestions > 0 ? ((float)Score / TotalQuestions) * 100f : 0f;
    }

    /// <summary>
    /// Pure C# quiz logic engine managing question bank, Fisher-Yates shuffling,
    /// option remapping, attempt generation, and attempt validation.
    /// </summary>
    public static class QuizEngine
    {
        public static readonly List<QuizQuestion> MasterBank = new List<QuizQuestion>()
        {
            new QuizQuestion(
                1,
                "If A=1 and B=0, what is the output of an AND gate?",
                new string[] { "0", "1", "A", "Undefined" },
                0,
                "An AND gate produces 1 only when both inputs are 1. Since B=0, the output is 0."
            ),
            new QuizQuestion(
                2,
                "Which gates are known as universal gates?",
                new string[] { "AND and OR", "XOR and NOT", "NAND and NOR", "AND and NOT" },
                2,
                "NAND and NOR are universal gates because either type can implement any Boolean logic function."
            ),
            new QuizQuestion(
                3,
                "What is the main function of a multiplexer (MUX)?",
                new string[] { "Convert binary numbers to decimal", "Select one input from multiple inputs and send it to one output", "Always invert the input", "Store data permanently" },
                1,
                "A multiplexer uses select lines to choose which input is connected to its single output."
            ),
            new QuizQuestion(
                4,
                "For a 1:2 DEMUX, if D=1 and S=1, what are the outputs?",
                new string[] { "Y1=1, Y2=0", "Y1=0, Y2=1", "Y1=1, Y2=1", "Y1=0, Y2=0" },
                1,
                "When S=1, the data input D is routed to Y2. Therefore, Y1=0 and Y2=1."
            ),
            new QuizQuestion(
                5,
                "If A=0 and B=0, what is the output of a NOR gate?",
                new string[] { "0", "1", "A", "Undefined" },
                1,
                "NOR is the inverse of OR. When both inputs are 0, the OR output is 0, so the NOR output is 1."
            ),
            new QuizQuestion(
                6,
                "If A=0 and B=1, what is the output of an OR gate?",
                new string[] { "0", "1", "Undefined", "NOT A" },
                1,
                "An OR gate produces 1 when at least one input is 1."
            ),
            new QuizQuestion(
                7,
                "If the input of a NOT gate is A=1, what is the output?",
                new string[] { "1", "0", "A", "Undefined" },
                1,
                "A NOT gate inverts its input, so an input of 1 produces an output of 0."
            ),
            new QuizQuestion(
                8,
                "If A=1 and B=1, what is the output of a NAND gate?",
                new string[] { "0", "1", "A", "Undefined" },
                0,
                "NAND is the inverse of AND. When both inputs are 1, the AND output is 1, so the NAND output is 0."
            ),
            new QuizQuestion(
                9,
                "For a 2:1 multiplexer, which input is selected when S=0?",
                new string[] { "L2", "Both L1 and L2 simultaneously", "L1", "Neither input" },
                2,
                "When S=0, the multiplexer connects input line L1 to output Y."
            ),
            new QuizQuestion(
                10,
                "For a 1:2 DEMUX, if D=1 and S=0, what are the outputs?",
                new string[] { "Y1=0, Y2=1", "Y1=1, Y2=0", "Y1=1, Y2=1", "Y1=0, Y2=0" },
                1,
                "When S=0, the data input D is routed to Y1. Since D=1, Y1=1 and Y2=0."
            )
        };

        private static Random rng = new Random();

        /// <summary>
        /// Creates a new 5-question attempt using Fisher-Yates shuffle,
        /// remapping options and ensuring set differentiation from previous attempt if supplied.
        /// </summary>
        public static QuizAttempt CreateAttempt(HashSet<int> previousQuestionIds = null)
        {
            List<QuizQuestion> pool = new List<QuizQuestion>();
            foreach (var q in MasterBank) pool.Add(q.Clone());

            // Try up to 10 shuffles to avoid having an identical set of questions
            List<QuizQuestion> chosen = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                FisherYatesShuffle(pool);
                chosen = pool.GetRange(0, 5);

                if (previousQuestionIds == null || previousQuestionIds.Count == 0)
                {
                    break;
                }

                // Check if set is identical to previous
                bool isExactSameSet = true;
                foreach (var q in chosen)
                {
                    if (!previousQuestionIds.Contains(q.Id))
                    {
                        isExactSameSet = false;
                        break;
                    }
                }

                if (!isExactSameSet)
                {
                    break;
                }
            }

            QuizAttempt newAttempt = new QuizAttempt();
            foreach (var q in chosen)
            {
                newAttempt.Questions.Add(ShuffleQuestionOptions(q));
            }

            return newAttempt;
        }

        private static ShuffledQuestion ShuffleQuestionOptions(QuizQuestion original)
        {
            string correctAnswerString = original.Options[original.CorrectIndex];

            // Create list of index mappings
            List<int> indices = new List<int> { 0, 1, 2, 3 };
            FisherYatesShuffle(indices);

            string[] shuffledOptions = new string[4];
            int newCorrectIndex = -1;

            for (int i = 0; i < 4; i++)
            {
                shuffledOptions[i] = original.Options[indices[i]];
                if (shuffledOptions[i] == correctAnswerString)
                {
                    newCorrectIndex = i;
                }
            }

            return new ShuffledQuestion
            {
                OriginalId = original.Id,
                Text = original.Text,
                ShuffledOptions = shuffledOptions,
                NewCorrectIndex = newCorrectIndex,
                Explanation = original.Explanation,
                SelectedIndex = -1,
                IsAnswered = false
            };
        }

        public static void FisherYatesShuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
