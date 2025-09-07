using GainIt.API.DTOs.ViewModels.GitHub.Base;

namespace GainIt.API.DTOs.ViewModels.GitHub
{
    /// <summary>
    /// Comprehensive GitHub project overview response DTO
    /// </summary>
    public class GitHubProjectOverviewResponseDto : GitHubBaseResponseDto
    {
        public GitHubRepositoryOverviewDto Repository { get; set; } = new();
        public GitHubRepositoryStatsOverviewDto? Stats { get; set; }
        public GitHubAnalyticsOverviewDto? Analytics { get; set; }
        public List<GitHubContributionOverviewDto> Contributions { get; set; } = new();
        public string ActivitySummary { get; set; } = string.Empty;
        public GitHubSyncStatusOverviewDto? SyncStatus { get; set; }
    }

    /// <summary>
    /// Repository overview information
    /// </summary>
    public class GitHubRepositoryOverviewDto
    {
        public Guid RepositoryId { get; set; }
        public string RepositoryName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public string? PrimaryLanguage { get; set; }
        public List<string> Languages { get; set; } = new();
        public int StarsCount { get; set; }
        public int ForksCount { get; set; }
        public int OpenIssuesCount { get; set; }
        public int OpenPullRequestsCount { get; set; }
        public string? DefaultBranch { get; set; }
        public DateTime LastActivityAtUtc { get; set; }
        public DateTime LastSyncedAtUtc { get; set; }
        public List<string> Branches { get; set; } = new();
    }

    /// <summary>
    /// Repository statistics overview
    /// </summary>
    public class GitHubRepositoryStatsOverviewDto
    {
        public int StarsCount { get; set; }
        public int ForksCount { get; set; }
        public int IssueCount { get; set; }
        public int PullRequestCount { get; set; }
        public int BranchCount { get; set; }
        public int ReleaseCount { get; set; }
        public int Contributors { get; set; }
        public List<TopContributorOverviewDto> TopContributors { get; set; } = new();
    }

    /// <summary>
    /// Top contributor overview
    /// </summary>
    public class TopContributorOverviewDto
    {
        public string GitHubUsername { get; set; } = string.Empty;
        public int TotalCommits { get; set; }
        public int TotalLinesChanged { get; set; }
        public int UniqueDaysWithCommits { get; set; }
    }

    /// <summary>
    /// Analytics overview
    /// </summary>
    public class GitHubAnalyticsOverviewDto
    {
        public DateTime CalculatedAt { get; set; }
        public int TotalCommits { get; set; }
        public int TotalAdditions { get; set; }
        public int TotalDeletions { get; set; }
        public int TotalLinesChanged { get; set; }
        public int TotalIssues { get; set; }
        public int OpenIssues { get; set; }
        public int ClosedIssues { get; set; }
        public int TotalPullRequests { get; set; }
        public int OpenPullRequests { get; set; }
        public int MergedPullRequests { get; set; }
        public int ClosedPullRequests { get; set; }
        public int ActiveContributors { get; set; }
        public int TotalContributors { get; set; }
        public DateTime? FirstCommitDate { get; set; }
        public DateTime? LastCommitDate { get; set; }
        public int TotalStars { get; set; }
        public int TotalForks { get; set; }
        public Dictionary<string, int> LanguageStats { get; set; } = new();
        public Dictionary<string, int> WeeklyCommits { get; set; } = new();
        public Dictionary<string, int> MonthlyCommits { get; set; } = new();
    }

    /// <summary>
    /// User contribution overview
    /// </summary>
    public class GitHubContributionOverviewDto
    {
        public Guid UserId { get; set; }
        public string? GitHubUsername { get; set; }
        public int TotalCommits { get; set; }
        public int TotalLinesChanged { get; set; }
        public int TotalIssuesCreated { get; set; }
        public int TotalPullRequestsCreated { get; set; }
        public int TotalReviews { get; set; }
        public int UniqueDaysWithCommits { get; set; }
        public int FilesModified { get; set; }
        public List<string> LanguagesContributed { get; set; } = new();
        public int LongestStreak { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime CalculatedAtUtc { get; set; }
    }

    /// <summary>
    /// Sync status overview
    /// </summary>
    public class GitHubSyncStatusOverviewDto
    {
        public string SyncType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public int ItemsProcessed { get; set; }
        public int TotalItems { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
