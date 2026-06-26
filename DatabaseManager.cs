using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WpfApp3;

namespace WpfApp3
{
    /// <summary>
    /// Handles all MySQL database operations for tasks and activity log.
    /// Connection string: read from environment variable "GUARDCYBERBOT_CONNECTION" if present,
    /// otherwise falls back to the embedded default (not recommended for production).
    /// </summary>
    public class DatabaseManager
    {
        // ── Change these values to match your MySQL installation or set the environment variable shown above ──────────
        private const string DefaultConnectionString =
        "server=localhost;" +
        "database=GuardCyberBotDB;" +
        "uid=root;" +
        "pwd=BasetsanaAM_6!;";
        // ─────────────────────────────────────────────────────────────────

        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("GUARDCYBERBOT_CONNECTION") ?? DefaultConnectionString;

        // Constructor: ensure schema exists
        public DatabaseManager()
        {
            try
            {
                EnsureDatabaseAndTables();
            }
            catch
            {
                // Swallow - calling code will handle connection failures. This avoids throwing during construction.
            }
        }

        // Build server-level connection string (without database=...) so we can create the database if missing.
        private static string GetServerConnectionString()
        {
            string env = Environment.GetEnvironmentVariable("GUARDCYBERBOT_CONNECTION");
            string cs = env ?? DefaultConnectionString;

            // Remove database=...; (case-insensitive)
            var cleaned = Regex.Replace(cs, "(?i)\\bdatabase=[^;]*;?", string.Empty);
            return cleaned;
        }

        // Ensure the database and required tables exist. Safe to call repeatedly.
        private void EnsureDatabaseAndTables()
        {
            string serverCs = GetServerConnectionString();

            using (var conn = new MySqlConnection(serverCs))
            {
                conn.Open();

                // Create database if missing
                using (var cmd = new MySqlCommand("CREATE DATABASE IF NOT EXISTS GuardCyberBotDB;", conn))
                    cmd.ExecuteNonQuery();

                // Use the database and create tables if missing
                using (var cmd = new MySqlCommand("USE GuardCyberBotDB;", conn))
                    cmd.ExecuteNonQuery();

                string createTasks =
                    "CREATE TABLE IF NOT EXISTS Tasks (" +
                    "Id INT AUTO_INCREMENT PRIMARY KEY, " +
                    "Title VARCHAR(255) NOT NULL, " +
                    "Description TEXT, " +
                    "IsCompleted TINYINT(1) DEFAULT 0, " +
                    "ReminderDate DATETIME NULL, " +
                    "CreatedAt DATETIME NOT NULL" +
                    ");";

                using (var cmd = new MySqlCommand(createTasks, conn))
                    cmd.ExecuteNonQuery();

                string createLog =
                    "CREATE TABLE IF NOT EXISTS ActivityLog (" +
                    "Id INT AUTO_INCREMENT PRIMARY KEY, " +
                    "Action TEXT NOT NULL, " +
                    "Timestamp DATETIME NOT NULL" +
                    ");";

                using (var cmd = new MySqlCommand(createLog, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        // ══════════════════════════════════════════════════════════
        // CONNECTION TEST
        // ══════════════════════════════════════════════════════════

        /// <summary>Returns true if a connection to MySQL can be opened.</summary>
        public bool TestConnection()
        {
            try
            {
                using (var conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                // Consider logging the exception in real applications
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        // TASK OPERATIONS
        // ══════════════════════════════════════════════════════════

        /// <summary>Inserts a new cybersecurity task into the Tasks table and returns the new Id.</summary>
        public int AddTask(Student task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql =
                "INSERT INTO Tasks (Title, Description, IsCompleted, ReminderDate, CreatedAt) " +
                "VALUES (@title, @desc, @done, @reminder, @created); " +
                "SELECT LAST_INSERT_ID();";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@title", MySqlDbType.VarChar).Value = task.Title ?? string.Empty;
                    cmd.Parameters.Add("@desc", MySqlDbType.Text).Value = task.Description ?? string.Empty;
                    // store boolean as tinyint(1)
                    cmd.Parameters.Add("@done", MySqlDbType.Bit).Value = task.IsCompleted ? (byte)1 : (byte)0;
                    if (task.ReminderDate.HasValue)
                        cmd.Parameters.Add("@reminder", MySqlDbType.DateTime).Value = task.ReminderDate.Value;
                    else
                        cmd.Parameters.Add("@reminder", MySqlDbType.DateTime).Value = DBNull.Value;

                    cmd.Parameters.Add("@created", MySqlDbType.DateTime).Value = task.CreatedAt;

                    // ExecuteScalar will return LAST_INSERT_ID() from the second statement.
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        /// <summary>Returns all tasks from the database, newest first.</summary>
        public List<Student> GetAllTasks()
        {
            var tasks = new List<Student>();

            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT Id, Title, Description, IsCompleted, ReminderDate, CreatedAt FROM Tasks ORDER BY CreatedAt DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var student = new Student();

                        student.Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        student.Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        student.Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

                        // IsCompleted may be stored as tinyint/bit
                        if (!reader.IsDBNull(3))
                        {
                            var val = reader.GetValue(3);
                            student.IsCompleted = val is bool b ? b : Convert.ToInt32(val) != 0;
                        }

                        student.ReminderDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
                        student.CreatedAt = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);

                        tasks.Add(student);
                    }
                }
            }

            return tasks;
        }

        /// <summary>Marks a task as completed (IsCompleted = true).</summary>
        public void CompleteTask(int taskId)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = "UPDATE Tasks SET IsCompleted = 1 WHERE Id = @id";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = taskId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Permanently deletes a task from the database.</summary>
        public void DeleteTask(int taskId)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Tasks WHERE Id = @id";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = taskId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        // ACTIVITY LOG OPERATIONS
        // ══════════════════════════════════════════════════════════

        /// <summary>Writes a new action to the ActivityLog table.</summary>
        public void LogAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action)) return;

            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql =
                "INSERT INTO ActivityLog (Action, Timestamp) " +
                "VALUES (@action, @ts)";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@action", MySqlDbType.Text).Value = action;
                    cmd.Parameters.Add("@ts", MySqlDbType.DateTime).Value = DateTime.Now;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Returns the most recent <paramref name="limit"/> log entries (newest first).
        /// Default is 10 entries. Limit is validated to avoid unreasonable values.
        /// </summary>
        public List<ActivityLogEntry> GetRecentLog(int limit = 10)
        {
            var entries = new List<ActivityLogEntry>();

            // basic validation to avoid SQL injection via string interpolation and protect the DB
            if (limit <= 0) limit = 10;
            if (limit > 1000) limit = 1000;

            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql =
                "SELECT Id, Action, Timestamp FROM ActivityLog " +
                "ORDER BY Timestamp DESC " +
                "LIMIT @limit";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@limit", MySqlDbType.Int32).Value = limit;

                    // Some MySQL versions/drivers do not support parameterizing the LIMIT clause.
                    // Use the validated integer directly in the SQL statement instead.
                    sql = $"SELECT Id, Action, Timestamp FROM ActivityLog ORDER BY Timestamp DESC LIMIT {limit}";
                    using (var cmd2 = new MySqlCommand(sql, conn))
                    using (var reader = cmd2.ExecuteReader())
                     {
                         while (reader.Read())
                         {
                             var entry = new ActivityLogEntry();

                             entry.Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                             entry.Action = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                             entry.Timestamp = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2);

                             entries.Add(entry);
                         }
                     }
                 }
             }

             return entries;
         }
     }
 }
