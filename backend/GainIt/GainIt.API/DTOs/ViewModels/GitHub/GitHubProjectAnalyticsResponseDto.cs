using GainIt.API.DTOs.ViewModels.GitHub.Base;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Response DTO for project analytics
    /// </summary>
    public class GitHubProjectAnalyticsResponseDto : GitHubBaseResponseDto
    {
        /// <summary>
        /// The analytics data
        /// </summary>
        public object Analytics { get; set; } = new();

        /// <summary>
        /// When the analytics were calculated
        /// </summary>
        public DateTime CalculatedAt { get; set; }
    }
}
