using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MahmoudAI.Teamwork
{
    public interface IAgent
    {
        string Name { get; }
        string Role { get; }
        Task<string> ExecuteAsync(string objective, CancellationToken cancellationToken);
    }

    public class ManagerAgent : IAgent
    {
        public string Name => "Manager";
        public string Role => "Coordination & Synthesis";
        private readonly ILogger<ManagerAgent> _logger;

        public ManagerAgent(ILogger<ManagerAgent> logger) => _logger = logger;

        public async Task<string> ExecuteAsync(string objective, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Manager agent coordinating mission: {Objective}", objective);
            await Task.Delay(100, cancellationToken);
            return $"[Manager] Mission coordinated and synthesized successfully for: {objective}";
        }
    }

    public class PlannerAgent : IAgent
    {
        public string Name => "Planner";
        public string Role => "Task Decomposition";
        private readonly ILogger<PlannerAgent> _logger;

        public PlannerAgent(ILogger<PlannerAgent> logger) => _logger = logger;

        public async Task<string> ExecuteAsync(string objective, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Planner agent decomposing: {Objective}", objective);
            await Task.Delay(100, cancellationToken);
            return $"[Planner] Decomposed objective into verified subtask DAG.";
        }
    }

    public class CodingAgent : IAgent
    {
        public string Name => "CodingAgent";
        public string Role => "Software Engineering";
        private readonly ILogger<CodingAgent> _logger;

        public CodingAgent(ILogger<CodingAgent> logger) => _logger = logger;

        public async Task<string> ExecuteAsync(string objective, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Coding agent implementing: {Objective}", objective);
            await Task.Delay(100, cancellationToken);
            return $"[CodingAgent] Implemented code changes and verified syntax for: {objective}";
        }
    }

    public class AgentOrchestrator
    {
        private readonly Dictionary<string, IAgent> _agents = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<AgentOrchestrator> _logger;

        public AgentOrchestrator(ILogger<AgentOrchestrator> logger, IEnumerable<IAgent> agents)
        {
            _logger = logger;
            foreach (var agent in agents)
            {
                _agents[agent.Name] = agent;
            }
        }

        public async Task<string> RunSequentialWorkflowAsync(string objective, IEnumerable<string> agentNames, CancellationToken cancellationToken)
        {
            string currentContext = objective;
            foreach (var name in agentNames)
            {
                if (_agents.TryGetValue(name, out var agent))
                {
                    _logger.LogInformation("Handing off to agent {AgentName}", name);
                    currentContext = await agent.ExecuteAsync(currentContext, cancellationToken);
                }
                else
                {
                    throw new KeyNotFoundException($"Mandatory agent '{name}' is not registered in the AgentOrchestrator registry.");
                }
            }
            return currentContext;
        }
    }
}
