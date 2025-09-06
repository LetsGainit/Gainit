using System.Text.RegularExpressions;
using GainIt.API.Data;
using GainIt.API.Models.Projects;
using GainIt.API.Services.GitHub.Interfaces;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GainIt.API.DTOs.ViewModels.GitHub;

namespace GainIt.API.Services.GitHub.Implementations
{
    public class GitHubService : IGitHubService
    {
        private readonly GainItDbContext _context;
        private readonly IGitHubApiClient _apiClient;
        private readonly IGitHubAnalyticsService _analyticsService;
        private readonly IProjectMatchingService _projectMatchingService;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(
            GainItDbContext context,
            IGitHubApiClient apiClient,
            IGitHubAnalyticsService analyticsService,
            IProjectMatchingService projectMatchingService,
            ILogger<GitHubService> logger)
        {
            _context = context;
            _apiClient = apiClient;
            _analyticsService = analyticsService;
            _projectMatchingService = projectMatchingService;
            _logger = logger;
        }

        public async Task<GitHubRepositoryLinkResponseDto> LinkRepositoryAsync(Guid projectId, string repositoryUrl)
        {
            try
        {
            _logger.LogInformation("Linking GitHub repository {RepositoryUrl} to project {ProjectId}", repositoryUrl, projectId);

                // Extract owner and name from URL
                var (owner, name) = ParseGitHubUrl(repositoryUrl);
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(name))
                {
                    return new GitHubRepositoryLinkResponseDto
                    {
                        Success = false,
                        Message = "Invalid GitHub repository URL format"
                    };
                }

                // Validate repository exists and is public
                var (isValid, repositoryData, isPrivate) = await _apiClient.ValidateRepositoryAsync(owner, name);
                
                if (!isValid || repositoryData == null)
                {
                    return new GitHubRepositoryLinkResponseDto
                    {
                        Success = false,
                        Message = "Invalid or inaccessible GitHub repository URL"
                    };
                }

                if (isPrivate == true)
                {
                    return new GitHubRepositoryLinkResponseDto
                    {
                        Success = false,
                        Message = "Private repositories are not supported"
                    };
                }

                // Check if this repository is already linked to another project
                var existingRepository = await _context.Set<GitHubRepository>()
                    .FirstOrDefaultAsync(r => r.OwnerName == owner && r.RepositoryName == name);

                if (existingRepository != null && existingRepository.ProjectId != projectId)
                {
                    return new GitHubRepositoryLinkResponseDto
                    {
                        Success = false,
                        Message = $"Repository {owner}/{name} is already linked to another project"
                    };
                }

                // Get additional repository data
                var languages = await _apiClient.GetRepositoryLanguagesAsync(owner, name);
                var contributors = await _apiClient.GetRepositoryContributorsAsync(owner, name);
                var branches = await _apiClient.GetRepositoryBranchesAsync(owner, name);

                // Check if this project already has a repository linked
                var projectRepository = await _context.Set<GitHubRepository>()
                    .FirstOrDefaultAsync(r => r.ProjectId == projectId);

                GitHubRepository repository;

                if (projectRepository != null)
                {
                    // Update existing repository for this project
                    projectRepository.RepositoryName = name;
                    projectRepository.OwnerName = owner;
                    projectRepository.FullName = repositoryData.FullName;
                    projectRepository.RepositoryUrl = repositoryData.HtmlUrl;
                    projectRepository.Description = repositoryData.Description;
                    projectRepository.IsPublic = !repositoryData.Private;
                    projectRepository.LastSyncedAtUtc = DateTime.UtcNow;
                    projectRepository.LastActivityAtUtc = repositoryData.PushedAt ?? repositoryData.UpdatedAt;
                    projectRepository.StarsCount = repositoryData.StargazersCount;
                    projectRepository.ForksCount = repositoryData.ForksCount;
                    projectRepository.OpenIssuesCount = repositoryData.OpenIssuesCount;
                    projectRepository.OpenPullRequestsCount = 0; // Will be fetched separately if needed
                    projectRepository.DefaultBranch = repositoryData.DefaultBranch;
                    projectRepository.PrimaryLanguage = repositoryData.Language;
                    projectRepository.Languages = languages.Keys.ToList();
                    projectRepository.License = repositoryData.License?.Name;
                    projectRepository.IsArchived = repositoryData.Archived;
                    projectRepository.IsFork = repositoryData.Fork;
                    
                    // Store all branches
                    projectRepository.Branches = branches ?? new List<string>();

                    repository = projectRepository;
                }
                else
                {
                    // Create new repository for this project
                    repository = new GitHubRepository
                {
                    ProjectId = projectId,
                    RepositoryName = name,
                    OwnerName = owner,
                        FullName = repositoryData.FullName,
                        RepositoryUrl = repositoryData.HtmlUrl,
                    Description = repositoryData.Description,
                        IsPublic = !repositoryData.Private,
                    CreatedAtUtc = repositoryData.CreatedAt,
                        LastSyncedAtUtc = DateTime.UtcNow,
                    LastActivityAtUtc = repositoryData.PushedAt ?? repositoryData.UpdatedAt,
                        StarsCount = repositoryData.StargazersCount,
                        ForksCount = repositoryData.ForksCount,
                        OpenIssuesCount = repositoryData.OpenIssuesCount,
                        OpenPullRequestsCount = 0, // Will be fetched separately if needed
                        DefaultBranch = repositoryData.DefaultBranch,
                        PrimaryLanguage = repositoryData.Language,
                        Languages = languages.Keys.ToList(),
                        License = repositoryData.License?.Name,
                        IsArchived = repositoryData.Archived,
                        IsFork = repositoryData.Fork,
                        Branches = branches ?? new List<string>()
                    };

                    _context.Set<GitHubRepository>().Add(repository);
                }

                await _context.SaveChangesAsync();

                return new GitHubRepositoryLinkResponseDto
                {
                    Success = true,
                    Message = $"Successfully linked repository {repositoryData.FullName}",
                    RepositoryId = repository.RepositoryId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking GitHub repository {RepositoryUrl} to project {ProjectId}", repositoryUrl, projectId);
                return new GitHubRepositoryLinkResponseDto
                {
                    Success = false,
                    Message = "An error occurred while linking the repository"
                };
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
                        // Get additional repository data
                        var languages = await _apiClient.GetRepositoryLanguagesAsync(repository.OwnerName, repository.RepositoryName);
                        var contributors = await _apiClient.GetRepositoryContributorsAsync(repository.OwnerName, repository.RepositoryName);
                        var branches = await _apiClient.GetRepositoryBranchesAsync(repository.OwnerName, repository.RepositoryName);
                        // Fetch PRs and count open ones to persist in DB
                        var prs = await _apiClient.GetPullRequestsAsync(repository.OwnerName, repository.RepositoryName, 30);
                        var openPrCount = prs?.Count(pr => string.Equals(pr.State, "open", StringComparison.OrdinalIgnoreCase)) ?? 0;

                        // Update repository information
                        repository.Description = repositoryData.Description;
                        repository.StarsCount = repositoryData.StargazersCount;
                        repository.ForksCount = repositoryData.ForksCount;
                        repository.OpenIssuesCount = repositoryData.OpenIssuesCount;
                        repository.OpenPullRequestsCount = openPrCount;
                        repository.LastActivityAtUtc = repositoryData.PushedAt ?? repositoryData.UpdatedAt;

                        // Update languages from REST API
                        if (languages != null && languages.Any())
                        {
                            repository.Languages = languages.Keys.ToList();
                        }

                        // Update branches from REST API
                        if (branches != null && branches.Any())
                        {
                            repository.Branches = branches;
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

        public async Task<GitHubAnalytics?> GetProjectAnalyticsAsync(Guid projectId, int daysPeriod = 30, bool force = false)
        {
            try
            {
                _logger.LogDebug("GetProjectAnalyticsAsync called for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
                
                var repository = await GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    _logger.LogWarning("Repository not found for project {ProjectId}", projectId);
                    return null;
                }

                _logger.LogDebug("Repository found for project {ProjectId}: {RepositoryName}", projectId, repository.RepositoryName);
                
                // Check if analytics exist and are fresh
                var analytics = await _context.Set<GitHubAnalytics>()
                    .FirstOrDefaultAsync(a => a.RepositoryId == repository.RepositoryId);

                if (analytics == null)
                {
                    _logger.LogInformation("No analytics record found for repository {RepositoryId} in project {ProjectId}, creating analytics automatically", repository.RepositoryId, projectId);
                    
                    // Create sync log entry for analytics creation
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
                        // Create analytics record automatically
                        analytics = await _analyticsService.ProcessRepositoryAnalyticsAsync(repository, daysPeriod);
                        if (analytics != null)
                        {
                            _context.Set<GitHubAnalytics>().Add(analytics);
                            await _context.SaveChangesAsync();
                            
                            // Update sync log
                            syncLog.Status = "Completed";
                            syncLog.CompletedAtUtc = DateTime.UtcNow;
                            syncLog.ItemsProcessed = 1;
                            syncLog.TotalItems = 1;
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation("Created analytics record for repository {RepositoryId} in project {ProjectId}", repository.RepositoryId, projectId);
                        }
                        else
                        {
                            syncLog.Status = "Failed";
                            syncLog.ErrorMessage = "Failed to process repository analytics";
                            syncLog.CompletedAtUtc = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        syncLog.Status = "Failed";
                        syncLog.ErrorMessage = ex.Message;
                        syncLog.CompletedAtUtc = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        throw;
                    }
                }
                else
                {
                    _logger.LogDebug("Analytics record found for repository {RepositoryId}: CalculatedAt={CalculatedAt}", repository.RepositoryId, analytics.CalculatedAtUtc);

                    // Check if analytics are fresh (less than 1 day old)
                    var isFresh = analytics.CalculatedAtUtc >= DateTime.UtcNow.AddDays(-1);
                    _logger.LogDebug("Analytics freshness check: IsFresh={IsFresh}, CalculatedAt={CalculatedAt}, Now={Now}", 
                        isFresh, analytics.CalculatedAtUtc, DateTime.UtcNow);

                    // Recompute if stale, or if caller requested different period, or force=true
                    if (!isFresh || analytics.DaysPeriod != daysPeriod || force)
                    {
                        _logger.LogInformation("Refreshing analytics for project {ProjectId}. Reason => Stale: {Stale}, PeriodChanged: {PeriodChanged}, Force: {Force}", 
                            projectId, !isFresh, analytics.DaysPeriod != daysPeriod, force);
                        
                        // Create sync log entry for analytics refresh
                        var refreshSyncLog = new GitHubSyncLog
                        {
                            RepositoryId = repository.RepositoryId,
                            SyncType = "AnalyticsRefresh",
                            Status = "InProgress",
                            StartedAtUtc = DateTime.UtcNow
                        };
                        _context.Set<GitHubSyncLog>().Add(refreshSyncLog);
                        await _context.SaveChangesAsync();

                        try
                        {
                            // Refresh analytics automatically
                            var refreshedAnalytics = await _analyticsService.ProcessRepositoryAnalyticsAsync(repository, daysPeriod);
                            if (refreshedAnalytics != null)
                            {
                                // Update existing analytics with full field set
                                analytics.DaysPeriod = refreshedAnalytics.DaysPeriod;
                                analytics.TotalCommits = refreshedAnalytics.TotalCommits;
                                analytics.TotalAdditions = refreshedAnalytics.TotalAdditions;
                                analytics.TotalDeletions = refreshedAnalytics.TotalDeletions;
                                analytics.TotalLinesChanged = refreshedAnalytics.TotalLinesChanged;
                                analytics.TotalIssues = refreshedAnalytics.TotalIssues;
                                analytics.OpenIssues = refreshedAnalytics.OpenIssues;
                                analytics.ClosedIssues = refreshedAnalytics.ClosedIssues;
                                analytics.TotalPullRequests = refreshedAnalytics.TotalPullRequests;
                                analytics.OpenPullRequests = refreshedAnalytics.OpenPullRequests;
                                analytics.MergedPullRequests = refreshedAnalytics.MergedPullRequests;
                                analytics.ClosedPullRequests = refreshedAnalytics.ClosedPullRequests;
                                analytics.TotalBranches = refreshedAnalytics.TotalBranches;
                                analytics.TotalReleases = refreshedAnalytics.TotalReleases;
                                analytics.TotalTags = refreshedAnalytics.TotalTags;
                                analytics.AverageTimeToCloseIssues = refreshedAnalytics.AverageTimeToCloseIssues;
                                analytics.AverageTimeToMergePRs = refreshedAnalytics.AverageTimeToMergePRs;
                                analytics.ActiveContributors = refreshedAnalytics.ActiveContributors;
                                analytics.TotalContributors = refreshedAnalytics.TotalContributors;
                                analytics.FirstCommitDate = refreshedAnalytics.FirstCommitDate;
                                analytics.LastCommitDate = refreshedAnalytics.LastCommitDate;
                                analytics.TotalStars = refreshedAnalytics.TotalStars;
                                analytics.TotalForks = refreshedAnalytics.TotalForks;
                                analytics.TotalWatchers = refreshedAnalytics.TotalWatchers;
                                analytics.LanguageStats = refreshedAnalytics.LanguageStats;
                                analytics.WeeklyCommits = refreshedAnalytics.WeeklyCommits;
                                analytics.WeeklyIssues = refreshedAnalytics.WeeklyIssues;
                                analytics.WeeklyPullRequests = refreshedAnalytics.WeeklyPullRequests;
                                analytics.MonthlyCommits = refreshedAnalytics.MonthlyCommits;
                                analytics.MonthlyIssues = refreshedAnalytics.MonthlyIssues;
                                analytics.MonthlyPullRequests = refreshedAnalytics.MonthlyPullRequests;
                                analytics.CalculatedAtUtc = DateTime.UtcNow;
                                
                                await _context.SaveChangesAsync();
                                
                                // Update sync log
                                refreshSyncLog.Status = "Completed";
                                refreshSyncLog.CompletedAtUtc = DateTime.UtcNow;
                                refreshSyncLog.ItemsProcessed = 1;
                                refreshSyncLog.TotalItems = 1;
                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation("Refreshed analytics for repository {RepositoryId} in project {ProjectId}", repository.RepositoryId, projectId);
                            }
                            else
                            {
                                refreshSyncLog.Status = "Failed";
                                refreshSyncLog.ErrorMessage = "Failed to refresh repository analytics";
                                refreshSyncLog.CompletedAtUtc = DateTime.UtcNow;
                                await _context.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            refreshSyncLog.Status = "Failed";
                            refreshSyncLog.ErrorMessage = ex.Message;
                            refreshSyncLog.CompletedAtUtc = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            throw;
                        }
                    }
                }

                // Do not recompute contributions here unless forced
                if (force)
                {
                    _logger.LogDebug("Force=true: updating user contributions for project {ProjectId}", projectId);
                    await EnsureContributionsUpToDateAsync(projectId, daysPeriod, force: true);
                }

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project analytics for project {ProjectId}", projectId);
                return null;
            }
        }

        /// <summary>
        /// Ensures user contributions are up to date for a project
        /// </summary>
        private async Task EnsureContributionsUpToDateAsync(Guid projectId, int daysPeriod, bool force = false)
        {
            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository == null) return;

                // Freshness gating: skip if recent unless forced
                var now = DateTime.UtcNow;
                var cutoff = now.AddDays(-1); // 1 day freshness window
                var minIntervalCutoff = now.AddHours(-1); // minimum 1 hour between updates

                var latestContribution = await _context.Set<GitHubContribution>()
                    .Where(c => c.RepositoryId == repository.RepositoryId)
                    .OrderByDescending(c => c.CalculatedAtUtc)
                    .FirstOrDefaultAsync();

                if (!force && latestContribution != null)
                {
                    if (latestContribution.CalculatedAtUtc >= cutoff)
                    {
                        _logger.LogDebug("Skipping contributions update for project {ProjectId}: data is fresh (CalculatedAtUtc={CalculatedAtUtc})", projectId, latestContribution.CalculatedAtUtc);
                        return;
                    }

                    if (latestContribution.CalculatedAtUtc >= minIntervalCutoff)
                    {
                        _logger.LogDebug("Skipping contributions update for project {ProjectId}: within min interval (CalculatedAtUtc={CalculatedAtUtc})", projectId, latestContribution.CalculatedAtUtc);
                        return;
                    }
                }

                // Create sync log entry for contributions update
                var contributionsSyncLog = new GitHubSyncLog
                {
                    RepositoryId = repository.RepositoryId,
                    SyncType = "Contributions",
                    Status = "InProgress",
                    StartedAtUtc = DateTime.UtcNow
                };
                _context.Set<GitHubSyncLog>().Add(contributionsSyncLog);
                await _context.SaveChangesAsync();

                try
                {
                    // Get project members to process contributions
                    var projectMembers = await _context.Set<ProjectMember>()
                        .Where(pm => pm.ProjectId == projectId && pm.LeftAtUtc == null)
                        .Include(pm => pm.User)
                        .ToListAsync();

                    _logger.LogDebug("Found {MemberCount} active project members for project {ProjectId}", projectMembers.Count, projectId);

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
                            _logger.LogDebug("Processing contribution for user {UserId} with GitHub username {GitHubUsername}", member.UserId, username);
                            var contribution = await _analyticsService.ProcessUserContributionAsync(
                                repository, member.UserId, username, daysPeriod);
                            contributions.Add(contribution);
                        }
                        else
                        {
                            _logger.LogDebug("Skipping user {UserId} - no GitHub username found", member.UserId);
                        }
                    }

                    _logger.LogDebug("Processed {ContributionCount} user contributions for project {ProjectId}", contributions.Count, projectId);

                    // In-place upsert: do not overwrite with empty results and avoid re-inserting existing PKs
                    var existingByUser = repository.Contributions.ToDictionary(c => c.UserId, c => c);
                    foreach (var incoming in contributions)
                    {
                        bool hasMeaningfulData = incoming.TotalCommits > 0 || incoming.TotalIssuesCreated > 0 || incoming.TotalPullRequestsCreated > 0 || incoming.TotalReviews > 0;

                        if (existingByUser.TryGetValue(incoming.UserId, out var existing))
                        {
                            if (hasMeaningfulData)
                            {
                                // Copy fields onto the existing tracked entity
                                existing.GitHubUsername = incoming.GitHubUsername;
                                existing.CalculatedAtUtc = incoming.CalculatedAtUtc;
                                existing.DaysPeriod = incoming.DaysPeriod;
                                existing.TotalCommits = incoming.TotalCommits;
                                existing.TotalAdditions = incoming.TotalAdditions;
                                existing.TotalDeletions = incoming.TotalDeletions;
                                existing.TotalLinesChanged = incoming.TotalLinesChanged;
                                existing.UniqueDaysWithCommits = incoming.UniqueDaysWithCommits;
                                existing.TotalIssuesCreated = incoming.TotalIssuesCreated;
                                existing.OpenIssuesCreated = incoming.OpenIssuesCreated;
                                existing.ClosedIssuesCreated = incoming.ClosedIssuesCreated;
                                existing.IssuesCommentedOn = incoming.IssuesCommentedOn;
                                existing.IssuesAssigned = incoming.IssuesAssigned;
                                existing.IssuesClosed = incoming.IssuesClosed;
                                existing.TotalPullRequestsCreated = incoming.TotalPullRequestsCreated;
                                existing.OpenPullRequestsCreated = incoming.OpenPullRequestsCreated;
                                existing.MergedPullRequestsCreated = incoming.MergedPullRequestsCreated;
                                existing.ClosedPullRequestsCreated = incoming.ClosedPullRequestsCreated;
                                existing.PullRequestsReviewed = incoming.PullRequestsReviewed;
                                existing.PullRequestsApproved = incoming.PullRequestsApproved;
                                existing.PullRequestsRequestedChanges = incoming.PullRequestsRequestedChanges;
                                existing.TotalReviews = incoming.TotalReviews;
                                existing.ReviewsApproved = incoming.ReviewsApproved;
                                existing.ReviewsRequestedChanges = incoming.ReviewsRequestedChanges;
                                existing.ReviewsCommented = incoming.ReviewsCommented;
                                existing.CommitsByDayOfWeek = incoming.CommitsByDayOfWeek;
                                existing.CommitsByHour = incoming.CommitsByHour;
                                existing.ActivityByMonth = incoming.ActivityByMonth;
                                existing.FilesModified = incoming.FilesModified;
                                existing.LanguagesContributed = incoming.LanguagesContributed;
                                existing.LongestStreak = incoming.LongestStreak;
                                existing.CurrentStreak = incoming.CurrentStreak;
                                existing.CollaboratorsInteractedWith = incoming.CollaboratorsInteractedWith;
                                existing.DiscussionsParticipated = incoming.DiscussionsParticipated;
                                existing.WikiPagesEdited = incoming.WikiPagesEdited;
                                existing.FirstCommitDate = incoming.FirstCommitDate;
                                existing.LastCommitDate = incoming.LastCommitDate;
                                existing.AverageCommitSize = incoming.AverageCommitSize;
                                existing.AverageReviewTime = incoming.AverageReviewTime;
                            }
                            // else: skip updating to preserve last good snapshot
                        }
                        else
                        {
                            if (hasMeaningfulData)
                            {
                                // New contributor snapshot to insert
                                _context.Set<GitHubContribution>().Add(incoming);
                                repository.Contributions.Add(incoming);
                            }
                            // else: no previous and no data -> skip
                        }
                    }

                    await _context.SaveChangesAsync();
                    
                    // Update sync log
                    contributionsSyncLog.Status = "Completed";
                    contributionsSyncLog.CompletedAtUtc = DateTime.UtcNow;
                    contributionsSyncLog.ItemsProcessed = contributions.Count;
                    contributionsSyncLog.TotalItems = projectMembers.Count;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Updated {ContributionCount} contributions for project {ProjectId}", contributions.Count, projectId);
                }
                catch (Exception ex)
                {
                    // Update sync log with error
                    contributionsSyncLog.Status = "Failed";
                    contributionsSyncLog.ErrorMessage = ex.Message;
                    contributionsSyncLog.CompletedAtUtc = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogError(ex, "Error ensuring contributions are up to date for project {ProjectId}", projectId);
                    // Don't throw - this is a background operation
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring contributions are up to date for project {ProjectId}", projectId);
                // Don't throw - this is a background operation
            }
        }

        public async Task<List<GitHubContribution>> ListProjectMembersContributionsAsync(Guid projectId, int daysPeriod = 30, bool force = false)
        {
            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository?.Contributions == null)
                {
                    return new List<GitHubContribution>();
                }

                // If forced or stale, refresh before returning
                var latest = repository.Contributions.OrderByDescending(c => c.CalculatedAtUtc).FirstOrDefault();
                var freshCutoff = DateTime.UtcNow.AddDays(-1);
                var minInterval = DateTime.UtcNow.AddHours(-1);
                var needsRefresh = force || latest == null || latest.CalculatedAtUtc < freshCutoff;
                if (!force && latest != null && latest.CalculatedAtUtc >= minInterval)
                {
                    needsRefresh = false;
                }
                if (needsRefresh)
                {
                    _logger.LogDebug("Refreshing contributions (force={Force}) for project {ProjectId}", force, projectId);
                    await EnsureContributionsUpToDateAsync(projectId, daysPeriod, force);
                    repository = await GetRepositoryAsync(projectId); // reload
                }

                // Filter contributions by the specified period (handle possible null after reload)
                var cutoffDate = DateTime.UtcNow.AddDays(-daysPeriod);
                var contributionsList = repository?.Contributions ?? new List<GitHubContribution>();
                return contributionsList
                    .Where(c => c.CalculatedAtUtc >= cutoffDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contributions for project {ProjectId}", projectId);
                return new List<GitHubContribution>();
            }
        }

