using GainIt.API.DTOs.ViewModels.GitHub.Base;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for user activity summary
    /// </summary>
    public class GitHubUserActivitySummaryResponseDto : GitHubActivitySummaryBaseDto
    {
        /// <summary>
        /// The unique identifier of the user
        /// </summary>
        public Guid UserId { get; set; }
    }
}
