using GainIt.API.Models.Projects;

namespace GainIt.API.Services.GitHub.Interfaces
{
    public interface IGitHubService
    {
        /// <summary>
        /// Links a GitHub repository to a project
        /// </summary>
        Task<GitHubRepository> LinkRepositoryAsync(Guid projectId, string repositoryUrl);

        /// <summary>
        /// Unlinks a GitHub repository from a project
        /// </summary>
        Task<bool> UnlinkRepositoryAsync(Guid projectId);

        /// <summary>
        /// Gets the GitHub repository linked to a project
        /// </summary>
        Task<GitHubRepository?> GetRepositoryAsync(Guid projectId);

        /// <summary>
        /// Syncs repository data from GitHub
        /// </summary>
        Task<bool> SyncRepositoryDataAsync(Guid projectId);

        /// <summary>
        /// Gets project-level analytics for a repository
        /// </summary>
        Task<GitHubAnalytics?> GetProjectAnalyticsAsync(Guid projectId, int daysPeriod = 30);

        /// <summary>
        /// Gets user-level contribution analytics for a repository
        /// </summary>
        Task<List<GitHubContribution>> GetUserContributionsAsync(Guid projectId, int daysPeriod = 30);

        /// <summary>
        /// Gets contribution analytics for a specific user in a repository
        /// </summary>
        Task<GitHubContribution?> GetUserContributionAsync(Guid projectId, Guid userId, int daysPeriod = 30);

        /// <summary>
        /// Syncs all analytics data for a repository
        /// </summary>
        Task<bool> SyncAnalyticsAsync(Guid projectId, int daysPeriod = 30);

        /// <summary>
        /// Gets the sync status for a repository
        /// </summary>
        Task<GitHubSyncLog?> GetLastSyncStatusAsync(Guid projectId);

        /// <summary>
        /// Validates if a GitHub URL is accessible and public
        /// </summary>
        Task<bool> ValidateRepositoryUrlAsync(string repositoryUrl);

        /// <summary>
        /// Gets repository statistics for display
        /// </summary>
        Task<object> GetRepositoryStatsAsync(Guid projectId);

        /// <summary>
        /// Gets user activity summary for ChatGPT context
        /// </summary>
        Task<string> GetUserActivitySummaryAsync(Guid projectId, Guid userId, int daysPeriod = 30);

        /// <summary>
        /// Gets project activity summary for ChatGPT context
        /// </summary>
        Task<string> GetProjectActivitySummaryAsync(Guid projectId, int daysPeriod = 30);
    }
}
