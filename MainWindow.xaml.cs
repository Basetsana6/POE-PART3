using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp3
{
    /// <summary>
    /// Code-behind for MainWindow.xaml.
    /// Wires together the ChatbotEngine, QuizEngine, DatabaseManager,
    /// NLPProcessor and all GUI controls across the four tabs.
    /// </summary>
    public partial class MainWindow : Window
    {
        // ── Core components ──────────────────────────────────────
        private ChatbotEngine _chatbot;
        private DatabaseManager _db;
        private QuizEngine _quiz;

        // ── Quiz state (for the dedicated Quiz tab) ───────────────
        private int _quizScore;
        private int _quizAnswered;

        // ── Constructor ───────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();

            _chatbot = new ChatbotEngine();
            _db = new DatabaseManager();
            _quiz = new QuizEngine();

            
            Loaded += OnLoaded;
        }

        // ── Startup ───────────────────────────────────────────────
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Check DB connectivity
            bool dbOk = await Task.Run(() => _db.TestConnection());
            UpdateDbIndicator(dbOk);

            // Welcome message in chat
            string greeting =
                "Hello! I'm GuardCyberBot 🛡️ — your cybersecurity awareness assistant.\n" +
                "I can chat about cybersecurity, manage your tasks, run a quiz, and log my actions.\n\n" +
                "💡 Quick commands:\n" +
                "  • 'add task to enable 2FA'\n" +
                "  • 'show tasks' / 'delete task 2'\n" +
                "  • 'quiz' — start the knowledge quiz\n" +
                "  • 'show log' — view activity history\n\n" +
                "What's your name?";

            AppendChat("🛡️ GuardCyberBot", greeting);
            

            // Populate Task and Log tabs
            RefreshTaskGrid();
            RefreshLogGrid();

            // Focus input
            txtMessage.Focus();
            SetStatus("Ready — type a message below or use the tabs.");
        }

        // ══════════════════════════════════════════════════════════
        //  CHAT TAB
        // ══════════════════════════════════════════════════════════

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await SendMessage();
        }

        private async Task SendMessage()
        {
            string userMsg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(userMsg)) return;

            AppendChat("You", userMsg);
            txtMessage.Clear();
            SetStatus("Thinking...");

            string response = await Task.Run(() => _chatbot.GetResponse(userMsg));

            AppendChat("🛡️ GuardCyberBot", response);
                       SetStatus("Ready.");

            // Auto-refresh tabs after task/log-related commands
            string lower = userMsg.ToLower();
            if (lower.Contains("task") || lower.Contains("remind"))
                RefreshTaskGrid();
            if (lower.Contains("log") || lower.Contains("done") ||
                lower.Contains("complete") || lower.Contains("delete") ||
                lower.Contains("quiz"))
                RefreshLogGrid();
        }

        // Quick-action buttons ─────────────────────────────────────
        private async void btnQuickQuiz_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "quiz";
            await SendMessage();
        }

        private async void btnQuickTasks_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "show tasks";
            await SendMessage();
        }

        private async void btnQuickLog_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "show log";
            await SendMessage();
        }

        private async void btnQuickHelp_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "help";
            await SendMessage();
        }

        // ══════════════════════════════════════════════════════════
        //  TASK TAB
        // ══════════════════════════════════════════════════════════

        private void btnAddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = txtTaskTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a task title.", "Missing Title",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime? reminder = dpReminder.SelectedDate;
            string description = BuildDescription(title);

            var task = new Student
            {
                Title = title,
                Description = description,
                IsCompleted = false,
                ReminderDate = reminder,
                CreatedAt = DateTime.Now
            };

            try
            {
                _db.AddTask(task);
                _db.LogAction(reminder.HasValue
                    ? $"Task added via Task tab: '{title}' (Reminder: {reminder.Value:dd MMM yyyy})"
                    : $"Task added via Task tab: '{title}' (No reminder)");

                txtTaskTitle.Clear();
                dpReminder.SelectedDate = null;
                RefreshTaskGrid();
                RefreshLogGrid();
                SetStatus($"Task '{title}' added successfully.");

                // Echo into chat
                AppendChat("🛡️ GuardCyberBot",
                    $"✅ Task added: \"{title}\"\n   {description}" +
                    (reminder.HasValue ? $"\n   🔔 Reminder: {reminder.Value:dd MMM yyyy}" : ""));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save task:\n{ex.Message}\n\nEnsure MySQL is running and the database is set up.",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCompleteTask_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgTasks.SelectedItem as Student;
            if (selected == null)
            {
                MessageBox.Show("Please select a task to mark as complete.", "No Selection",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                _db.CompleteTask(selected.Id);
                _db.LogAction($"Task #{selected.Id} marked complete: '{selected.Title}'");
                RefreshTaskGrid();
                RefreshLogGrid();
                SetStatus($"Task #{selected.Id} marked as completed.");
                AppendChat("🛡️ GuardCyberBot",
                    $"✅ Task #{selected.Id} \"{selected.Title}\" marked as complete. Well done!");
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void btnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgTasks.SelectedItem as Student;
            if (selected == null)
            {
                MessageBox.Show("Please select a task to delete.", "No Selection",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete task \"{selected.Title}\"?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _db.DeleteTask(selected.Id);
                _db.LogAction($"Task #{selected.Id} deleted: '{selected.Title}'");
                RefreshTaskGrid();
                RefreshLogGrid();
                SetStatus($"Task #{selected.Id} deleted.");
                AppendChat("🛡️ GuardCyberBot",
                    $"🗑️ Task #{selected.Id} \"{selected.Title}\" has been removed.");
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void btnRefreshTasks_Click(object sender, RoutedEventArgs e)
        {
            RefreshTaskGrid();
            SetStatus("Task list refreshed.");
        }

        private void RefreshTaskGrid()
        {
            try
            {
                dgTasks.ItemsSource = _db.GetAllTasks();
            }
            catch (Exception ex)
            {
                // Don't crash — just show a placeholder
                SetStatus($"DB unavailable: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  QUIZ TAB
        // ══════════════════════════════════════════════════════════

        private void btnStartQuiz_Click(object sender, RoutedEventArgs e)
        {
            _quiz.Start();
            _quizScore = 0;
            _quizAnswered = 0;

            _db.LogAction("Quiz started via Quiz tab");
            RefreshLogGrid();

            btnStartQuiz.Visibility = Visibility.Collapsed;
            txtFeedback.Text = "";
            SetQuizQuestion();
            SetStatus("Quiz in progress — answer each question!");
        }

        private void btnAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (!_quiz.IsActive) return;

            Button clicked = (Button)sender;
            int selectedIndex = clicked.Name == "btnA" ? 0 :
                                clicked.Name == "btnB" ? 1 :
                                clicked.Name == "btnC" ? 2 : 3;

            QuizQuestion current = _quiz.CurrentQuestion();
            if (current == null) return;

            // Only show options A–B for True/False
            if (current.IsTrueFalse && selectedIndex > 1) return;

            bool correct = _quiz.SubmitAnswer(selectedIndex);
            if (correct) _quizScore++;
            _quizAnswered++;

            txtFeedback.Text = correct
                ? $"✅ Correct! {current.Explanation}"
                : $"❌ Incorrect. {current.Options[current.CorrectIndex]} was right.\n{current.Explanation}";

            txtFeedback.Foreground = correct
                ? new SolidColorBrush(Color.FromRgb(0, 255, 65))
                : new SolidColorBrush(Color.FromRgb(255, 80, 80));

            UpdateScoreDisplay();

            if (_quiz.IsFinished)
            {
                ShowQuizFinished();
            }
            else
            {
                SetQuizQuestion();
            }
        }

        private void SetQuizQuestion()
        {
            QuizQuestion q = _quiz.CurrentQuestion();
            if (q == null) return;

            txtQuestion.Text = q.QuestionText;
            txtQuizStatus.Text = $"Question {_quiz.CurrentIndex + 1} of {_quiz.TotalQuestions}";

            // Set button labels from options
            var btns = new[] { btnA, btnB, btnC, btnD };
            for (int i = 0; i < btns.Length; i++)
            {
                if (i < q.Options.Count)
                {
                    btns[i].Content = q.Options[i];
                    btns[i].Visibility = Visibility.Visible;
                    btns[i].IsEnabled = true;
                }
                else
                {
                    btns[i].Visibility = Visibility.Hidden;
                }
            }
        }

        private void UpdateScoreDisplay()
        {
            txtScore.Text = $"Score: {_quizScore} / {_quizAnswered}";
        }

        private void ShowQuizFinished()
        {
            string feedback = _quiz.GetFinalFeedback();

            txtQuestion.Text = $"🏁 Quiz Complete!\n\n{feedback}";
            txtQuizStatus.Text = "Quiz finished!";

            foreach (var btn in new[] { btnA, btnB, btnC, btnD })
                btn.Visibility = Visibility.Hidden;

            btnStartQuiz.Content = "🔄 Play Again";
            btnStartQuiz.Visibility = Visibility.Visible;

            _db.LogAction($"Quiz completed — Score: {_quizScore}/{_quiz.TotalQuestions}");
            RefreshLogGrid();
            SetStatus("Quiz finished! See your results above.");

            AppendChat("🛡️ GuardCyberBot",
                $"🏁 Quiz complete! Your score: {_quizScore}/{_quiz.TotalQuestions}\n{feedback}");
        }

        // ══════════════════════════════════════════════════════════
        //  ACTIVITY LOG TAB
        // ══════════════════════════════════════════════════════════

        private void btnRefreshLog_Click(object sender, RoutedEventArgs e)
        {
            RefreshLogGrid();
            SetStatus("Activity log refreshed.");
        }

        private void RefreshLogGrid()
        {
            try
            {
                var entries = _db.GetRecentLog(10);
                dgLog.ItemsSource = entries;
                txtLogInfo.Text = $"  Showing last {entries.Count} actions";
            }
            catch
            {
                txtLogInfo.Text = "  (DB unavailable — ensure MySQL is running)";
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private void AppendChat(string sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtChat.AppendText($"{sender}:\n{message}\n\n{"─".PadRight(60, '─')}\n\n");
                txtChat.ScrollToEnd();
            });
        }

        private void SetStatus(string msg)
        {
            Dispatcher.Invoke(() => txtStatusBar.Text = msg);
        }

        private void UpdateDbIndicator(bool ok)
        {
            Dispatcher.Invoke(() =>
            {
                ellDbStatus.Fill = ok
                    ? new SolidColorBrush(Color.FromRgb(0, 255, 65))
                    : new SolidColorBrush(Color.FromRgb(255, 80, 80));
                txtDbStatus.Text = ok ? "DB: Connected" : "DB: Offline (check MySQL)";
            });
        }

        private void ShowDbError(Exception ex)
        {
            MessageBox.Show($"Database error:\n{ex.Message}\n\nEnsure MySQL is running.",
                            "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>Generates a cybersecurity description based on the task title.</summary>
        private string BuildDescription(string title)
        {
            string lower = title.ToLower();

            if (lower.Contains("2fa") || lower.Contains("two-factor") || lower.Contains("two factor"))
                return "Enable two-factor authentication to add an essential extra layer of account security.";
            if (lower.Contains("password"))
                return "Review and strengthen passwords — use unique, 12+ character passwords per account.";
            if (lower.Contains("privacy"))
                return "Review account privacy settings to control who can see your personal data.";
            if (lower.Contains("backup"))
                return "Back up important files to protect against ransomware and accidental data loss.";
            if (lower.Contains("antivirus") || lower.Contains("virus"))
                return "Run an antivirus scan and ensure protection software is fully up to date.";
            if (lower.Contains("update") || lower.Contains("patch"))
                return "Install pending updates to patch known security vulnerabilities.";
            if (lower.Contains("vpn"))
                return "Set up a VPN for encrypted browsing on public or untrusted networks.";
            if (lower.Contains("firewall"))
                return "Review and configure firewall rules for optimal network protection.";

            return $"Complete this cybersecurity task to improve your overall online safety.";
        }

    }
}