        public async Task<GitHubContribution?> GetProjectMemberContributionAsync(Guid projectId, Guid userId, int daysPeriod = 30, bool force = false)
        {
            try
            {
                var contributions = await ListProjectMembersContributionsAsync(projectId, daysPeriod, force);
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
            try
            {
                _logger.LogDebug("SyncAnalyticsAsync called for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
                
                var repository = await GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    _logger.LogWarning("Repository not found for project {ProjectId}, cannot sync analytics", projectId);
                    return false;
                }

                _logger.LogDebug("Repository found for project {ProjectId}: {RepositoryName}", projectId, repository.RepositoryName);

                // Create sync log
                var syncLog = new GitHubSyncLog
                {
                    RepositoryId = repository.RepositoryId,
                    SyncType = "analytics",
                    Status = "Started",
                    StartedAtUtc = DateTime.UtcNow
                };

                _context.Set<GitHubSyncLog>().Add(syncLog);
                await _context.SaveChangesAsync();

                                    _logger.LogDebug("Created sync log for analytics sync: {SyncLogId}", syncLog.SyncLogId);

                try
                {
                    // Get project members to process contributions
                    var projectMembers = await _context.Set<ProjectMember>()
                        .Where(pm => pm.ProjectId == projectId && pm.LeftAtUtc == null)
                        .Include(pm => pm.User)
                        .ToListAsync();

                    _logger.LogDebug("Found {MemberCount} active project members for project {ProjectId}", projectMembers.Count, projectId);

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
                            _logger.LogDebug("Processing contribution for user {UserId} with GitHub username {GitHubUsername}", member.UserId, username);
                            var contribution = await _analyticsService.ProcessUserContributionAsync(
                                repository, member.UserId, username, daysPeriod);
                            contributions.Add(contribution);
                        }
                        else
                        {
                            _logger.LogDebug("Skipping user {UserId} - no GitHub username found", member.UserId);
                        }
                    }

                    _logger.LogDebug("Processed {ContributionCount} user contributions for project {ProjectId}", contributions.Count, projectId);

                    // Remove old contributions and add new ones
                    if (repository.Contributions.Any())
                    {
                        _logger.LogDebug("Removing {OldContributionCount} old contributions for project {ProjectId}", repository.Contributions.Count, projectId);
                        _context.Set<GitHubContribution>().RemoveRange(repository.Contributions);
                    }
                    
                    repository.Contributions = contributions;
                    _context.Set<GitHubContribution>().AddRange(contributions);

                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Saved {ContributionCount} contributions to database for project {ProjectId}", contributions.Count, projectId);

                    // Update sync log
                    syncLog.Status = "Completed";
                    syncLog.CompletedAtUtc = DateTime.UtcNow;
                    syncLog.ItemsProcessed = 1 + contributions.Count;
                    syncLog.TotalItems = 1 + contributions.Count;

                    _logger.LogInformation("Successfully synced analytics for project {ProjectId}: {ContributionCount} contributions processed", projectId, contributions.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during analytics sync for project {ProjectId}", projectId);
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
                var (isValid, _, _) = await _apiClient.ValidateRepositoryAsync(owner, name);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate repository URL: {RepositoryUrl}", repositoryUrl);
                return false;
            }
        }

        public async Task<GitHubRepositoryStatsDto?> GetRepositoryStatsAsync(Guid projectId)
        {
            try
            {
                var repository = await GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    return null;
                }

                // Short-lived cache for repo stats (10 minutes) using simple static cache
                var cacheKey = $"repo-stats:{repository.OwnerName}/{repository.RepositoryName}";
                var (hasCached, cached) = InMemoryGitHubCache.TryGet<GitHubRepositoryStats>(cacheKey);
                GitHubRepositoryStats repoStats;
                if (hasCached)
                {
                    repoStats = cached!;
                }
                else
                {
                    repoStats = await _apiClient.GetRepositoryStatsAsync(repository.OwnerName, repository.RepositoryName);
                    InMemoryGitHubCache.Set(cacheKey, repoStats, TimeSpan.FromMinutes(10));
                }
                var analytics = await GetProjectAnalyticsAsync(projectId);
                var contributions = await ListProjectMembersContributionsAsync(projectId);

                _logger.LogDebug("Repository stats for project {ProjectId}: Found {ContributionCount} contributions", 
                    projectId, contributions.Count);

                // Log contribution details for debugging
                foreach (var contribution in contributions)
                {
                    _logger.LogDebug("Contribution for user {UserId}: Commits={Commits}, LinesChanged={LinesChanged}, Username={Username}", 
                        contribution.UserId, contribution.TotalCommits, contribution.TotalLinesChanged, contribution.GitHubUsername);
                }

                // De-duplicate contributions by GitHubUsername (pick latest snapshot per user)
                var distinctContributions = contributions
                    .Where(c => !string.IsNullOrWhiteSpace(c.GitHubUsername))
                    .GroupBy(c => c.GitHubUsername!, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(c => c.CalculatedAtUtc).First())
                    .ToList();

                var dto = new GitHubRepositoryStatsDto
                {
                    RepositoryName = repository.RepositoryName,
                    OwnerName = repository.OwnerName,
                    FullName = repository.FullName,
                    Description = repository.Description,
                    IsPublic = repository.IsPublic,
                    StarsCount = repository.StarsCount,
                    ForksCount = repository.ForksCount,
                    PrimaryLanguage = repository.PrimaryLanguage,
                    Languages = repository.Languages,
                    LastActivityAtUtc = repository.LastActivityAtUtc,
                    LastSyncedAtUtc = repository.LastSyncedAtUtc,
                    IssueCount = repoStats.IssueCount,
                    PullRequestCount = repoStats.PullRequestCount,
                    BranchCount = repository.Branches?.Count ?? 0, // Use stored branch count
                    Branches = repository.Branches ?? new List<string>(), // ← Add actual branch names
                    ReleaseCount = repoStats.ReleaseCount,
                    Contributors = distinctContributions.Count,
                    TopContributors = distinctContributions
                        .Where(c => c.TotalCommits > 0)
                        .OrderByDescending(c => c.TotalCommits)
                        .Take(5)
                        .Select(c => new TopContributorDto
                        {
                            GitHubUsername = c.GitHubUsername ?? string.Empty,
                            TotalCommits = c.TotalCommits,
                            TotalLinesChanged = c.TotalLinesChanged,
                            UniqueDaysWithCommits = c.UniqueDaysWithCommits
                        })
                        .ToList()
                };

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository stats for project {ProjectId}", projectId);
                return null;
            }
        }

