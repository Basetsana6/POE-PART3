using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApp3
{
    /// <summary>
    /// Core chatbot engine — extends Part 2 with:
    ///   • Task management (via DatabaseManager)
    ///   • Quiz mini-game (via QuizEngine)
    ///   • NLP intent detection (via NLPProcessor)
    ///   • Activity log (stored in MySQL)
    /// </summary>
    public class ChatbotEngine
    {
        // ── Part 2 fields ────────────────────────────────────────
        private Dictionary<string, List<string>> keywordResponses;
        private Dictionary<string, List<string>> randomResponseCategories;
        private Dictionary<string, string> userMemory;
        private Dictionary<string, string> conversationContext;
        private Random random;
        private string currentTopic;
        private string lastUserQuestion;

        // ── Part 3 fields ────────────────────────────────────────
        private DatabaseManager _db;
        private QuizEngine _quiz;
        private NLPProcessor _nlp;

        // Tracks whether we are waiting for the user to confirm a reminder
        private bool _awaitingReminderReply;
        private string _pendingTaskTitle;
        private string _pendingTaskDescription;

        // ── Constructor ──────────────────────────────────────────
        public ChatbotEngine()
        {
            InitializeResponses();
            userMemory = new Dictionary<string, string>();
            conversationContext = new Dictionary<string, string>();
            random = new Random();
            currentTopic = "";
            lastUserQuestion = "";

            _db = new DatabaseManager();
            _quiz = new QuizEngine();
            _nlp = new NLPProcessor();

            _awaitingReminderReply = false;
        }

        // ══════════════════════════════════════════════════════════
        //  MAIN RESPONSE METHOD
        // ══════════════════════════════════════════════════════════

        public string GetResponse(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput)) return "Please type something!";

            string lower = userInput.ToLower().Trim();
            lastUserQuestion = userInput;

            // ── 1. Quiz in-progress: route answers to quiz engine ─
            if (_quiz.IsActive)
                return HandleQuizAnswer(lower);

            // ── 2. Waiting for reminder confirmation ─────────────
            if (_awaitingReminderReply)
                return HandleReminderConfirmation(userInput, lower);

            // ── 3. Greeting / farewell (Part 2) ──────────────────
            if (IsGreeting(lower)) return BuildGreeting();
            if (IsFarewell(lower)) return BuildFarewell();

            // ── 4. Name storage (Part 2) ─────────────────────────
            string nameResp = CheckAndStoreNameWithResponse(lower, userInput);
            if (nameResp != null) return nameResp;

            // ── 5. Memory recall (Part 2) ────────────────────────
            string memResp = CheckMemoryRecall(lower);
            if (memResp != null) return memResp;

            // ── 6. NLP intent detection (Part 3) ─────────────────
            string intent = _nlp.DetectIntent(userInput);

            switch (intent)
            {
                case NLPProcessor.INTENT_ADD_TASK: return HandleAddTask(userInput);
                case NLPProcessor.INTENT_VIEW_TASKS: return HandleViewTasks();
                case NLPProcessor.INTENT_COMPLETE_TASK: return HandleCompleteTask(userInput);
                case NLPProcessor.INTENT_DELETE_TASK: return HandleDeleteTask(userInput);
                case NLPProcessor.INTENT_START_QUIZ: return HandleStartQuiz();
                case NLPProcessor.INTENT_SHOW_LOG: return HandleShowLog();
            }

            // ── 7. Keyword detection (Part 2) ────────────────────
            StoreUserInfo(lower, userInput);

            string followUp = HandleFollowUp(lower);
            if (followUp != null) return followUp;

            foreach (var kw in keywordResponses.Keys)
            {
                if (lower.Contains(kw))
                {
                    currentTopic = kw;
                    string kwResp = GetRandomFromList(keywordResponses[kw]);
                    LogAction($"Cybersecurity info provided on topic: {kw}");
                }
            }

            if (lower.Contains("phishing tip"))
                return GetRandomFromList(randomResponseCategories["phishing_tips"]);
            if (lower.Contains("password tip"))
                return GetRandomFromList(randomResponseCategories["password_tips"]);

            // If nothing matched, return a sensible default
            return GetDefaultResponse("neutral");
        }

        // ══════════════════════════════════════════════════════════
        //  TASK 1: TASK ASSISTANT
        // ══════════════════════════════════════════════════════════

        private string HandleAddTask(string userInput)
        {
            string title = _nlp.ExtractTaskTitle(userInput);
            if (string.IsNullOrWhiteSpace(title))
                title = "Cybersecurity Task";

            // Try to detect a reminder in the same sentence
            DateTime? reminder = _nlp.ExtractReminderDate(userInput);

            string description = BuildTaskDescription(title);

            if (reminder.HasValue)
            {
                // Add immediately with the embedded reminder
                SaveTask(title, description, reminder);
                return $"✅ Task added: \"{title}\" — Reminder set for {reminder.Value:dd MMM yyyy}. " +
                       $"Type 'show tasks' to view your task list.";
            }
            else
            {
                // Park the task and ask about a reminder
                _pendingTaskTitle = title;
                _pendingTaskDescription = description;
                _awaitingReminderReply = true;

                return $"✅ Task added: \"{title}\" — {description}\n" +
                       "Would you like a reminder? If so, say something like " +
                       "\"Yes, in 3 days\" or \"Remind me tomorrow\". Otherwise just say \"No\".";
            }
        }

        private string HandleReminderConfirmation(string originalInput, string lower)
        {
            _awaitingReminderReply = false;
            DateTime? reminder = _nlp.ExtractReminderDate(originalInput);

            bool declined = lower.Contains("no") || lower.Contains("skip") ||
                            lower.Contains("none") || lower.Contains("don't");

            if (declined || reminder == null)
            {
                SaveTask(_pendingTaskTitle, _pendingTaskDescription, null);
                _pendingTaskTitle = _pendingTaskDescription = null;
                return $"Got it! Task \"{_pendingTaskTitle ?? "your task"}\" saved without a reminder. " +
                       "Type 'show tasks' anytime to view your list.";
            }

            SaveTask(_pendingTaskTitle, _pendingTaskDescription, reminder);
            string saved = _pendingTaskTitle;
            _pendingTaskTitle = _pendingTaskDescription = null;
            return $"✅ Reminder set for \"{saved}\" on {reminder.Value:dd MMM yyyy}. " +
                   "I'll keep that noted for you!";
        }

        private string HandleViewTasks()
        {
            try
            {
                var tasks = _db.GetAllTasks();
                if (tasks.Count == 0)
                    return "📋 You have no tasks yet. Say 'add task' to create one!";

                var sb = new StringBuilder("📋 Your cybersecurity tasks:\n\n");
                foreach (var t in tasks)
                    sb.AppendLine($"  [{t.Id}] {t.StatusDisplay} {t.Title}\n       📝 {t.Description}\n       {t.ReminderDisplay}\n");

                sb.AppendLine("💡 To complete: \"complete task [ID]\"  |  To delete: \"delete task [ID]\"");

                LogAction("User viewed task list");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Could not load tasks: {ex.Message}\n(Check that MySQL is running and the DB is set up.)";
            }
        }

        private string HandleCompleteTask(string userInput)
        {
            int? id = _nlp.ExtractTaskId(userInput);
            if (id == null)
                return "Please tell me the task ID, e.g. \"complete task 2\". Type 'show tasks' to see IDs.";

            try
            {
                _db.CompleteTask(id.Value);
                LogAction($"Task #{id} marked as completed");
                return $"✅ Task #{id} marked as completed. Great job staying on top of your cybersecurity!";
            }
            catch (Exception ex)
            {
                return $"❌ Could not complete task: {ex.Message}";
            }
        }

        private string HandleDeleteTask(string userInput)
        {
            int? id = _nlp.ExtractTaskId(userInput);
            if (id == null)
                return "Please tell me the task ID to delete, e.g. \"delete task 3\". Type 'show tasks' to see IDs.";

            try
            {
                _db.DeleteTask(id.Value);
                LogAction($"Task #{id} deleted");
                return $"🗑️ Task #{id} has been removed from your list.";
            }
            catch (Exception ex)
            {
                return $"❌ Could not delete task: {ex.Message}";
            }
        }

        private void SaveTask(string title, string description, DateTime? reminder)
        {
            var task = new Student
            {
                Title = title,
                Description = description,
                IsCompleted = false,
                ReminderDate = reminder,
                CreatedAt = DateTime.Now
            };
            _db.AddTask(task);

            string logMsg = reminder.HasValue
                ? $"Task added: '{title}' (Reminder: {reminder.Value:dd MMM yyyy})"
                : $"Task added: '{title}' (No reminder)";
            LogAction(logMsg);
        }

        /// <summary>
        /// Generates a context-aware cybersecurity description for a task title.
        /// </summary>
        private string BuildTaskDescription(string title)
        {
            string lower = title.ToLower();

            if (lower.Contains("2fa") || lower.Contains("two-factor") || lower.Contains("two factor"))
                return "Enable two-factor authentication to add a critical extra layer of security to your accounts.";
            if (lower.Contains("password"))
                return "Review and update your passwords to ensure they are strong, unique, and not reused.";
            if (lower.Contains("privacy"))
                return "Review account privacy settings to ensure your personal data is protected.";
            if (lower.Contains("backup"))
                return "Back up important files to protect against ransomware and data loss.";
            if (lower.Contains("antivirus") || lower.Contains("virus"))
                return "Run an antivirus scan and ensure your software is up to date.";
            if (lower.Contains("update") || lower.Contains("patch"))
                return "Install pending system and software updates to close known security vulnerabilities.";
            if (lower.Contains("vpn"))
                return "Configure and use a VPN when connecting to public or untrusted Wi-Fi networks.";
            if (lower.Contains("firewall"))
                return "Check and configure your firewall settings for optimal protection.";

            return $"Complete the cybersecurity task: \"{title}\" to improve your online safety.";
        }

        // ══════════════════════════════════════════════════════════
        //  TASK 2: QUIZ
        // ══════════════════════════════════════════════════════════

        private string HandleStartQuiz()
        {
            _quiz.Start();
            LogAction("Quiz started");
            string intro = "🎮 Welcome to the Cybersecurity Quiz! Answer the following questions.\n" +
                           "Type A, B, C, or D (or True/False for T/F questions).\n\n";
            return intro + FormatCurrentQuestion();
        }

        private string HandleQuizAnswer(string lower)
        {
            QuizQuestion current = _quiz.CurrentQuestion();
            if (current == null) return "The quiz is not active. Type 'quiz' to start!";

            int? selectedIndex = ParseAnswerInput(lower, current);
            if (selectedIndex == null)
                return "Please answer with A, B, C, D (or True/False). " + FormatCurrentQuestion();

            bool correct = _quiz.SubmitAnswer(selectedIndex.Value);
            string feedback = correct
                ? $"✅ Correct! {current.Explanation}"
                : $"❌ Incorrect. The correct answer was {current.Options[current.CorrectIndex]}.\n{current.Explanation}";

            if (_quiz.IsFinished)
            {
                string finalMsg = _quiz.GetFinalFeedback();
                LogAction($"Quiz completed — Score: {_quiz.Score}/{_quiz.TotalQuestions}");
                return $"{feedback}\n\n━━━━━━━━━━━━━━━━━━━━━━━━\n🏁 Quiz Over!\n{finalMsg}\n" +
                       "Type 'quiz' to play again!";
            }

            return $"{feedback}\n\n{FormatCurrentQuestion()}";
        }

        private string FormatCurrentQuestion()
        {
            QuizQuestion q = _quiz.CurrentQuestion();
            if (q == null) return "";

            var sb = new StringBuilder();
            sb.AppendLine(q.QuestionText);
            foreach (string opt in q.Options)
                sb.AppendLine($"  {opt}");
            return sb.ToString();
        }

        private int? ParseAnswerInput(string lower, QuizQuestion q)
        {
            // Letter answers: a, b, c, d
            if (lower.StartsWith("a")) return 0;
            if (lower.StartsWith("b")) return 1;
            if (lower.StartsWith("c")) return 2;
            if (lower.StartsWith("d")) return 3;

            // True/False
            if (lower.Contains("true") || lower == "t") return 0;
            if (lower.Contains("false") || lower == "f") return 1;

            // Numeric "1", "2", "3", "4"
            if (int.TryParse(lower.Trim(), out int num) && num >= 1 && num <= 4)
                return num - 1;

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  TASK 4: ACTIVITY LOG
        // ══════════════════════════════════════════════════════════

        private string HandleShowLog()
        {
            try
            {
                var entries = _db.GetRecentLog(10);
                if (entries.Count == 0)
                    return "📋 No activity logged yet. Start chatting, adding tasks, or playing the quiz!";

                var sb = new StringBuilder("📋 Recent Activity (last 10 actions):\n\n");
                int i = 1;
                foreach (var entry in entries)
                    sb.AppendLine($"  {i++}. {entry.DisplayText}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Could not load activity log: {ex.Message}";
            }
        }

        /// <summary>Writes an action to the DB log (silent — no crash on failure).</summary>
        private void LogAction(string action)
        {
            try { _db.LogAction(action); }
            catch { /* non-critical — swallow DB errors */ }
        }

        // ══════════════════════════════════════════════════════════
        //  PART 2 HELPERS (unchanged from original)
        // ══════════════════════════════════════════════════════════

        private bool IsGreeting(string lower) =>
            lower.Contains("hello") || lower.Contains("hi ") ||
            lower.StartsWith("hi") || lower.Contains("hey");

        private bool IsFarewell(string lower) =>
            lower.Contains("bye") || lower.Contains("goodbye") ||
            lower.Contains("see you") || lower.Contains("farewell");

        private string BuildGreeting()
        {
            string name = userMemory.ContainsKey("user_name") ? userMemory["user_name"] : null;
            return name != null
                ? $"Hello {name}! I'm GuardCyberBot. Ask me about cybersecurity, add tasks, or type 'quiz' to test yourself!"
                : "Hello! I'm GuardCyberBot 🛡️. Before we begin — what's your name?";
        }

        private string BuildFarewell()
        {
            string name = userMemory.ContainsKey("user_name") ? userMemory["user_name"] : null;
            return name != null
                ? $"Goodbye {name}! Stay safe online! 🛡️"
                : "Goodbye! Stay safe online! 🛡️";
        }

        private string GetRandomFromList(List<string> list) =>
            list[random.Next(list.Count)];

        private string HandleFollowUp(string lower)
        {
            bool wantsMore = lower.Contains("tell me more") || lower.Contains("explain more") ||
                             lower.Contains("more detail") || lower.Contains("another tip") ||
                             lower.Contains("more about");

            if (wantsMore && !string.IsNullOrEmpty(currentTopic) &&
                keywordResponses.ContainsKey(currentTopic))
            {
                return $"Here's more about {currentTopic}: " +
                       GetRandomFromList(keywordResponses[currentTopic]);
            }

            bool confused = lower.Contains("what do you mean") ||
                            lower.Contains("i don't understand") ||
                            lower.Contains("not clear");

            if (confused && !string.IsNullOrEmpty(currentTopic) &&
                keywordResponses.ContainsKey(currentTopic))
            {
                return $"Let me clarify about {currentTopic}: " +
                       keywordResponses[currentTopic][0];
            }

            return null;
        }

        private string CheckAndStoreNameWithResponse(string lower, string original)
        {
            bool namePhrase = lower.Contains("my name is") || lower.Contains("i'm ") ||
                              lower.Contains("i am ") || lower.Contains("call me");

            if (!namePhrase || userMemory.ContainsKey("user_name")) return null;

            string[] words = original.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length - 1; i++)
            {
                string cw = words[i].ToLower();
                string next = words[i + 1].Trim('.', '!', '?', ',');
                if ((cw == "is" || cw == "i'm" || cw == "am" || cw == "me") &&
                    next.Length > 1 && char.IsUpper(next[0]))
                {
                    userMemory["user_name"] = next;
                    LogAction($"User introduced themselves as '{next}'");
                    return $"Great to meet you, {next}! How can I help with cybersecurity?";
                }
                if (cw == "call" && i + 1 < words.Length)
                {
                    string name = words[i + 1].Trim('.', '!', '?', ',');
                    if (name.Length > 1)
                    {
                        userMemory["user_name"] = name;
                        LogAction($"User introduced themselves as '{name}'");
                        return $"Nice to meet you, {name}! How can I help with cybersecurity?";
                    }
                }
            }
            return null;
        }

        private void StoreUserInfo(string lower, string original)
        {
            if (userMemory.ContainsKey("user_name")) return;
            // (deduplication handled by CheckAndStoreNameWithResponse above)
        }

        private string CheckMemoryRecall(string lower)
        {
            if ((lower.Contains("what is my name") || lower.Contains("do you remember me")) &&
                userMemory.ContainsKey("user_name"))
            {
                return $"Of course! Your name is {userMemory["user_name"]}.";
            }
            return null;
        }

        private string GetDefaultResponse(string sentiment)
        {
            string[] defaults =
            {
                "I'm not sure I understand. Try asking about passwords, phishing, or privacy — or type 'quiz' for a challenge!",
                "Hmm, I didn't catch that. You can also say 'add task', 'show tasks', or 'show log'.",
                "I want to help! Ask me a cybersecurity question or type 'help' to see what I can do.",
                "Could you rephrase that? I'm great at topics like scams, firewalls, and safe browsing!"
            };

            string response = defaults[random.Next(defaults.Length)];

            if (sentiment == "frustrated")
                response = "I can see you're frustrated — I'm sorry! " + response;
            else if (sentiment == "worried")
                response = "Take a breath — I'm here to help! " + response;

            return response;
        }

        // ══════════════════════════════════════════════════════════
        //  PART 2 KEYWORD INIT (unchanged)
        // ══════════════════════════════════════════════════════════

        private void InitializeResponses()
        {
            keywordResponses = new Dictionary<string, List<string>>
            {
                ["password"] = new List<string>
                {
                    "🔐 Create strong passwords with 12+ characters: uppercase, lowercase, numbers, and symbols!",
                    "🔐 Never reuse passwords. Use a password manager like LastPass or Bitwarden.",
                    "🔐 Enable two-factor authentication (2FA) wherever possible.",
                    "💡 The most common password is still '123456' — please don't use that!"
                },
                ["scam"] = new List<string>
                {
                    "⚠️ Be wary of unsolicited calls/emails asking for personal information.",
                    "⚠️ Never click suspicious links — verify the sender's identity first.",
                    "⚠️ If it sounds too good to be true, it's probably a scam.",
                    "⚠️ Report scams to the relevant authority to protect others!"
                },
                ["privacy"] = new List<string>
                {
                    "🔒 Regularly review your social media privacy settings.",
                    "🔒 Be careful about what personal info you share online.",
                    "🔒 Use a VPN on public Wi-Fi to encrypt your data.",
                    "🔒 Check app permissions — many apps request access they don't need!"
                },
                ["phishing"] = new List<string>
                {
                    "🎣 Check email addresses carefully — scammers use near-identical domains.",
                    "🎣 Never enter credentials on a site reached via an email link.",
                    "🎣 Watch for spelling mistakes and urgent language demanding immediate action.",
                    "🎣 When in doubt, contact the company directly through their official site."
                },
                ["virus"] = new List<string>
                {
                    "🦠 Keep antivirus software updated and run weekly scans.",
                    "🦠 Don't open attachments from unknown senders.",
                    "🦠 Back up your files regularly to defend against ransomware.",
                    "🦠 Be careful with USB drives from unknown sources — they can carry malware!"
                },
                ["firewall"] = new List<string>
                {
                    "🔥 Always keep your firewall enabled to monitor network traffic.",
                    "🔥 Configure firewall rules to block unnecessary ports.",
                    "🔥 Use both hardware and software firewalls for layered protection."
                },
                ["help"] = new List<string>
                {
                    "🛡️ I can discuss: passwords, scams, privacy, phishing, viruses, firewalls.\n" +
                    "   I can also: add/show tasks ('add task'), run a quiz ('quiz'), or show the activity log ('show log')!",
                    "🛡️ Try: 'Tell me about password safety' or 'Add task to enable 2FA'!",
                    "🛡️ Type 'quiz' to test your cybersecurity knowledge in a fun mini-game!"
                }
            };

            randomResponseCategories = new Dictionary<string, List<string>>
            {
                ["phishing_tips"] = new List<string>
                {
                    "🎣 Hover over links before clicking to see the real URL.",
                    "🎣 Legitimate companies never ask for passwords via email.",
                    "🎣 Look for the HTTPS padlock on sites before entering data.",
                    "🎣 When in doubt, type the URL directly into your browser.",
                    "🎣 Poor grammar and spelling are major phishing red flags!"
                },
                ["password_tips"] = new List<string>
                {
                    "💡 Use passphrases like 'Blue-Coffee-Jump-42' — longer and easier to remember!",
                    "💡 Change default passwords on all devices, including your router.",
                    "💡 Never write passwords on sticky notes near your monitor!",
                    "💡 Consider biometric options like fingerprint where available.",
                    "💡 Aim for 15+ character passwords for maximum strength."
                }
            };
        }
    }
}
