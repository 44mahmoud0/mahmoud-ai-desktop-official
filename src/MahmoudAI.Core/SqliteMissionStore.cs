using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace MahmoudAI.Storage
{
    public class SqliteMissionStore
    {
        private readonly string _connectionString;
        private readonly ILogger<SqliteMissionStore> _logger;

        public SqliteMissionStore(string dbPath, ILogger<SqliteMissionStore> logger)
        {
            _connectionString = $"Data Source={dbPath}";
            _logger = logger;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS missions (
                    id TEXT PRIMARY KEY,
                    title TEXT NOT NULL,
                    objective TEXT NOT NULL,
                    status TEXT NOT NULL,
                    priority TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    completed_at TEXT,
                    error_message TEXT
                );
                CREATE TABLE IF NOT EXISTS memory_vectors (
                    id TEXT PRIMARY KEY,
                    content TEXT NOT NULL,
                    metadata TEXT,
                    created_at TEXT NOT NULL
                );
            ";
            command.ExecuteNonQuery();
            _logger.LogInformation("SQLite mission store initialized successfully.");
        }

        public async Task SaveMissionAsync(string id, string title, string objective, string status, string priority, CancellationToken ct)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO missions (id, title, objective, status, priority, created_at)
                VALUES (@id, @title, @objective, @status, @priority, @createdAt);
            ";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@objective", objective);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@priority", priority);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(ct);
        }

        public async Task SaveMemoryAsync(string id, string content, string metadata, CancellationToken ct)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO memory_vectors (id, content, metadata, created_at)
                VALUES (@id, @content, @metadata, @createdAt);
            ";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@content", content);
            command.Parameters.AddWithValue("@metadata", metadata ?? string.Empty);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(ct);
        }
    }
}
