using System.Collections.Generic;

namespace WpfApp3
{
    /// <summary>
    /// Manages the cybersecurity quiz: question bank, scoring, and progression.
    /// </summary>
    public class QuizEngine
    {
        // ── State ────────────────────────────────────────────────
        private List<QuizQuestion> _questions;
        private int _currentIndex;
        private int _score;

        public bool IsActive { get; private set; }
        public int TotalQuestions => _questions.Count;
        public int CurrentIndex => _currentIndex;
        public int Score => _score;

        // ── Initialise ───────────────────────────────────────────
        public QuizEngine()
        {
            BuildQuestionBank();
        }

        private void BuildQuestionBank()
        {
            // 12 questions: mix of multiple-choice and true/false
            _questions = new List<QuizQuestion>
            {
                // 1 — Phishing (MC)
                new QuizQuestion
                {
                    QuestionText = "❓ Q1/12 — What should you do if you receive an email asking for your password?",
                    Options      = new List<string>
                    {
                        "A) Reply with your password",
                        "B) Delete the email",
                        "C) Report the email as phishing",
                        "D) Ignore it"
                    },
                    CorrectIndex = 2,
                    Explanation  = "✅ Reporting phishing emails alerts your email provider and helps protect others from the same scam."
                },

                // 2 — Password length (TF)
                new QuizQuestion
                {
                    QuestionText = "❓ Q2/12 — True or False: A 6-character password is strong enough for your bank account.",
                    Options      = new List<string> { "A) True", "B) False" },
                    CorrectIndex = 1,
                    Explanation  = "✅ False! Security experts recommend at least 12–16 characters with a mix of letters, numbers, and symbols."
                },

                // 3 — Public Wi-Fi (MC)
                new QuizQuestion
                {
                    QuestionText = "❓ Q3/12 — What is the safest thing to do on public Wi-Fi?",
                    Options      = new List<string>
                    {
                        "A) Log into your bank account",
                        "B) Use a VPN to encrypt your traffic",
                        "C) Share your screen with others",
                        "D) Disable your firewall for faster speed"
                    },
                    CorrectIndex = 1,
                    Explanation  = "✅ A VPN (Virtual Private Network) encrypts all your traffic, keeping it safe even on unsecured networks."
                },

                // 4 — 2FA (TF)
                new QuizQuestion
                {
                    QuestionText = "❓ Q4/12 — True or False: Two-factor authentication (2FA) makes your account significantly more secure.",
                    Options      = new List<string> { "A) True", "B) False" },
                    CorrectIndex = 0,
                    Explanation  = "✅ True! 2FA adds a second verification step, blocking 99% of automated account attacks even if your password is stolen."
                },

                // 5 — Social engineering (MC)
                new QuizQuestion
                {
                    QuestionText = "❓ Q5/12 — A stranger calls pretending to be IT support and asks for your login. What do you do?",
                    Options      = new List<string>
                    {
                        "A) Give them the info — they sound official",
                        "B) Hang up and call your IT department directly",
                        "C) Email them your password instead",
                        "D) Let them remote into your PC immediately"
                    },
                    CorrectIndex = 1,
                    Explanation  = "✅ This is called a vishing (voice phishing) attack. Always verify by calling the official number yourself."
                },

                // 6 — Password reuse (TF)
                new QuizQuestion
                {
                    QuestionText = "❓ Q6/12 — True or False: Using the same password on multiple sites is safe if the password is strong.",
                    Options      = new List<string> { "A) True", "B) False" },
                    CorrectIndex = 1,
                    Explanation  = "✅ False! If one site is breached, attackers use credential stuffing to try your password on all other sites."
                },

                // 7 — HTTPS (MC)
                new QuizQuestion
                {
                    QuestionText = "❓ Q7/12 — What does the padlock icon in your browser's address bar mean?",
                    Options      = new List<string>
                    {
                        "A) The site is owned by a trusted company",
                        "B) Your connection to the site is encrypted (HTTPS)",
                        "C) The site has no viruses",
                        "D) Your personal data is 100% safe"
                    },
                    CorrectIndex = 1,
                    Explanation  = "✅ HTTPS encrypts data in transit, but it does NOT guarantee the site itself is legitimate or trustworthy."
                },

                // 8 — Ransomware (TF)
                new QuizQuestion
                {
                    QuestionText = "❓ Q8/12 — True or False: Regularly backing up your files is one of the best defences against ransomware.",
                    Options      = new List<string> { "A) True", "B) False" },
                    CorrectIndex = 0,
                    Explanation  = "✅ True! Offline or cloud backups let you restore your files without paying a ransom if you are attacked."
                },

                // 9 — Software updates (MC)
                new QuizQuestion
                {
                    QuestionText = "❓ Q9/12 — Why is it important to install software updates promptly?",
                    Options      = new List<string>
                    {
                        "A) Updates only add new features",
                        "B) Updates are optional and rarely matter",
                        "C) Updates patch security vulnerabilities attackers exploit",
                        "D) Updates slow down your computer on purpose"
                    },
                    CorrectIndex = 2,
                    Explanation  = "✅ Many cyberattacks exploit known vulnerabilities. Patches close these holes — update as soon as possible!"
                },

                // 10 — Social media (TF)
                new QuizQuestion
                {
                    QuestionText = "❓ Q10/12 — True or False: Posting your home address and holiday plans on social media is harmless.",
                    Options      = new List<string> { "A) True", "B) False" },
                    CorrectIndex = 1,
                    Explanation  = "✅ False! Oversharing enables burglars to know when you are away and gives identity thieves personal details."
                },

                // 11 — Password manager (MC)
                new QuizQuestion
                {
                    QuestionText = "❓ Q11/12 — What is a password manager used for?",
                    Options      = new List<string>
                    {
                        "A) Sharing passwords with friends easily",
                        "B) Storing and generating strong, unique passwords securely",
                        "C) Remembering your PIN by storing it in the cloud",
                        "D) Logging into sites without any password"
                    },
                    CorrectIndex = 1,
                    Explanation  = "✅ A password manager (e.g. Bitwarden, LastPass) stores unique passwords for every site securely in an encrypted vault."
                },

                // 12 — Antivirus (TF)
                new QuizQuestion
                {
                    QuestionText = "❓ Q12/12 — True or False: Antivirus software alone is enough to keep your computer completely safe.",
                    Options      = new List<string> { "A) True", "B) False" },
                    CorrectIndex = 1,
                    Explanation  = "✅ False! Antivirus is one layer. You also need strong passwords, 2FA, updates, backups, and safe browsing habits."
                }
            };
        }

