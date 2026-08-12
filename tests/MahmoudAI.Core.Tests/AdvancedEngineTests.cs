using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MahmoudAI.Core.Engine;
using MahmoudAI.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MahmoudAI.Core.Tests
{
    public class AdvancedEngineTests
    {
        [Fact]
        public async Task TaskGraphEngine_ShouldHandleRetriesAndTimeouts()
        {
            var logger = NullLogger<TaskGraphEngine>.Instance;
            var engine = new TaskGraphEngine(logger);

            int attempts = 0;
            var tasks = new List<MissionTask>
            {
                new MissionTask
                {
                    Id = "retry_task",
                    Name = "Flaky Task",
                    MaxRetries = 2,
                    Action = async ct =>
                    {
                        attempts++;
                        if (attempts < 3)
                        {
                            throw new InvalidOperationException("Temporary failure");
                        }
                        await Task.Delay(5, ct);
                        return true;
                    }
                }
            };

            bool success = await engine.ExecuteGraphAsync(tasks, CancellationToken.None);

            success.Should().BeTrue();
            attempts.Should().Be(3);
        }

        [Fact]
        public async Task SqliteMissionStore_ShouldStoreMissionsAndMemory()
        {
            string dbPath = Path.Combine(Path.GetTempPath(), $"test_mission_{Guid.NewGuid():N}.db");
            try
            {
                var logger = NullLogger<SqliteMissionStore>.Instance;
                var store = new SqliteMissionStore(dbPath, logger);

                await store.SaveMissionAsync("m_1", "Test Mission", "Verify SQLite persistence", "Running", "High", CancellationToken.None);
                await store.SaveMemoryAsync("mem_1", "Important context data", "tag:test", CancellationToken.None);

                File.Exists(dbPath).Should().BeTrue();
            }
            finally
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }
    }
}
