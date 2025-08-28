using GainIt.API.Models.Projects;

namespace GainIt.API.Services.GitHub.Interfaces
{
    public interface IGitHubAnalyticsService
    {
        /// <summary>
        /// Processes raw GitHub data and creates analytics
        /// </summary>
        Task<GitHubAnalytics> ProcessRepositoryAnalyticsAsync(GitHubRepository repository, int daysPeriod = 30);

        /// <summary>
        /// Processes user contribution data and creates contribution analytics
        /// </summary>
        Task<GitHubContribution> ProcessUserContributionAsync(GitHubRepository repository, Guid userId, string username, int daysPeriod = 30);

        /// <summary>
        /// Aggregates analytics data from multiple time periods
        /// </summary>
        Task<List<GitHubAnalytics>> GetAggregatedAnalyticsAsync(Guid repositoryId, List<int> periods);

        /// <summary>
        /// Gets analytics trends over time
        /// </summary>
        Task<object> GetAnalyticsTrendsAsync(Guid repositoryId, int daysPeriod = 365);

        /// <summary>
        /// Calculates repository health score
        /// </summary>
        Task<double> CalculateRepositoryHealthScoreAsync(GitHubAnalytics analytics);

        /// <summary>
        /// Calculates user contribution score
        /// </summary>
        Task<double> CalculateUserContributionScoreAsync(GitHubContribution contribution);

        /// <summary>
        /// Generates activity summary for ChatGPT context
        /// </summary>
        Task<string> GenerateActivitySummaryAsync(GitHubAnalytics analytics, List<GitHubContribution> contributions);

        /// <summary>
        /// Cleans up old analytics data based on retention policy
        /// </summary>
        Task<int> CleanupOldAnalyticsAsync(int retentionDays = 365);

        /// <summary>
        /// Exports analytics data in various formats
        /// </summary>
        Task<byte[]> ExportAnalyticsAsync(Guid repositoryId, string format = "json");

        // Repo-to-project resolution lives in IGitHubService, not here
    }
}
