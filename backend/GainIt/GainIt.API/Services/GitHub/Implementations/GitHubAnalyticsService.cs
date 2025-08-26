using System.Text.Json;
using GainIt.API.Models.Projects;
using GainIt.API.Services.GitHub.Interfaces;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.GitHub.Implementations
{
    public class GitHubAnalyticsService : IGitHubAnalyticsService
    {
        private readonly ILogger<GitHubAnalyticsService> _logger;
        private readonly IGitHubApiClient _gitHubApiClient;

        public GitHubAnalyticsService(ILogger<GitHubAnalyticsService> logger, IGitHubApiClient gitHubApiClient)
        {
            _logger = logger;
            _gitHubApiClient = gitHubApiClient;
        }

        public async Task<GitHubAnalytics> ProcessRepositoryAnalyticsAsync(GitHubRepository repository, int daysPeriod = 30)
        {
            _logger.LogInformation("Processing repository analytics for {Repository} over {DaysPeriod} days", 
                repository.FullName, daysPeriod);

            try
            {
                var analytics = new GitHubAnalytics
                {
                    RepositoryId = repository.RepositoryId,
                    CalculatedAtUtc = DateTime.UtcNow,
                    DaysPeriod = daysPeriod
                };

                // Process basic repository stats
                analytics.TotalStars = repository.StarsCount ?? 0;
                analytics.TotalForks = repository.ForksCount ?? 0;
                analytics.TotalWatchers = 0; // Will be populated from API data
                analytics.TotalIssues = repository.OpenIssuesCount ?? 0;
                analytics.TotalPullRequests = repository.OpenPullRequestsCount ?? 0;
                analytics.TotalBranches = 0; // Will be populated from API data
                analytics.TotalReleases = 0; // Will be populated from API data
                analytics.TotalTags = 0; // Will be populated from API data

                // Set activity dates
                analytics.FirstCommitDate = repository.CreatedAtUtc;
                analytics.LastCommitDate = repository.LastActivityAtUtc;

                // Initialize language stats
                analytics.LanguageStats = repository.Languages.ToDictionary(lang => lang, lang => 0);

                // Initialize time-based activity tracking
                analytics.WeeklyCommits = InitializeWeeklyTracking();
                analytics.WeeklyIssues = InitializeWeeklyTracking();
                analytics.WeeklyPullRequests = InitializeWeeklyTracking();
                analytics.MonthlyCommits = InitializeMonthlyTracking();
                analytics.MonthlyIssues = InitializeMonthlyTracking();
                analytics.MonthlyPullRequests = InitializeMonthlyTracking();

                _logger.LogInformation("Repository analytics processed successfully for {Repository}", repository.FullName);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing repository analytics for {Repository}", repository.FullName);
                throw;
            }
        }

        public async Task<GitHubContribution> ProcessUserContributionAsync(GitHubRepository repository, Guid userId, string username, int daysPeriod = 30)
        {
            _logger.LogInformation("Processing user contribution analytics for {Username} in {Repository} over {DaysPeriod} days", 
                username, repository.FullName, daysPeriod);

            try
            {
                var contribution = new GitHubContribution
                {
                    RepositoryId = repository.RepositoryId,
                    UserId = userId,
                    GitHubUsername = username,
                    CalculatedAtUtc = DateTime.UtcNow,
                    DaysPeriod = daysPeriod
                };

                // Fetch real GitHub contribution data
                var gitHubContributions = await _gitHubApiClient.GetUserContributionsAsync(
                    repository.OwnerName, 
                    repository.RepositoryName, 
                    username, 
                    daysPeriod);

                if (gitHubContributions != null && gitHubContributions.Any())
                {
                    _logger.LogInformation("Found {ContributionCount} GitHub contributions for {Username} in {Repository}", 
                        gitHubContributions.Count, username, repository.FullName);

                    // Process commit data
                    contribution.TotalCommits = gitHubContributions.Count;
                    contribution.TotalAdditions = gitHubContributions.Sum(c => c.Additions);
                    contribution.TotalDeletions = gitHubContributions.Sum(c => c.Deletions);
                    contribution.TotalLinesChanged = contribution.TotalAdditions + contribution.TotalDeletions;
                    contribution.FilesModified = gitHubContributions.Sum(c => c.ChangedFiles);

                    // Calculate unique days with commits
                    var commitDates = gitHubContributions
                        .Select(c => c.CommittedDate.Date)
                        .Distinct()
                        .ToList();
                    contribution.UniqueDaysWithCommits = commitDates.Count;

                    // Process activity patterns
                    contribution.CommitsByDayOfWeek = ProcessCommitsByDayOfWeek(gitHubContributions);
                    contribution.CommitsByHour = ProcessCommitsByHour(gitHubContributions);
                    contribution.ActivityByMonth = ProcessActivityByMonth(gitHubContributions);

                    // Extract languages from commit messages (basic approach)
                    contribution.LanguagesContributed = ExtractLanguagesFromCommits(gitHubContributions);

                    // For now, set placeholder values for features not yet implemented
                    contribution.TotalIssuesCreated = 0; // Would need GitHub Issues API
                    contribution.TotalPullRequestsCreated = 0; // Would need GitHub PRs API
                    contribution.TotalReviews = 0; // Would need GitHub Reviews API
                    contribution.CollaboratorsInteractedWith = 0; // Would need more complex analysis
                }
                else
                {
                    _logger.LogInformation("No GitHub contributions found for {Username} in {Repository}", username, repository.FullName);
                    
                    // Initialize with zero values
                    contribution.TotalCommits = 0;
                    contribution.TotalAdditions = 0;
                    contribution.TotalDeletions = 0;
                    contribution.TotalLinesChanged = 0;
                    contribution.FilesModified = 0;
                    contribution.UniqueDaysWithCommits = 0;
                    contribution.TotalIssuesCreated = 0;
                    contribution.TotalPullRequestsCreated = 0;
                    contribution.TotalReviews = 0;
                    contribution.CollaboratorsInteractedWith = 0;

                    // Initialize empty activity patterns
                    contribution.CommitsByDayOfWeek = InitializeDayOfWeekTracking();
                    contribution.CommitsByHour = InitializeHourTracking();
                    contribution.ActivityByMonth = InitializeMonthlyTracking();
                    contribution.LanguagesContributed = new List<string>();
                }

                _logger.LogInformation("User contribution analytics processed successfully for {Username} in {Repository}: {Commits} commits, {LinesChanged} lines changed", 
                    username, repository.FullName, contribution.TotalCommits, contribution.TotalLinesChanged);
                return contribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user contribution analytics for {Username} in {Repository}", 
                    username, repository.FullName);
                throw;
            }
        }

        public async Task<List<GitHubAnalytics>> GetAggregatedAnalyticsAsync(Guid repositoryId, List<int> periods)
        {
            _logger.LogInformation("Getting aggregated analytics for repository {RepositoryId} with periods: {Periods}", 
                repositoryId, string.Join(", ", periods));

            // This would typically query the database for existing analytics
            // For now, return an empty list
            return new List<GitHubAnalytics>();
        }

        public async Task<object> GetAnalyticsTrendsAsync(Guid repositoryId, int daysPeriod = 365)
        {
            _logger.LogInformation("Getting analytics trends for repository {RepositoryId} over {DaysPeriod} days", 
                repositoryId, daysPeriod);

            // This would analyze trends over time
            // For now, return a placeholder object
            return new
            {
                RepositoryId = repositoryId,
                Period = daysPeriod,
                Trends = new object()
            };
        }

        public async Task<double> CalculateRepositoryHealthScoreAsync(GitHubAnalytics analytics)
        {
            try
            {
                var score = 0.0;
                var maxScore = 100.0;

                // Activity score (30 points)
                var activityScore = Math.Min(30.0, (analytics.TotalCommits / 100.0) * 30);
                score += activityScore;

                // Engagement score (25 points)
                var engagementScore = Math.Min(25.0, ((analytics.TotalStars + analytics.TotalForks) / 50.0) * 25);
                score += engagementScore;

                // Issue management score (20 points)
                var issueScore = 0.0;
                if (analytics.TotalIssues > 0)
                {
                    var closedRatio = (double)analytics.ClosedIssues / analytics.TotalIssues;
                    issueScore = closedRatio * 20;
                }
                score += issueScore;

                // PR management score (15 points)
                var prScore = 0.0;
                if (analytics.TotalPullRequests > 0)
                {
                    var mergedRatio = (double)analytics.MergedPullRequests / analytics.TotalPullRequests;
                    prScore = mergedRatio * 15;
                }
                score += prScore;

                // Recency score (10 points)
                var daysSinceLastActivity = (DateTime.UtcNow - analytics.LastCommitDate)?.TotalDays ?? 0;
                var recencyScore = Math.Max(0, 10 - (daysSinceLastActivity / 30.0) * 10);
                score += recencyScore;

                return Math.Round(score, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating repository health score");
                return 0.0;
            }
        }

        public async Task<double> CalculateUserContributionScoreAsync(GitHubContribution contribution)
        {
            try
            {
                var score = 0.0;
                var maxScore = 100.0;

                // Commit activity score (40 points)
                var commitScore = Math.Min(40.0, (contribution.TotalCommits / 50.0) * 40);
                score += commitScore;

                // Code quality score (25 points)
                var qualityScore = 0.0;
                if (contribution.TotalCommits > 0)
                {
                    var avgCommitSize = contribution.TotalLinesChanged / (double)contribution.TotalCommits;
                    // Prefer moderate commit sizes (not too small, not too large)
                    if (avgCommitSize >= 10 && avgCommitSize <= 200)
                    {
                        qualityScore = 25;
                    }
                    else if (avgCommitSize > 0)
                    {
                        qualityScore = Math.Max(0, 25 - Math.Abs(avgCommitSize - 100) / 10);
                    }
                }
                score += qualityScore;

                // Consistency score (20 points)
                var consistencyScore = Math.Min(20.0, (contribution.UniqueDaysWithCommits / 30.0) * 20);
                score += consistencyScore;

                // Collaboration score (15 points)
                var collaborationScore = Math.Min(15.0, (contribution.CollaboratorsInteractedWith / 10.0) * 15);
                score += collaborationScore;

                return Math.Round(score, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating user contribution score");
                return 0.0;
            }
        }

        public async Task<string> GenerateActivitySummaryAsync(GitHubAnalytics analytics, List<GitHubContribution> contributions)
        {
            try
            {
                var summary = $"Repository Activity Summary for the last {analytics.DaysPeriod} days:\n\n";

                // Repository overview
                summary += $"📊 Repository Overview:\n";
                summary += $"• Total Commits: {analytics.TotalCommits}\n";
                summary += $"• Total Issues: {analytics.TotalIssues} (Open: {analytics.OpenIssues}, Closed: {analytics.ClosedIssues})\n";
                summary += $"• Total Pull Requests: {analytics.TotalPullRequests} (Open: {analytics.OpenPullRequests}, Merged: {analytics.MergedPullRequests})\n";
                summary += $"• Stars: {analytics.TotalStars}, Forks: {analytics.TotalForks}\n";
                summary += $"• Active Contributors: {analytics.ActiveContributors}\n\n";

                // Activity patterns
                if (analytics.WeeklyCommits.Any())
                {
                    var mostActiveWeek = analytics.WeeklyCommits.OrderByDescending(x => x.Value).First();
                    summary += $"📈 Activity Patterns:\n";
                    summary += $"• Most Active Week: {mostActiveWeek.Key} ({mostActiveWeek.Value} commits)\n";
                }

                // User contributions
                if (contributions.Any())
                {
                    summary += $"\n👥 Top Contributors:\n";
                    var topContributors = contributions
                        .OrderByDescending(c => c.TotalCommits)
                        .Take(5);

                    foreach (var contributor in topContributors)
                    {
                        summary += $"• {contributor.GitHubUsername}: {contributor.TotalCommits} commits, {contributor.TotalLinesChanged} lines changed\n";
                    }
                }

                // Repository health
                var healthScore = await CalculateRepositoryHealthScoreAsync(analytics);
                summary += $"\n🏥 Repository Health Score: {healthScore}/100\n";

                if (healthScore >= 80)
                    summary += "Status: Excellent - Repository is very active and well-maintained\n";
                else if (healthScore >= 60)
                    summary += "Status: Good - Repository shows consistent activity\n";
                else if (healthScore >= 40)
                    summary += "Status: Fair - Repository has some activity but could improve\n";
                else
                    summary += "Status: Needs Attention - Repository shows low activity\n";

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating activity summary");
                return "Unable to generate activity summary due to an error.";
            }
        }

        public async Task<int> CleanupOldAnalyticsAsync(int retentionDays = 365)
        {
            _logger.LogInformation("Cleaning up analytics data older than {RetentionDays} days", retentionDays);

            // This would typically delete old analytics data from the database
            // For now, return 0 as no cleanup was performed
            return 0;
        }

        public async Task<byte[]> ExportAnalyticsAsync(Guid repositoryId, string format = "json")
        {
            _logger.LogInformation("Exporting analytics for repository {RepositoryId} in {Format} format", repositoryId, format);

            try
            {
                // This would typically query the database and format the data
                // For now, return a placeholder JSON
                var placeholderData = new
                {
                    RepositoryId = repositoryId,
                    ExportDate = DateTime.UtcNow,
                    Format = format,
                    Message = "Analytics export not yet implemented"
                };

                var json = JsonSerializer.Serialize(placeholderData);
                return System.Text.Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics for repository {RepositoryId}", repositoryId);
                throw;
            }
        }

        public async Task<Guid?> GetProjectIdForRepositoryAsync(string owner, string name)
        {
            _logger.LogInformation("Getting project ID for repository {Owner}/{Name}", owner, name);
            
            try
            {
                // This would typically query the database to find the project ID
                // For now, return null as this method is not yet fully implemented
                _logger.LogWarning("GetProjectIdForRepositoryAsync not yet implemented for {Owner}/{Name}", owner, name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project ID for repository {Owner}/{Name}", owner, name);
                return null;
            }
        }

        #region Helper Methods

        private Dictionary<string, int> InitializeWeeklyTracking()
        {
            var tracking = new Dictionary<string, int>();
            var startDate = DateTime.UtcNow.AddDays(-52 * 7); // Last 52 weeks

            for (int i = 0; i < 52; i++)
            {
                var weekStart = startDate.AddDays(i * 7);
                var weekKey = weekStart.ToString("yyyy-MM-dd");
                tracking[weekKey] = 0;
            }

            return tracking;
        }

        private Dictionary<string, int> InitializeMonthlyTracking()
        {
            var tracking = new Dictionary<string, int>();
            var startDate = DateTime.UtcNow.AddMonths(-12); // Last 12 months

            for (int i = 0; i < 12; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthKey = monthStart.ToString("yyyy-MM");
                tracking[monthKey] = 0;
            }

            return tracking;
        }

        private Dictionary<string, int> InitializeDayOfWeekTracking()
        {
            return new Dictionary<string, int>
            {
                ["Monday"] = 0,
                ["Tuesday"] = 0,
                ["Wednesday"] = 0,
                ["Thursday"] = 0,
                ["Friday"] = 0,
                ["Saturday"] = 0,
                ["Sunday"] = 0
            };
        }

        private Dictionary<string, int> InitializeHourTracking()
        {
            var tracking = new Dictionary<string, int>();
            for (int hour = 0; hour < 24; hour++)
            {
                tracking[hour.ToString("00")] = 0;
            }
            return tracking;
        }

        private Dictionary<string, int> ProcessCommitsByDayOfWeek(List<GitHubAnalyticsCommitNode> commits)
        {
            var dayOfWeekCounts = InitializeDayOfWeekTracking();
            
            foreach (var commit in commits)
            {
                var dayOfWeek = commit.CommittedDate.DayOfWeek.ToString();
                if (dayOfWeekCounts.ContainsKey(dayOfWeek))
                {
                    dayOfWeekCounts[dayOfWeek]++;
                }
            }
            
            return dayOfWeekCounts;
        }

        private Dictionary<string, int> ProcessCommitsByHour(List<GitHubAnalyticsCommitNode> commits)
        {
            var hourCounts = InitializeHourTracking();
            
            foreach (var commit in commits)
            {
                var hour = commit.CommittedDate.Hour.ToString("00");
                if (hourCounts.ContainsKey(hour))
                {
                    hourCounts[hour]++;
                }
            }
            
            return hourCounts;
        }

        private Dictionary<string, int> ProcessActivityByMonth(List<GitHubAnalyticsCommitNode> commits)
        {
            var monthCounts = InitializeMonthlyTracking();
            
            foreach (var commit in commits)
            {
                var month = commit.CommittedDate.ToString("yyyy-MM");
                if (monthCounts.ContainsKey(month))
                {
                    monthCounts[month]++;
                }
            }
            
            return monthCounts;
        }

        private List<string> ExtractLanguagesFromCommits(List<GitHubAnalyticsCommitNode> commits)
        {
            var languages = new HashSet<string>();
            
            // Basic language detection from commit messages
            foreach (var commit in commits)
            {
                var message = commit.Message.ToLower();
                
                // Common programming language keywords
                if (message.Contains("javascript") || message.Contains("js")) languages.Add("JavaScript");
                if (message.Contains("typescript") || message.Contains("ts")) languages.Add("TypeScript");
                if (message.Contains("python") || message.Contains("py")) languages.Add("Python");
                if (message.Contains("c#") || message.Contains("csharp")) languages.Add("C#");
                if (message.Contains("java")) languages.Add("Java");
                if (message.Contains("php")) languages.Add("PHP");
                if (message.Contains("ruby")) languages.Add("Ruby");
                if (message.Contains("go")) languages.Add("Go");
                if (message.Contains("rust")) languages.Add("Rust");
                if (message.Contains("swift")) languages.Add("Swift");
                if (message.Contains("kotlin")) languages.Add("Kotlin");
                if (message.Contains("scala")) languages.Add("Scala");
                if (message.Contains("r")) languages.Add("R");
                if (message.Contains("sql")) languages.Add("SQL");
                if (message.Contains("html")) languages.Add("HTML");
                if (message.Contains("css")) languages.Add("CSS");
            }
            
            return languages.ToList();
        }

        #endregion
    }
}
