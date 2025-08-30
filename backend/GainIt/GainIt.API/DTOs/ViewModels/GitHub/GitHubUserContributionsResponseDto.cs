using GainIt.API.DTOs.ViewModels.GitHub.Base;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for user contributions
    /// </summary>
    public class GitHubUserContributionsResponseDto : GitHubBaseResponseDto
    {
        /// <summary>
        /// Number of contributors
        /// </summary>
        public int Contributors { get; set; }

        /// <summary>
        /// List of user contributions
        /// </summary>
        public List<GitHubUserContributionDto> Contributions { get; set; } = new();
    }
}
