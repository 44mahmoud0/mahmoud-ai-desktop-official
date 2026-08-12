using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MahmoudAI.Core.Engine
{
    public enum TaskStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public class MissionTask
    {
        public string Id { get; init; } = Guid.NewGuid().ToString("N");
        public string Name { get; init; } = string.Empty;
        public Func<CancellationToken, Task<bool>> Action { get; init; } = _ => Task.FromResult(true);
        public List<string> Dependencies { get; init; } = new();
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public string? Error { get; set; }
    }

    public class TaskGraphEngine
    {
        private readonly ILogger<TaskGraphEngine> _logger;

        public TaskGraphEngine(ILogger<TaskGraphEngine> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ExecuteGraphAsync(IEnumerable<MissionTask> tasks, CancellationToken cancellationToken)
        {
            var taskDict = new Dictionary<string, MissionTask>();
            foreach (var t in tasks)
            {
                taskDict[t.Id] = t;
            }

            var completed = new HashSet<string>();
            var running = new HashSet<string>();

            while (completed.Count < taskDict.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool progressMade = false;

                foreach (var kvp in taskDict)
                {
                    var task = kvp.Value;
                    if (task.Status != TaskStatus.Pending) continue;

                    bool depsSatisfied = true;
                    foreach (var dep in task.Dependencies)
                    {
                        if (!completed.Contains(dep))
                        {
                            depsSatisfied = false;
                            break;
                        }
                    }

                    if (depsSatisfied && !running.Contains(task.Id))
                    {
                        running.Add(task.Id);
                        progressMade = true;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                task.Status = TaskStatus.Running;
                                _logger.LogInformation("Executing task {TaskId}: {TaskName}", task.Id, task.Name);
                                bool success = await task.Action(cancellationToken);
                                task.Status = success ? TaskStatus.Completed : TaskStatus.Failed;
                                lock (completed)
                                {
                                    if (success) completed.Add(task.Id);
                                }
                            }
                            catch (Exception ex)
                            {
                                task.Status = TaskStatus.Failed;
                                task.Error = ex.Message;
                                _logger.LogError(ex, "Task {TaskId} failed with exception", task.Id);
                            }
                            finally
                            {
                                lock (running) { running.Remove(task.Id); }
                            }
                        }, cancellationToken);
                    }
                }

                if (!progressMade && running.Count == 0)
                {
                    // Deadlock or unresolvable dependencies
                    _logger.LogError("Task graph execution stalled due to unresolved dependencies or failure.");
                    return false;
                }

                await Task.Delay(50, cancellationToken);
            }

            return true;
        }
    }
}