        // ── Public API ───────────────────────────────────────────

        /// <summary>Resets the quiz and marks it as active.</summary>
        public void Start()
        {
            _currentIndex = 0;
            _score = 0;
            IsActive = true;
        }

        /// <summary>Returns the current question, or null if the quiz is over.</summary>
        public QuizQuestion CurrentQuestion()
        {
            if (!IsActive || _currentIndex >= _questions.Count)
                return null;

            return _questions[_currentIndex];
        }

        /// <summary>
        /// Submits an answer (0-based option index).
        /// Returns true if correct; advances the question pointer.
        /// </summary>
        public bool SubmitAnswer(int selectedIndex)
        {
            if (!IsActive || _currentIndex >= _questions.Count)
                return false;

            bool correct = selectedIndex == _questions[_currentIndex].CorrectIndex;
            if (correct) _score++;

            _currentIndex++;

            if (_currentIndex >= _questions.Count)
                IsActive = false;

            return correct;
        }

        /// <summary>Returns true when all questions have been answered.</summary>
        public bool IsFinished => !IsActive && _currentIndex >= _questions.Count;

        /// <summary>Builds the end-of-quiz feedback message.</summary>
        public string GetFinalFeedback()
        {
            double pct = (double)_score / TotalQuestions * 100;

            if (pct == 100)
                return $"🏆 Perfect score! {_score}/{TotalQuestions} — You're a cybersecurity pro!";
            if (pct >= 80)
                return $"🌟 Great job! {_score}/{TotalQuestions} — You have solid cybersecurity knowledge!";
            if (pct >= 60)
                return $"👍 Not bad! {_score}/{TotalQuestions} — Keep learning to stay safer online.";

            return $"📚 {_score}/{TotalQuestions} — Keep learning! Review the tips above to boost your knowledge.";
        }
    }
}