        public async Task<string> GetUserActivitySummaryAsync(Guid projectId, Guid userId, int daysPeriod = 30)
        {
            try
            {
                var contribution = await GetProjectMemberContributionAsync(projectId, userId, daysPeriod);
                if (contribution == null)
                {
                    return "No contribution data available for this user.";
                }

                // Generate the raw analytics summary
                var rawSummary = $"User Activity Summary for {contribution.GitHubUsername} in the last {daysPeriod} days:\n\n";
                rawSummary += $"📝 Commit Activity:\n";
                rawSummary += $"• Total Commits: {contribution.TotalCommits}\n";
                rawSummary += $"• Lines Changed: {contribution.TotalLinesChanged} (+{contribution.TotalAdditions}, -{contribution.TotalDeletions})\n";
                rawSummary += $"• Files Modified: {contribution.FilesModified}\n";
                rawSummary += $"• Active Days: {contribution.UniqueDaysWithCommits}\n\n";

                rawSummary += $"🔧 Issue & PR Activity:\n";
                rawSummary += $"• Issues Created: {contribution.TotalIssuesCreated}\n";
                rawSummary += $"• Pull Requests: {contribution.TotalPullRequestsCreated}\n";
                rawSummary += $"• Code Reviews: {contribution.TotalReviews}\n\n";

                // Latest work context (if available)
                if (!string.IsNullOrWhiteSpace(contribution.LatestPullRequestTitle))
                {
                    var prWhen = contribution.LatestPullRequestCreatedAt?.ToString("yyyy-MM-dd") ?? "";
                    var prNum = contribution.LatestPullRequestNumber.HasValue ? $"#{contribution.LatestPullRequestNumber}" : string.Empty;
                    rawSummary += $"🆕 Latest PR: {contribution.LatestPullRequestTitle} {prNum} {prWhen}\n";
                }
                if (!string.IsNullOrWhiteSpace(contribution.LatestCommitMessage))
                {
                    var cWhen = contribution.LatestCommitDate?.ToString("yyyy-MM-dd HH:mm") ?? "";
                    rawSummary += $"🆕 Latest Commit: {contribution.LatestCommitMessage} ({cWhen} UTC)\n\n";
                }

                rawSummary += $"📊 Activity Patterns:\n";
                if (contribution.CommitsByDayOfWeek.Any())
                {
                    var mostActiveDay = contribution.CommitsByDayOfWeek.OrderByDescending(x => x.Value).First();
                    rawSummary += $"• Most Active Day: {mostActiveDay.Key} ({mostActiveDay.Value} commits)\n";
                }

                if (contribution.CommitsByHour.Any())
                {
                    var mostActiveHour = contribution.CommitsByHour.OrderByDescending(x => x.Value).First();
                    rawSummary += $"• Most Active Hour: {mostActiveHour.Key}:00 ({mostActiveHour.Value} commits)\n";
                }

                if (contribution.LanguagesContributed.Any())
                {
                    rawSummary += $"• Languages: {string.Join(", ", contribution.LanguagesContributed)}\n";
                }

                var contributionScore = await _analyticsService.CalculateUserContributionScoreAsync(contribution);
                rawSummary += $"\n🏆 Contribution Score: {contributionScore}/100\n";

                // Use GPT service to enhance the summary with AI insights
                try
                {
                    var enhancedSummary = await _projectMatchingService.GetGitHubAnalyticsExplanationAsync(
                        rawSummary,
                        null,
                        Services.Projects.Interfaces.GitHubInsightsMode.UserSummary,
                        daysPeriod
                    );
                    
                    _logger.LogInformation("Successfully generated AI-enhanced user activity summary for user {UserId} in project {ProjectId}", userId, projectId);
                    return enhancedSummary;
                }
                catch (Exception gptEx)
                {
                    _logger.LogWarning(gptEx, "GPT service failed for user activity summary, falling back to raw summary for user {UserId} in project {ProjectId}", userId, projectId);
                    return rawSummary;
                }
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
                var contributions = await ListProjectMembersContributionsAsync(projectId, daysPeriod);

                if (analytics == null)
                {
                    return "No analytics data available for this project.";
                }

                // Generate the raw analytics summary
                var rawSummary = await _analyticsService.GenerateActivitySummaryAsync(analytics, contributions);
                
                // Use GPT service to enhance the summary with AI insights
                try
                {
                    var enhancedSummary = await _projectMatchingService.GetGitHubAnalyticsExplanationAsync(
                        rawSummary,
                        null,
                        Services.Projects.Interfaces.GitHubInsightsMode.ProjectSummary,
                        daysPeriod
                    );
                    _logger.LogInformation("Successfully generated AI-enhanced project activity summary for project {ProjectId}", projectId);
                    return enhancedSummary;
                }
                catch (Exception gptEx)
                {
                    _logger.LogWarning(gptEx, "GPT service failed for project activity summary, falling back to raw summary for project {ProjectId}", projectId);
                    return rawSummary;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project activity summary for project {ProjectId}", projectId);
                return "Unable to generate project activity summary due to an error.";
            }
        }

        /// <summary>
        /// Gets personalized GitHub analytics insights based on a user's specific query
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="userQuery">The user's specific question or area of interest</param>
        /// <param name="daysPeriod">Number of days to analyze (default: 30, max: 365)</param>
        /// <returns>AI-powered insights tailored to the user's query</returns>
        public async Task<string> GetPersonalizedAnalyticsInsightsAsync(Guid projectId, string userQuery, int daysPeriod = 30)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userQuery))
                {
                    throw new ArgumentException("User query cannot be null or empty");
                }

