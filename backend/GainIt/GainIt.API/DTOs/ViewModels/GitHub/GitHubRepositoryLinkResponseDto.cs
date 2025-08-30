using GainIt.API.DTOs.ViewModels.GitHub;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for repository link operations
    /// </summary>
    public class GitHubRepositoryLinkResponseDto
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Success or error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Repository ID if successfully linked
        /// </summary>
        public Guid? RepositoryId { get; set; }
    }
}
