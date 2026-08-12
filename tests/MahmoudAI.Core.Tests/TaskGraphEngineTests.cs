using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MahmoudAI.Core.Engine;
using MahmoudAI.Core.Models;
using MahmoudAI.Teamwork;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MahmoudAI.Core.Tests
{
    public class TaskGraphEngineTests
    {
        [Fact]
        public async Task ExecuteGraphAsync_ShouldRespectDependencies()
        {
            var logger = NullLogger<TaskGraphEngine>.Instance;
            var engine = new TaskGraphEngine(logger);

            var executedOrder = new List<string>();

            var tasks = new List<MissionTask>
            {
                new MissionTask
                {
                    Id = "t1",
                    Name = "Task 1",
                    Action = async ct => { await Task.Delay(10, ct); executedOrder.Add("t1"); return true; }
                },
                new MissionTask
                {
                    Id = "t2",
                    Name = "Task 2",
                    Dependencies = new List<string> { "t1" },
                    Action = async ct => { await Task.Delay(10, ct); executedOrder.Add("t2"); return true; }
                }
            };

            bool success = await engine.ExecuteGraphAsync(tasks, CancellationToken.None);

            success.Should().BeTrue();
            executedOrder.Should().ContainInOrder("t1", "t2");
        }

        [Fact]
        public async Task AgentOrchestrator_ShouldExecuteSequentialWorkflow()
        {
            var logger = NullLogger<AgentOrchestrator>.Instance;
            var manager = new ManagerAgent(NullLogger<ManagerAgent>.Instance);
            var planner = new PlannerAgent(NullLogger<PlannerAgent>.Instance);

            var orchestrator = new AgentOrchestrator(logger, new IAgent[] { manager, planner });

            string result = await orchestrator.RunSequentialWorkflowAsync("Build App", new[] { "Planner", "Manager" }, CancellationToken.None);

            result.Should().Contain("Manager");
        }
    }
}
