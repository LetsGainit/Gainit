using GainIt.API.DTOs.ViewModels.GitHub;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for sync status
    /// </summary>
    public class SyncStatusResponseDto
    {
        public Guid ProjectId { get; set; }
        public GitHubSyncStatusDto SyncStatus { get; set; } = new();
    }
}
