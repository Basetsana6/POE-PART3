using System;

namespace WpfApp3
{
    /// <summary>
    /// Represents a single logged action recorded in the MySQL database.
    /// </summary>
    public class ActivityLogEntry
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }

        public string DisplayText =>
        $"[{Timestamp:HH:mm dd/MM}] {Action}";
    }
}
