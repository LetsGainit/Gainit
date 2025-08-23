using GainIt.API.DTOs.ViewModels.GitHub.Base;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for sync operations
    /// </summary>
    public class GitHubSyncResponseDto : GitHubMessageResponseDto
    {
        /// <summary>
        /// Type of sync performed
        /// </summary>
        public string SyncType { get; set; } = string.Empty;
    }
}
