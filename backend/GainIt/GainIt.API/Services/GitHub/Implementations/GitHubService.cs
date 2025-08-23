using System.Text.RegularExpressions;
using GainIt.API.Data;
using GainIt.API.Models.Projects;
using GainIt.API.Services.GitHub.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.GitHub.Implementations
{
    public class GitHubService : IGitHubService
    {
        private readonly GainItDbContext _context;
        private readonly IGitHubApiClient _apiClient;
        private readonly IGitHubAnalyticsService _analyticsService;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(
            GainItDbContext context,
            IGitHubApiClient apiClient,
            IGitHubAnalyticsService analyticsService,
            ILogger<GitHubService> logger)
        {
            _context = context;
            _apiClient = apiClient;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        public async Task<GitHubRepository> LinkRepositoryAsync(Guid projectId, string repositoryUrl)
        {
            _logger.LogInformation("Linking GitHub repository {RepositoryUrl} to project {ProjectId}", repositoryUrl, projectId);

            try
            {
                // Validate the repository URL
                if (!await ValidateRepositoryUrlAsync(repositoryUrl))
                {
                    throw new ArgumentException("Invalid or inaccessible GitHub repository URL");
                }

                // Parse the repository URL to extract owner and name
                var (owner, name) = ParseGitHubUrl(repositoryUrl);

                // Check if repository is already linked to this project
                var existingRepository = await _context.Set<GitHubRepository>()
                    .FirstOrDefaultAsync(r => r.ProjectId == projectId);

                if (existingRepository != null)
                {
                    throw new InvalidOperationException("Project already has a linked GitHub repository");
                }

                // Get repository data from GitHub API
                var repositoryData = await _apiClient.GetRepositoryAsync(owner, name);
                if (repositoryData == null)
                {
                    throw new InvalidOperationException("Failed to retrieve repository data from GitHub");
                }

                // Create new repository record
                var repository = new GitHubRepository
                {
                    ProjectId = projectId,
                    RepositoryName = name,
                    OwnerName = owner,
                    FullName = repositoryData.NameWithOwner,
                    RepositoryUrl = repositoryUrl,
                    Description = repositoryData.Description,
                    IsPublic = !repositoryData.IsPrivate,
                    IsArchived = repositoryData.IsArchived,
                    IsFork = repositoryData.IsFork,
                    DefaultBranch = repositoryData.DefaultBranchRef?.Name,
                    PrimaryLanguage = repositoryData.PrimaryLanguage?.Name,
                    StarsCount = repositoryData.StargazerCount,
                    ForksCount = repositoryData.ForkCount,
                    OpenIssuesCount = repositoryData.Issues?.TotalCount,
                    OpenPullRequestsCount = repositoryData.PullRequests?.TotalCount,
                    CreatedAtUtc = repositoryData.CreatedAt,
                    LastActivityAtUtc = repositoryData.PushedAt ?? repositoryData.UpdatedAt,
                    LastSyncedAtUtc = DateTime.UtcNow
                };

                // Add languages
                if (repositoryData.Languages?.Nodes != null)
                {
                    repository.Languages = repositoryData.Languages.Nodes.Select(l => l.Name).ToList();
                }

                // Add license info
                if (repositoryData.LicenseInfo != null)
                {
                    repository.License = repositoryData.LicenseInfo.Name;
                }

                // Save to database
                _context.Set<GitHubRepository>().Add(repository);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully linked GitHub repository {Repository} to project {ProjectId}", 
                    repository.FullName, projectId);

                return repository;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking GitHub repository {RepositoryUrl} to project {ProjectId}", 
                    repositoryUrl, projectId);
                throw;
            }
        }

        public async Task<bool> UnlinkRepositoryAsync(Guid projectId)
        {
            _logger.LogInformation("Unlinking GitHub repository from project {ProjectId}", projectId);

            try
            {
                var repository = await _context.Set<GitHubRepository>()
                    .FirstOrDefaultAsync(r => r.ProjectId == projectId);

                if (repository == null)
                {
                    _logger.LogWarning("No GitHub repository found for project {ProjectId}", projectId);
                    return false;
                }

                // Remove related data
                var analytics = await _context.Set<GitHubAnalytics>()
                    .Where(a => a.RepositoryId == repository.RepositoryId)
                    .ToListAsync();

                var contributions = await _context.Set<GitHubContribution>()
                    .Where(c => c.RepositoryId == repository.RepositoryId)
                    .ToListAsync();

                var syncLogs = await _context.Set<GitHubSyncLog>()
                    .Where(s => s.RepositoryId == repository.RepositoryId)
                    .ToListAsync();

                _context.Set<GitHubAnalytics>().RemoveRange(analytics);
                _context.Set<GitHubContribution>().RemoveRange(contributions);
                _context.Set<GitHubSyncLog>().RemoveRange(syncLogs);
                _context.Set<GitHubRepository>().Remove(repository);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully unlinked GitHub repository from project {ProjectId}", projectId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking GitHub repository from project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<GitHubRepository?> GetRepositoryAsync(Guid projectId)
        {
            return await _context.Set<GitHubRepository>()
                .Include(r => r.Analytics)
                .Include(r => r.Contributions)
                .FirstOrDefaultAsync(r => r.ProjectId == projectId);
        }

        public async Task<bool> SyncRepositoryDataAsync(Guid projectId)
        {
            _logger.LogInformation("Syncing repository data for project {ProjectId}", projectId);

            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    _logger.LogWarning("No GitHub repository found for project {ProjectId}", projectId);
                    return false;
                }

                // Create sync log entry
                var syncLog = new GitHubSyncLog
                {
                    RepositoryId = repository.RepositoryId,
                    SyncType = "Repository",
                    Status = "InProgress",
                    StartedAtUtc = DateTime.UtcNow
                };

                _context.Set<GitHubSyncLog>().Add(syncLog);
                await _context.SaveChangesAsync();

                try
                {
                    // Get updated repository data from GitHub
                    var repositoryData = await _apiClient.GetRepositoryAsync(repository.OwnerName, repository.RepositoryName);
                    if (repositoryData != null)
                    {
                        // Update repository information
                        repository.Description = repositoryData.Description;
                        repository.StarsCount = repositoryData.StargazerCount;
                        repository.ForksCount = repositoryData.ForkCount;
                        repository.OpenIssuesCount = repositoryData.Issues?.TotalCount;
                        repository.OpenPullRequestsCount = repositoryData.PullRequests?.TotalCount;
                        repository.LastActivityAtUtc = repositoryData.PushedAt ?? repositoryData.UpdatedAt;
                        repository.LastSyncedAtUtc = DateTime.UtcNow;

                        // Update languages
                        if (repositoryData.Languages?.Nodes != null)
                        {
                            repository.Languages = repositoryData.Languages.Nodes.Select(l => l.Name).ToList();
                        }

                        await _context.SaveChangesAsync();

                        // Update sync log
                        syncLog.Status = "Completed";
                        syncLog.CompletedAtUtc = DateTime.UtcNow;
                        syncLog.ItemsProcessed = 1;
                        syncLog.TotalItems = 1;

                        _logger.LogInformation("Successfully synced repository data for project {ProjectId}", projectId);
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to retrieve repository data from GitHub");
                    }
                }
                catch (Exception ex)
                {
                    syncLog.Status = "Failed";
                    syncLog.ErrorMessage = ex.Message;
                    syncLog.CompletedAtUtc = DateTime.UtcNow;
                    throw;
                }
                finally
                {
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing repository data for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<GitHubAnalytics?> GetProjectAnalyticsAsync(Guid projectId, int daysPeriod = 30)
        {
            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository?.Analytics == null)
                {
                    return null;
                }

                // Check if analytics are fresh enough
                var analyticsAge = DateTime.UtcNow - repository.Analytics.CalculatedAtUtc;
                if (analyticsAge.TotalDays > 1) // Refresh if older than 1 day
                {
                    await SyncAnalyticsAsync(projectId, daysPeriod);
                    repository = await GetRepositoryAsync(projectId);
                }

                return repository?.Analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project analytics for project {ProjectId}", projectId);
                return null;
            }
        }

        public async Task<List<GitHubContribution>> GetUserContributionsAsync(Guid projectId, int daysPeriod = 30)
        {
            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository?.Contributions == null)
                {
                    return new List<GitHubContribution>();
                }

                // Filter contributions by the specified period
                var cutoffDate = DateTime.UtcNow.AddDays(-daysPeriod);
                return repository.Contributions
                    .Where(c => c.CalculatedAtUtc >= cutoffDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contributions for project {ProjectId}", projectId);
                return new List<GitHubContribution>();
            }
        }

        public async Task<GitHubContribution?> GetUserContributionAsync(Guid projectId, Guid userId, int daysPeriod = 30)
        {
            try
            {
                var contributions = await GetUserContributionsAsync(projectId, daysPeriod);
                return contributions.FirstOrDefault(c => c.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contribution for user {UserId} in project {ProjectId}", userId, projectId);
                return null;
            }
        }

        public async Task<bool> SyncAnalyticsAsync(Guid projectId, int daysPeriod = 30)
        {
            _logger.LogInformation("Syncing analytics for project {ProjectId} over {DaysPeriod} days", projectId, daysPeriod);

            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    _logger.LogWarning("No GitHub repository found for project {ProjectId}", projectId);
                    return false;
                }

                // Create sync log entry
                var syncLog = new GitHubSyncLog
                {
                    RepositoryId = repository.RepositoryId,
                    SyncType = "Analytics",
                    Status = "InProgress",
                    StartedAtUtc = DateTime.UtcNow
                };

                _context.Set<GitHubSyncLog>().Add(syncLog);
                await _context.SaveChangesAsync();

                try
                {
                    // Get project members to sync their contributions
                    var projectMembers = await _context.Set<ProjectMember>()
                        .Where(pm => pm.ProjectId == projectId)
                        .Include(pm => pm.User)
                        .ToListAsync();

                    // Process repository analytics
                    var analytics = await _analyticsService.ProcessRepositoryAnalyticsAsync(repository, daysPeriod);
                    
                    // Remove old analytics and add new ones
                    if (repository.Analytics != null)
                    {
                        _context.Set<GitHubAnalytics>().Remove(repository.Analytics);
                    }
                    
                    repository.Analytics = analytics;
                    _context.Set<GitHubAnalytics>().Add(analytics);

                    // Process user contributions
                    var contributions = new List<GitHubContribution>();
                    foreach (var member in projectMembers)
                    {
                        // Use stored GitHubUsername if available, fallback to extracting from GitHubURL
                        string? username = member.User.GitHubUsername;
                        if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(member.User.GitHubURL))
                        {
                            username = ExtractGitHubUsername(member.User.GitHubURL);
                        }
                        
                        if (!string.IsNullOrEmpty(username))
                        {
                            var contribution = await _analyticsService.ProcessUserContributionAsync(
                                repository, member.UserId, username, daysPeriod);
                            contributions.Add(contribution);
                        }
                    }

                    // Remove old contributions and add new ones
                    if (repository.Contributions.Any())
                    {
                        _context.Set<GitHubContribution>().RemoveRange(repository.Contributions);
                    }
                    
                    repository.Contributions = contributions;
                    _context.Set<GitHubContribution>().AddRange(contributions);

                    await _context.SaveChangesAsync();

                    // Update sync log
                    syncLog.Status = "Completed";
                    syncLog.CompletedAtUtc = DateTime.UtcNow;
                    syncLog.ItemsProcessed = 1 + contributions.Count;
                    syncLog.TotalItems = 1 + contributions.Count;

                    _logger.LogInformation("Successfully synced analytics for project {ProjectId}", projectId);
                }
                catch (Exception ex)
                {
                    syncLog.Status = "Failed";
                    syncLog.ErrorMessage = ex.Message;
                    syncLog.CompletedAtUtc = DateTime.UtcNow;
                    throw;
                }
                finally
                {
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing analytics for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<GitHubSyncLog?> GetLastSyncStatusAsync(Guid projectId)
        {
            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    return null;
                }

                return await _context.Set<GitHubSyncLog>()
                    .Where(s => s.RepositoryId == repository.RepositoryId)
                    .OrderByDescending(s => s.StartedAtUtc)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync status for project {ProjectId}", projectId);
                return null;
            }
        }

        public async Task<bool> ValidateRepositoryUrlAsync(string repositoryUrl)
        {
            try
            {
                var (owner, name) = ParseGitHubUrl(repositoryUrl);
                return await _apiClient.ValidateRepositoryAsync(owner, name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate repository URL: {RepositoryUrl}", repositoryUrl);
                return false;
            }
        }

        public async Task<object> GetRepositoryStatsAsync(Guid projectId)
        {
            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    return new { Error = "Repository not found" };
                }

                var analytics = await GetProjectAnalyticsAsync(projectId);
                var contributions = await GetUserContributionsAsync(projectId);

                return new
                {
                    Repository = new
                    {
                        repository.RepositoryName,
                        repository.OwnerName,
                        repository.FullName,
                        repository.Description,
                        repository.IsPublic,
                        repository.StarsCount,
                        repository.ForksCount,
                        repository.PrimaryLanguage,
                        repository.Languages,
                        repository.LastActivityAtUtc,
                        repository.LastSyncedAtUtc
                    },
                    Analytics = analytics,
                    Contributors = contributions.Count,
                    TopContributors = contributions
                        .OrderByDescending(c => c.TotalCommits)
                        .Take(5)
                        .Select(c => new
                        {
                            c.GitHubUsername,
                            c.TotalCommits,
                            c.TotalLinesChanged,
                            c.UniqueDaysWithCommits
                        })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository stats for project {ProjectId}", projectId);
                return new { Error = "Failed to retrieve repository stats" };
            }
        }

        public async Task<string> GetUserActivitySummaryAsync(Guid projectId, Guid userId, int daysPeriod = 30)
        {
            try
            {
                var contribution = await GetUserContributionAsync(projectId, userId, daysPeriod);
                if (contribution == null)
                {
                    return "No contribution data available for this user.";
                }

                var summary = $"User Activity Summary for {contribution.GitHubUsername} in the last {daysPeriod} days:\n\n";
                summary += $"📝 Commit Activity:\n";
                summary += $"• Total Commits: {contribution.TotalCommits}\n";
                summary += $"• Lines Changed: {contribution.TotalLinesChanged} (+{contribution.TotalAdditions}, -{contribution.TotalDeletions})\n";
                summary += $"• Files Modified: {contribution.FilesModified}\n";
                summary += $"• Active Days: {contribution.UniqueDaysWithCommits}\n\n";

                summary += $"🔧 Issue & PR Activity:\n";
                summary += $"• Issues Created: {contribution.TotalIssuesCreated}\n";
                summary += $"• Pull Requests: {contribution.TotalPullRequestsCreated}\n";
                summary += $"• Code Reviews: {contribution.TotalReviews}\n\n";

                summary += $"📊 Activity Patterns:\n";
                if (contribution.CommitsByDayOfWeek.Any())
                {
                    var mostActiveDay = contribution.CommitsByDayOfWeek.OrderByDescending(x => x.Value).First();
                    summary += $"• Most Active Day: {mostActiveDay.Key} ({mostActiveDay.Value} commits)\n";
                }

                if (contribution.LanguagesContributed.Any())
                {
                    summary += $"• Languages: {string.Join(", ", contribution.LanguagesContributed)}\n";
                }

                var contributionScore = await _analyticsService.CalculateUserContributionScoreAsync(contribution);
                summary += $"\n🏆 Contribution Score: {contributionScore}/100\n";

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity summary for user {UserId} in project {ProjectId}", userId, projectId);
                return "Unable to generate user activity summary due to an error.";
            }
        }

        public async Task<string> GetProjectActivitySummaryAsync(Guid projectId, int daysPeriod = 30)
        {
            try
            {
                var analytics = await GetProjectAnalyticsAsync(projectId, daysPeriod);
                var contributions = await GetUserContributionsAsync(projectId, daysPeriod);

                if (analytics == null)
                {
                    return "No analytics data available for this project.";
                }

                return await _analyticsService.GenerateActivitySummaryAsync(analytics, contributions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project activity summary for project {ProjectId}", projectId);
                return "Unable to generate project activity summary due to an error.";
            }
        }

        #region Helper Methods

        private (string owner, string name) ParseGitHubUrl(string url)
        {
            // Support various GitHub URL formats
            var patterns = new[]
            {
                @"github\.com/([^/]+)/([^/]+?)(?:\.git)?/?$",
                @"github\.com/([^/]+)/([^/]+?)/?$"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(url, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return (match.Groups[1].Value, match.Groups[2].Value);
                }
            }

            throw new ArgumentException("Invalid GitHub repository URL format");
        }

        private string? ExtractGitHubUsername(string githubUrl)
        {
            try
            {
                var match = Regex.Match(githubUrl, @"github\.com/([^/]+)");
                return match.Success ? match.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
