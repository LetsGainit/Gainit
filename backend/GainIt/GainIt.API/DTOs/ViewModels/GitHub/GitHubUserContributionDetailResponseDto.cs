using GainIt.API.DTOs.ViewModels.GitHub.Base;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for user contribution details
    /// </summary>
    public class GitHubUserContributionDetailResponseDto : GitHubBaseResponseDto
    {
        /// <summary>
        /// The unique identifier of the user
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The contribution data
        /// </summary>
        public GitHubDetailedContributionDto Contribution { get; set; } = new();
    }
}
