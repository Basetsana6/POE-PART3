using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WpfApp3
{
    /// <summary>
    /// NLP (Natural Language Processing) simulation layer.
    /// Uses keyword detection, regex, and synonym mapping to understand
    /// varied user phrasings and resolve them to a canonical intent.
    /// </summary>
    public class NLPProcessor
    {
        // ── Intent constants ─────────────────────────────────────
        public const string INTENT_ADD_TASK = "add_task";
        public const string INTENT_VIEW_TASKS = "view_tasks";
        public const string INTENT_COMPLETE_TASK = "complete_task";
        public const string INTENT_DELETE_TASK = "delete_task";
        public const string INTENT_SET_REMINDER = "set_reminder";
        public const string INTENT_START_QUIZ = "start_quiz";
        public const string INTENT_SHOW_LOG = "show_log";
        public const string INTENT_UNKNOWN = "unknown";

        // ── Synonym tables ────────────────────────────────────────
        // Maps many natural phrasings → one normalised keyword cluster
        private readonly Dictionary<string, List<string>> _synonyms;

        public NLPProcessor()
        {
            _synonyms = new Dictionary<string, List<string>>
            {
                ["add_task"] = new List<string>
        {
          "add task", "create task", "new task", "add a task", "make a task",
          "set a task", "schedule task", "add reminder", "set reminder",
          "remind me", "remind me to", "add todo", "set a reminder",
          "i need to", "don't forget", "remember to", "can you remind",
          "please remind", "add to my list"
        },
                ["view_tasks"] = new List<string>
        {
          "show tasks", "view tasks", "list tasks", "my tasks",
          "what tasks", "pending tasks", "show my tasks", "all tasks",
          "what do i have", "what's on my list", "show list",
          "task list", "show reminders", "what reminders"
        },
                ["complete_task"] = new List<string>
        {
          "complete task", "mark done", "mark as done", "mark complete",
          "finish task", "done with", "completed", "task done",
          "mark finished", "i finished", "i've done", "i did"
        },
                ["delete_task"] = new List<string>
        {
          "delete task", "remove task", "cancel task",
          "get rid of task", "erase task", "drop task"
        },
                ["start_quiz"] = new List<string>
        {
          "quiz", "start quiz", "play quiz", "begin quiz",
          "test me", "test my knowledge", "challenge me",
          "cybersecurity quiz", "knowledge test", "mini game",
          "question", "trivia", "quiz game"
        },
                ["show_log"] = new List<string>
        {
          "activity log", "show log", "show activity", "recent actions",
          "what have you done", "what did you do", "history",
          "log", "actions", "summary", "what have you been doing",
          "recap", "my history", "what happened"
        }
            };
        }

        // ══════════════════════════════════════════════════════════
        //  INTENT DETECTION
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Analyses <paramref name="input"/> and returns the best-matching intent constant.
        /// </summary>
        public string DetectIntent(string input)
        {
            string normalised = Normalise(input);

            foreach (var kvp in _synonyms)
            {
                foreach (string phrase in kvp.Value)
                {
                    if (normalised.Contains(phrase))
                        return kvp.Key;
                }
            }

            return INTENT_UNKNOWN;
        }

        // ══════════════════════════════════════════════════════════
        //  ENTITY EXTRACTION
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Tries to extract a task title from natural language.
        /// E.g. "add task to enable 2FA" → "Enable 2FA"
        ///      "remind me to update my password" → "Update My Password"
        /// </summary>
        public string ExtractTaskTitle(string input)
        {
            string lower = Normalise(input);

            // Patterns like "remind me to X" / "add task to X" / "add task: X"
            string[] patterns =
      {
        @"remind me to (.+)",
        @"add (?:a )?task (?:to |for |about )?(.+)",
        @"create (?:a )?task (?:to |for |about )?(.+)",
        @"new task[:\-]?\s*(.+)",
        @"don't forget (?:to )?(.+)",
        @"remember to (.+)",
        @"set (?:a )?reminder (?:to |for )?(.+)",
        @"i need to (.+)",
        @"can you remind me (?:to )?(.+)",
        @"please remind me (?:to )?(.+)"
      };

            foreach (string pattern in patterns)
            {
                var m = Regex.Match(lower, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    string title = m.Groups[1].Value.Trim();
                    // Remove trailing noise
                    title = Regex.Replace(title, @"\s+(?:tomorrow|today|in \d+ days?|next week).*$", "",
                     RegexOptions.IgnoreCase).Trim();
                    return ToTitleCase(title);
                }
            }

            // Fallback: strip common prefix words and use the rest
            string stripped = Regex.Replace(lower,
        @"^(?:add|create|new|set|remind|please|can you|i need to|remember to)\s+(?:task|reminder|a task|a reminder)?\s*(?:to|for|about)?\s*",
        "", RegexOptions.IgnoreCase).Trim();

            return stripped.Length > 2 ? ToTitleCase(stripped) : null;
        }

        /// <summary>
        /// Extracts a DateTime reminder from phrases like:
        ///   "in 3 days", "in 1 week", "tomorrow", "in 2 hours"
        /// Returns null if no date phrase is found.
        /// </summary>
        public DateTime? ExtractReminderDate(string input)
        {
            string lower = Normalise(input);

            // "tomorrow"
            if (Regex.IsMatch(lower, @"\btomorrow\b"))
                return DateTime.Today.AddDays(1);

            // "today"
            if (Regex.IsMatch(lower, @"\btoday\b"))
                return DateTime.Today;

            // "in N day(s)"
            var dayMatch = Regex.Match(lower, @"in (\d+)\s+days?");
            if (dayMatch.Success)
                return DateTime.Today.AddDays(int.Parse(dayMatch.Groups[1].Value));

            // "in N week(s)"
            var weekMatch = Regex.Match(lower, @"in (\d+)\s+weeks?");
            if (weekMatch.Success)
                return DateTime.Today.AddDays(int.Parse(weekMatch.Groups[1].Value) * 7);

            // "in N hour(s)"
            var hourMatch = Regex.Match(lower, @"in (\d+)\s+hours?");
            if (hourMatch.Success)
                return DateTime.Now.AddHours(int.Parse(hourMatch.Groups[1].Value));

            // "next week"
            if (Regex.IsMatch(lower, @"\bnext week\b"))
                return DateTime.Today.AddDays(7);

            return null;
        }

        /// <summary>
        /// Tries to extract a numeric task ID from input.
        /// E.g. "complete task 3" → 3
        /// </summary>
        public int? ExtractTaskId(string input)
        {
            var match = Regex.Match(input, @"\b(\d+)\b");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
                return id;

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private string Normalise(string input) =>
      input?.ToLower().Trim() ?? string.Empty;

        private string ToTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }
    }
}
