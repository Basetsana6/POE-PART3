using System.Collections.Generic;

namespace WpfApp3
{
    /// <summary>
    /// Represents one quiz question with answer options and an explanation.
    /// </summary>
    public class QuizQuestion
    {
        public string QuestionText { get; set; }

        /// <summary>List of answer choices displayed to the user.</summary>
        public List<string> Options { get; set; }

        /// <summary>Zero-based index of the correct option.</summary>
        public int CorrectIndex { get; set; }

        /// <summary>Explanation shown after the user answers.</summary>
        public string Explanation { get; set; }

        /// <summary>True/False questions have exactly 2 options.</summary>
        public bool IsTrueFalse => Options != null && Options.Count == 2;
    }
}