namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// DTO for GitHub sync status information
    /// </summary>
    public class GitHubSyncStatusDto
    {
        public string SyncType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public int ItemsProcessed { get; set; }
        public int TotalItems { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Response DTO for sync status operations
    /// </summary>
    public class SyncStatusResponseDto
    {
        public Guid ProjectId { get; set; }
        public GitHubSyncStatusDto SyncStatus { get; set; } = new();
    }
}
