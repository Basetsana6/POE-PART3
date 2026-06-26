using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using WpfApp3;

namespace WpfApp3
{
    /// <summary>
    /// Handles all MySQL database operations for tasks and activity log.
    /// Connection string: update the server/uid/pwd to match your MySQL setup.
    /// </summary>
    public class DatabaseManager
    {
        // ── Change these values to match your MySQL installation ──────────
        private const string ConnectionString =
        "server=localhost;" +
        "database=GuardCyberBotDB;" +
        "uid=root;" +
        "pwd=BasetsanaAM_6!;";
        // ─────────────────────────────────────────────────────────────────

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
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        // TASK OPERATIONS
        // ══════════════════════════════════════════════════════════

        /// <summary>Inserts a new cybersecurity task into the Tasks table.</summary>
        public int AddTask(Student task)
        {
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql =
                "INSERT INTO Tasks (Title, Description, IsCompleted, ReminderDate, CreatedAt) " +
                "VALUES (@title, @desc, @done, @reminder, @created); " +
                "SELECT LAST_INSERT_ID();";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@title", task.Title);
                    cmd.Parameters.AddWithValue("@desc", task.Description);
                    cmd.Parameters.AddWithValue("@done", task.IsCompleted);
                    cmd.Parameters.AddWithValue("@reminder", (object)task.ReminderDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@created", task.CreatedAt);

                    return Convert.ToInt32(cmd.ExecuteScalar());
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
                string sql = "SELECT * FROM Tasks ORDER BY CreatedAt DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(new Student
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"].ToString(),
                            IsCompleted = Convert.ToBoolean(reader["IsCompleted"]),
                            ReminderDate = reader["ReminderDate"] == DBNull.Value
                        ? (DateTime?)null
                        : Convert.ToDateTime(reader["ReminderDate"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        });
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
                string sql = "UPDATE Tasks SET IsCompleted = TRUE WHERE Id = @id";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", taskId);
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
                    cmd.Parameters.AddWithValue("@id", taskId);
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
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql =
                "INSERT INTO ActivityLog (Action, Timestamp) " +
                "VALUES (@action, @ts)";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Returns the most recent <paramref name="limit"/> log entries (newest first).
        /// Default is 10 entries.
        /// </summary>
        public List<ActivityLogEntry> GetRecentLog(int limit = 10)
        {
            var entries = new List<ActivityLogEntry>();

            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql =
                "SELECT * FROM ActivityLog " +
                "ORDER BY Timestamp DESC " +
                $"LIMIT {limit}";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(new ActivityLogEntry
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Action = reader["Action"].ToString(),
                            Timestamp = Convert.ToDateTime(reader["Timestamp"])
                        });
                    }
                }
            }

            return entries;
        }
    }
}
