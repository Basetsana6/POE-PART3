using System;

namespace WpfApp3
{
    /// <summary>
    /// Represents a cybersecurity task stored in the MySQL database.
    /// </summary>
    public class Student
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? ReminderDate { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Display-friendly reminder text for the UI.</summary>
        public string ReminderDisplay =>
            ReminderDate.HasValue
                ? $"Reminder: {ReminderDate.Value:dd MMM yyyy}"
                : "No reminder";

        /// <summary>Display-friendly status for the UI.</summary>
        public string StatusDisplay => IsCompleted ? "✅ Done" : "🔄 Pending";

        public override string ToString() =>
            $"[{StatusDisplay}] {Title} — {ReminderDisplay}";
    }
}