                // Validate days period
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    throw new ArgumentException("Days period must be between 1 and 365");
                }

                var analytics = await GetProjectAnalyticsAsync(projectId, daysPeriod);
                var contributions = await ListProjectMembersContributionsAsync(projectId, daysPeriod);

                if (analytics == null)
                {
                    return "No analytics data available for this project.";
                }

                // Generate the raw analytics summary
                var rawSummary = await _analyticsService.GenerateActivitySummaryAsync(analytics, contributions);
                
                // Use GPT service to provide personalized insights based on the user's query
                try
                {
                    var personalizedInsights = await _projectMatchingService.GetGitHubAnalyticsExplanationAsync(
                        rawSummary, 
                        userQuery,
                        Services.Projects.Interfaces.GitHubInsightsMode.QA,
                        daysPeriod
                    );
                    
                    _logger.LogInformation("Successfully generated personalized analytics insights for project {ProjectId} with query: {UserQuery}", projectId, userQuery);
                    return personalizedInsights;
                }
                catch (Exception gptEx)
                {
                    _logger.LogWarning(gptEx, "GPT service failed for personalized insights, falling back to raw summary for project {ProjectId}", projectId);
                    return $"Unable to generate personalized insights for your query: '{userQuery}'. Here's the standard analytics summary:\n\n{rawSummary}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized analytics insights for project {ProjectId} with query: {UserQuery}", projectId, userQuery);
                return "Unable to generate personalized analytics insights due to an error.";
            }
        }

        /// <summary>
        /// Checks if a repository is already linked to any project
        /// </summary>
        /// <param name="owner">Repository owner</param>
        /// <param name="name">Repository name</param>
        /// <returns>True if repository is already linked, false otherwise</returns>
        public async Task<bool> IsRepositoryAlreadyLinkedAsync(string owner, string name)
        {
            try
            {
                var existingRepository = await _context.Set<GitHubRepository>()
                    .FirstOrDefaultAsync(r => r.OwnerName == owner && r.RepositoryName == name);
                
                return existingRepository != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if repository {Owner}/{Name} is already linked", owner, name);
                return false;
            }
        }

        /// <summary>
        /// Gets the project ID that a repository is linked to
        /// </summary>
        /// <param name="owner">Repository owner</param>
        /// <param name="name">Repository name</param>
        /// <returns>Project ID if repository is linked, null otherwise</returns>
        public async Task<Guid?> GetProjectIdForRepositoryAsync(string owner, string name)
        {
            try
            {
                var repository = await _context.Set<GitHubRepository>()
                    .FirstOrDefaultAsync(r => r.OwnerName == owner && r.RepositoryName == name);
                
                return repository?.ProjectId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project ID for repository {Owner}/{Name}", owner, name);
                return null;
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

        public string? ExtractGitHubUsername(string githubUrl)
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
