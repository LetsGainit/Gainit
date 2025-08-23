using GainIt.API.Models.Projects;

namespace GainIt.API.Services.GitHub.Interfaces
{
    public interface IGitHubApiClient
    {
        /// <summary>
        /// Gets repository information using GraphQL
        /// </summary>
        Task<GitHubRepositoryNode?> GetRepositoryAsync(string owner, string name);

        /// <summary>
        /// Gets repository analytics data using GraphQL
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
        Task<object> GetRepositoryStatsAsync(string owner, string name);

        /// <summary>
        /// Validates if a repository exists and is public
        /// </summary>
        Task<bool> ValidateRepositoryAsync(string owner, string name);

        /// <summary>
        /// Gets the current rate limit status
        /// </summary>
        Task<(int Remaining, DateTime ResetAt)> GetRateLimitStatusAsync();

        /// <summary>
        /// Checks if we have enough rate limit quota
        /// </summary>
        Task<bool> HasRateLimitQuotaAsync(int requiredRequests = 1);
    }
}
