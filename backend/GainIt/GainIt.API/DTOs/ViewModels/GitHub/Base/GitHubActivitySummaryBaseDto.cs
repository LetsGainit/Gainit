using GainIt.API.DTOs.ViewModels.GitHub.Base;

namespace GainIt.API.DTOs.ViewModels.GitHub.Base
{
    /// <summary>
    /// Base DTO for GitHub activity summary responses
    /// </summary>
    public class GitHubActivitySummaryBaseDto : GitHubBaseResponseDto
    {
        /// <summary>
        /// The activity summary data
        /// </summary>
        public object Summary { get; set; } = new();
    }
}
