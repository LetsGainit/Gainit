using GainIt.API.DTOs.ViewModels.GitHub;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for repository link operations
    /// </summary>
    public class GitHubRepositoryLinkResponseDto
    {
        /// <summary>
        /// Success message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Repository information
        /// </summary>
        public GitHubRepositoryInfoDto Repository { get; set; } = new();
    }
}
