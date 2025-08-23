namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// DTO for detailed GitHub contribution information
    /// </summary>
    public class GitHubDetailedContributionDto
    {
        public string? GitHubUsername { get; set; }
        public int TotalCommits { get; set; }
        public int TotalAdditions { get; set; }
        public int TotalDeletions { get; set; }
        public int TotalLinesChanged { get; set; }
        public int TotalIssuesCreated { get; set; }
        public int TotalPullRequestsCreated { get; set; }
        public int TotalReviews { get; set; }
        public int UniqueDaysWithCommits { get; set; }
        public string? FilesModified { get; set; }
        public List<string> LanguagesContributed { get; set; } = new();
        public DateTime CalculatedAtUtc { get; set; }
    }
}
