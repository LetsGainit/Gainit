namespace GainIt.API.DTOs.ViewModels.GitHub.Base
{
    /// <summary>
    /// Base DTO for GitHub responses with common properties
    /// </summary>
    public abstract class GitHubBaseResponseDto
    {
        /// <summary>
        /// The unique identifier of the project
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Number of days analyzed (if applicable)
        /// </summary>
        public int? DaysPeriod { get; set; }

        /// <summary>
        /// Timestamp when the response was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
