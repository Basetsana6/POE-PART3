using System.Collections.Generic;

namespace WpfApp3
{
    /// <summary>
       /// Represents one quiz question with answer options and an explanation.
       /// </summary>

    // Represents one question in the cybersecurity quiz.
    public class QuizQuestion
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public int CorrectIndex { get; set; }   // 0-based
        public string Explanation { get; set; }

        // Helper to indicate a True/False question (only two options)
        public bool IsTrueFalse => Options != null && Options.Count == 2;
    }
}