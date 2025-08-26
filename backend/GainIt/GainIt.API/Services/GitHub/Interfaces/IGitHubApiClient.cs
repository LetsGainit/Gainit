using GainIt.API.Models.Projects;

namespace GainIt.API.Services.GitHub.Interfaces
{
    public interface IGitHubApiClient
    {
        /// <summary>
        /// Gets repository information via REST API
        /// </summary>
        Task<GitHubRestApiRepository?> GetRepositoryAsync(string owner, string name);

        /// <summary>
        /// Gets repository analytics data using REST API
        /// </summary>
        Task<GitHubAnalyticsRepository?> GetRepositoryAnalyticsAsync(string owner, string name, int daysPeriod = 30);

        /// <summary>
        /// Gets user contribution data for a repository
        /// </summary>
        Task<List<GitHubAnalyticsCommitNode>> GetUserContributionsAsync(string owner, string name, string username, int daysPeriod = 30);

        /// <summary>
        /// Gets commit history for a repository
        /// </summary>
        Task<List<GitHubAnalyticsCommitNode>> GetCommitHistoryAsync(string owner, string name, int daysPeriod = 30);

        /// <summary>
        /// Gets issues for a repository
        /// </summary>
        Task<List<GitHubIssueNode>> GetIssuesAsync(string owner, string name, int daysPeriod = 30);

        /// <summary>
        /// Gets pull requests for a repository
        /// </summary>
        Task<List<GitHubPullRequestNode>> GetPullRequestsAsync(string owner, string name, int daysPeriod = 30);

        /// <summary>
        /// Gets repository statistics (stars, forks, etc.)
        /// </summary>
        Task<GitHubRepositoryStats> GetRepositoryStatsAsync(string owner, string name);

        /// <summary>
        /// Gets repository languages with byte counts
        /// </summary>
        Task<Dictionary<string, int>> GetRepositoryLanguagesAsync(string owner, string name);

        /// <summary>
        /// Gets repository contributors
        /// </summary>
        Task<List<GitHubRestApiContributor>> GetRepositoryContributorsAsync(string owner, string name);

        /// <summary>
        /// Gets repository branches
        /// </summary>
        Task<List<string>> GetRepositoryBranchesAsync(string owner, string name);

        /// <summary>
        /// Validates if a repository exists and is accessible via REST API
        /// </summary>
        Task<(bool IsValid, GitHubRestApiRepository? Repository, bool? IsPrivate)> ValidateRepositoryAsync(string owner, string name);

        /// <summary>
        /// Gets the current rate limit status
        /// </summary>
        Task<(int Remaining, DateTime ResetAt)> GetRateLimitStatusAsync();

        /// <summary>
        /// Checks if we have enough rate limit quota
        /// </summary>
        Task<bool> HasRateLimitQuotaAsync(int requiredRequests = 1);
    }

    /// <summary>
    /// Repository statistics model
    /// </summary>
    public class GitHubRepositoryStats
    {
        public int StargazerCount { get; set; }
        public int ForkCount { get; set; }
        public int IssueCount { get; set; }
        public int PullRequestCount { get; set; }
        public int BranchCount { get; set; }
        public int ReleaseCount { get; set; }
    }
}
