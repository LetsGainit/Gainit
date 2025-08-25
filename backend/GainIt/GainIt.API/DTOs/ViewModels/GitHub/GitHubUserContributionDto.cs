namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// DTO for GitHub user contribution information
    /// </summary>
    public class GitHubUserContributionDto
    {
        public Guid UserId { get; set; }
        public string? GitHubUsername { get; set; }
        public int TotalCommits { get; set; }
        public int TotalLinesChanged { get; set; }
        public int TotalIssuesCreated { get; set; }
        public int TotalPullRequestsCreated { get; set; }
        public int TotalReviews { get; set; }
        public int UniqueDaysWithCommits { get; set; }
        public DateTime CalculatedAtUtc { get; set; }
    }
}
