using System;
using System.Collections.Generic;

namespace MahmoudAI.Core.Models
{
    public enum MissionStatus
    {
        Created,
        Planning,
        WaitingForApproval,
        Ready,
        Running,
        Paused,
        Waiting,
        Completed,
        Failed,
        Cancelled,
        RolledBack
    }

    public enum MissionPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public record MissionId(string Value)
    {
        public static MissionId New() => new($"mission_{Guid.NewGuid():N}");
        public override string ToString() => Value;
    }

    public class MissionContext
    {
        public MissionId Id { get; init; } = MissionId.New();
        public string Title { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public MissionStatus Status { get; set; } = MissionStatus.Created;
        public MissionPriority Priority { get; set; } = MissionPriority.Normal;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Artifacts { get; } = new();
        public string? ErrorMessage { get; set; }
    }
}
