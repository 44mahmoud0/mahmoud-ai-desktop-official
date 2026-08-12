using System;
using System.Threading;
using System.Threading.Tasks;
using MahmoudAI.Core.Engine;
using MahmoudAI.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace MahmoudAI.Core
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("=== Mahmoud AI (.NET 10 LTS Core Engine) ===");
            var logger = NullLogger<TaskGraphEngine>.Instance;
            var engine = new TaskGraphEngine(logger);

            var mission = new MissionContext
            {
                Title = "Autonomous System Initialization",
                Objective = "Initialize core subsystems and verify task graph execution."
            };

            Console.WriteLine($"Starting mission: {mission.Title}");
            
            var tasks = new[]
            {
                new MissionTask
                {
                    Id = "init_runtime",
                    Name = "Initialize Runtime DI & Security",
                    Action = async ct => { await Task.Delay(100, ct); Console.WriteLine("[Task] Runtime initialized."); return true; }
                },
                new MissionTask
                {
                    Id = "init_agents",
                    Name = "Register Multi-Agent Teamwork",
                    Dependencies = { "init_runtime" },
                    Action = async ct => { await Task.Delay(100, ct); Console.WriteLine("[Task] Agents registered."); return true; }
                }
            };

            bool success = await engine.ExecuteGraphAsync(tasks, CancellationToken.None);
            if (success)
            {
                Console.WriteLine("Mission completed successfully.");
                return 0;
            }
            else
            {
                Console.WriteLine("Mission execution failed.");
                return 1;
            }
        }
    }
}
