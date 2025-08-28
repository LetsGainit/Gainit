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

        /// <summary>
        /// Builds repository-level analytics from already-fetched repository metadata and cached state.
        /// Note: This method does not call GitHub; it derives metrics from the stored <see cref="GitHubRepository"/>
        /// and initializes time-bucketed structures for charts.
        /// </summary>
        /// <param name="repository">Repository entity with stored metadata (stars, forks, languages, etc.).</param>
        /// <param name="daysPeriod">Analysis period (e.g., 30 days). Used for chart ranges and summaries.</param>
        /// <returns>Computed <see cref="GitHubAnalytics"/> object ready to persist.</returns>
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

                // Process basic repository stats (static fields from stored repository)
                analytics.TotalStars = repository.StarsCount ?? 0;
                analytics.TotalForks = repository.ForksCount ?? 0;
                analytics.TotalWatchers = 0; // Not tracked currently
                analytics.TotalBranches = repository.Branches?.Count ?? 0;
                analytics.TotalReleases = 0; // Not tracked currently
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

                // Derive branches count from stored metadata (already set above)
                analytics.TotalBranches = repository.Branches?.Count ?? 0;

                // Repository-level Issues (open/closed) within the analysis window
                var repoIssues = await _gitHubApiClient.GetIssuesAsync(repository.OwnerName, repository.RepositoryName, daysPeriod);
                if (repoIssues != null)
                {
                    var cutoffIssues = DateTime.UtcNow.AddDays(-daysPeriod);
                    var issuesOnly = repoIssues
                        .Where(i => i.PullRequest == null)
                        .Where(i => i.CreatedAt >= cutoffIssues)
                        .ToList();

                    var openIssues = issuesOnly.Count(i => string.Equals(i.State, "open", StringComparison.OrdinalIgnoreCase));
                    var closedIssues = issuesOnly.Count(i => string.Equals(i.State, "closed", StringComparison.OrdinalIgnoreCase));
                    analytics.OpenIssues = openIssues;
                    analytics.ClosedIssues = closedIssues;
                    analytics.TotalIssues = openIssues + closedIssues;
                }

                // Repository-level PRs (open/merged/closed) within the analysis window
                var repoPrs = await _gitHubApiClient.GetPullRequestsAsync(repository.OwnerName, repository.RepositoryName, daysPeriod);
                if (repoPrs != null)
                {
                    var openPr = repoPrs.Count(pr => string.Equals(pr.State, "open", StringComparison.OrdinalIgnoreCase));
                    var mergedPr = repoPrs.Count(pr => pr.MergedAt.HasValue);
                    var closedPr = repoPrs.Count(pr => string.Equals(pr.State, "closed", StringComparison.OrdinalIgnoreCase) && !pr.MergedAt.HasValue);

                    analytics.OpenPullRequests = openPr;
                    analytics.MergedPullRequests = mergedPr;
                    analytics.TotalPullRequests = openPr + mergedPr + closedPr;
                }

                // Populate commit-based metrics from REST API commit history
                var commitHistory = await _gitHubApiClient.GetCommitHistoryAsync(repository.OwnerName, repository.RepositoryName, daysPeriod);
                if (commitHistory != null && commitHistory.Any())
                {
                    analytics.TotalCommits = commitHistory.Count;

                    // Bucket into weekly keys already initialized
                    var weeklyKeys = analytics.WeeklyCommits.Keys
                        .Select(k => DateTime.Parse(k))
                        .OrderBy(d => d)
                        .ToList();
                    foreach (var commit in commitHistory)
                    {
                        // Weekly
                        var commitDate = commit.CommittedDate.Date;
                        var weekKeyDate = weeklyKeys.LastOrDefault(d => d <= commitDate);
                        if (weekKeyDate != default)
                        {
                            var wk = weekKeyDate.ToString("yyyy-MM-dd");
                            analytics.WeeklyCommits[wk] = analytics.WeeklyCommits[wk] + 1;
                        }

                        // Monthly
                        var monthKey = new DateTime(commitDate.Year, commitDate.Month, 1).ToString("yyyy-MM");
                        if (analytics.MonthlyCommits.ContainsKey(monthKey))
                        {
                            analytics.MonthlyCommits[monthKey] = analytics.MonthlyCommits[monthKey] + 1;
                        }
                    }

                    // Contributors: prefer stored per-user contributions (deduped) over raw commit author names
                    var distinctUsers = repository.Contributions
                        .Where(c => c.TotalCommits > 0)
                        .Where(c => !string.IsNullOrWhiteSpace(c.GitHubUsername))
                        .Select(c => c.GitHubUsername!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    analytics.TotalContributors = distinctUsers;
                    analytics.ActiveContributors = distinctUsers;
                }
                else
                {
                    // Fallback: use stored contributions (deduped by username, with activity)
                    var distinctUsers = repository.Contributions
                        .Where(c => c.TotalCommits > 0)
                        .Where(c => !string.IsNullOrWhiteSpace(c.GitHubUsername))
                        .Select(c => c.GitHubUsername!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    analytics.TotalContributors = distinctUsers;
                    analytics.ActiveContributors = distinctUsers;
                }

                _logger.LogInformation("Repository analytics processed successfully for {Repository}", repository.FullName);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing repository analytics for {Repository}", repository.FullName);
                throw;
            }
        }

        /// <summary>
        /// Computes per-user contribution analytics across all stored branches for a repository.
        /// Fetches commits per branch for the user and optionally enriches with commit details (capped) to populate
        /// additions, deletions, and files changed.
        /// </summary>
        /// <param name="repository">Repository with <see cref="GitHubRepository.Branches"/> populated.</param>
        /// <param name="userId">Internal user identifier associated with the GitHub username.</param>
        /// <param name="username">GitHub username to aggregate contributions for.</param>
        /// <param name="daysPeriod">Days window to look back for contributions.</param>
        /// <returns>Aggregated <see cref="GitHubContribution"/> for the user.</returns>
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

                // Fetch real GitHub contribution data from ALL branches
                var allCommits = new List<GitHubAnalyticsCommitNode>();

                // Normalize and de-duplicate branch list before calling the API
                var distinctBranches = (repository.Branches ?? new List<string>())
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Select(b => b.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (distinctBranches.Count != (repository.Branches?.Count ?? 0))
                {
                    _logger.LogDebug("Normalized branch list for {Repository}: {OriginalCount} -> {DistinctCount}",
                        repository.FullName, repository.Branches?.Count ?? 0, distinctBranches.Count);
                }

                foreach (var branch in distinctBranches)
                {
                    _logger.LogDebug("Fetching contributions for {Username} on branch {Branch} in {Repository}", 
                        username, branch, repository.FullName);
                        
                    var branchCommits = await _gitHubApiClient.GetUserContributionsForBranchAsync(
                        repository.OwnerName, 
                        repository.RepositoryName, 
                        username, 
                        branch,           // ← Each branch individually
                        daysPeriod);
                        
                    allCommits.AddRange(branchCommits);
                    
                    _logger.LogDebug("Found {CommitCount} commits for {Username} on branch {Branch} in {Repository}", 
                        branchCommits.Count, username, branch, repository.FullName);
                }

                if (allCommits.Any())
                {
                    _logger.LogInformation("Found {TotalCommitCount} total GitHub contributions for {Username} across {BranchCount} branches in {Repository}", 
                        allCommits.Count, username, distinctBranches.Count, repository.FullName);

                    // Log commits per branch for debugging
                    foreach (var branch in distinctBranches)
                    {
                        var branchCommitCount = allCommits.Count(c => c.Id.StartsWith(branch) || c.Message.Contains(branch));
                        _logger.LogDebug("Branch {Branch}: {CommitCount} commits for {Username}", branch, branchCommitCount, username);
                    }

                    // Optionally enrich with per-commit details to populate additions/deletions/files
                    // Cap detailed lookups to avoid rate limits
                    // Caps for external calls (tunable)
                    const int MaxCommitDetails = 40;
                    const int MaxPrReviewsLookups = 20;

                    var maxDetails = Math.Min(MaxCommitDetails, allCommits.Count);
                    for (int i = 0; i < maxDetails; i++)
                    {
                        var c = allCommits[i];
                        // If stats were not present from list, try to fetch details
                        if (c.Additions == 0 && c.Deletions == 0 && c.ChangedFiles == 0)
                        {
                            try
                            {
                                var details = await _gitHubApiClient.GetCommitDetailsAsync(repository.OwnerName, repository.RepositoryName, c.Id);
                                if (details?.Stats != null)
                                {
                                    c.Additions = details.Stats.Additions;
                                    c.Deletions = details.Stats.Deletions;
                                    c.ChangedFiles = details.Files?.Count ?? 0;
                                }
                            }
                            catch (InvalidOperationException ex) when (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogWarning("Rate limit hit during commit detail enrichment. Stopping enrichment early at index {Index}.", i);
                                break; // early-stop enrichment to preserve quota
                            }
                        }
                    }

                    // Process commit data from ALL branches
                    contribution.TotalCommits = allCommits.Count;
                    contribution.TotalAdditions = allCommits.Sum(c => c.Additions);
                    contribution.TotalDeletions = allCommits.Sum(c => c.Deletions);
                    contribution.TotalLinesChanged = contribution.TotalAdditions + contribution.TotalDeletions;
                    contribution.FilesModified = allCommits.Sum(c => c.ChangedFiles);

                    // Calculate unique days with commits
                    var commitDates = allCommits
                        .Select(c => c.CommittedDate.Date)
                        .Distinct()
                        .ToList();
                    contribution.UniqueDaysWithCommits = commitDates.Count;

                    // Calculate streaks (longest and current) from commitDates
                    var orderedDays = commitDates.OrderBy(d => d).ToList();
                    int longest = 0, current = 0;
                    DateTime? prevDay = null;
                    foreach (var day in orderedDays)
                    {
                        if (prevDay.HasValue && (day - prevDay.Value).TotalDays == 1)
                        {
                            current++;
                        }
                        else
                        {
                            current = 1;
                        }
                        if (current > longest) longest = current;
                        prevDay = day;
                    }
                    contribution.LongestStreak = longest;
                    contribution.CurrentStreak = current;

                    // Process activity patterns from ALL branches
                    contribution.CommitsByDayOfWeek = ProcessCommitsByDayOfWeek(allCommits);
                    contribution.CommitsByHour = ProcessCommitsByHour(allCommits);
                    contribution.ActivityByMonth = ProcessActivityByMonth(allCommits);

                    // Languages: prefer repository languages (stable) over commit-message heuristics
                    contribution.LanguagesContributed = repository.Languages?.Any() == true
                        ? repository.Languages
                        : ExtractLanguagesFromCommits(allCommits);

                    // Latest commit details
                    var latestCommit = allCommits.OrderByDescending(c => c.CommittedDate).FirstOrDefault();
                    if (latestCommit != null)
                    {
                        contribution.LatestCommitMessage = latestCommit.Message;
                        contribution.LatestCommitDate = latestCommit.CommittedDate;
                    }

                    // Augment with Issues, PRs, and Reviews using REST API
                    // PRs
                    var pullRequests = await _gitHubApiClient.GetPullRequestsAsync(repository.OwnerName, repository.RepositoryName, daysPeriod);
                    if (pullRequests != null && pullRequests.Any())
                    {
                        var cutoff = DateTime.UtcNow.AddDays(-daysPeriod);
                        var userPrs = pullRequests
                            .Where(pr => pr.CreatedAt >= cutoff)
                            .Where(pr => string.Equals(pr.User?.Login, username, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        contribution.TotalPullRequestsCreated = userPrs.Count;

                        // Breakdown by state
                        var prsOpened = userPrs.Count(pr => string.Equals(pr.State, "open", StringComparison.OrdinalIgnoreCase));
                        var prsMerged = userPrs.Count(pr => pr.MergedAt.HasValue);
                        var prsClosed = userPrs.Count(pr => string.Equals(pr.State, "closed", StringComparison.OrdinalIgnoreCase) && !pr.MergedAt.HasValue);
                        contribution.OpenPullRequestsCreated = prsOpened;
                        contribution.MergedPullRequestsCreated = prsMerged;
                        contribution.ClosedPullRequestsCreated = prsClosed;

                        // Latest PR details
                        var latestPr = userPrs
                            .OrderByDescending(pr => pr.UpdatedAt ?? pr.CreatedAt)
                            .FirstOrDefault();
                        if (latestPr != null)
                        {
                            contribution.LatestPullRequestTitle = latestPr.Title;
                            contribution.LatestPullRequestNumber = latestPr.Number;
                            contribution.LatestPullRequestCreatedAt = latestPr.CreatedAt;
                        }

                        // Reviews made by the user on their own PRs (or in general, could be extended to all PRs)
                        int totalReviews = 0;
                        foreach (var pr in userPrs.Take(MaxPrReviewsLookups)) // cap review lookups
                        {
                            var reviews = await _gitHubApiClient.GetPullRequestReviewsAsync(repository.OwnerName, repository.RepositoryName, pr.Number);
                            totalReviews += reviews.Count(r => string.Equals(r.User?.Login, username, StringComparison.OrdinalIgnoreCase));
                        }
                        contribution.TotalReviews = totalReviews;
                        // If controller maps breakdowns, it can read them from DTO; core entity doesn't currently store
                    }

                    // Issues
                    var issues = await _gitHubApiClient.GetIssuesAsync(repository.OwnerName, repository.RepositoryName, daysPeriod);
                    if (issues != null && issues.Any())
                    {
                        var cutoffIssues = DateTime.UtcNow.AddDays(-daysPeriod);
                        // Exclude PRs returned by issues endpoint, respect time window by CreatedAt
                        var userIssues = issues
                            .Where(i => i.PullRequest == null)
                            .Where(i => i.CreatedAt >= cutoffIssues)
                            .Where(i => string.Equals(i.User?.Login, username, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        contribution.TotalIssuesCreated = userIssues.Count;
                        var issuesOpened = userIssues.Count(i => string.Equals(i.State, "open", StringComparison.OrdinalIgnoreCase));
                        var issuesClosed = userIssues.Count(i => string.Equals(i.State, "closed", StringComparison.OrdinalIgnoreCase));
                        contribution.OpenIssuesCreated = issuesOpened;
                        contribution.ClosedIssuesCreated = issuesClosed;
                    }

                    // Collaborators metric is simplistic: distinct authors appearing in commit set (excluding self)
                    contribution.CollaboratorsInteractedWith = Math.Max(0, allCommits
                        .Select(c => c.Author?.Name)
                        .Where(n => !string.IsNullOrEmpty(n) && !string.Equals(n, username, StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count());
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

                _logger.LogInformation("User contribution analytics processed successfully for {Username} in {Repository}: {Commits} commits across {BranchCount} branches, {LinesChanged} lines changed", 
                    username, repository.FullName, contribution.TotalCommits, distinctBranches.Count, contribution.TotalLinesChanged);
                return contribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user contribution analytics for {Username} in {Repository}", 
                    username, repository.FullName);
                throw;
            }
        }

        /// <summary>
        /// Placeholder for future aggregation across multiple periods (e.g., 30/90/365 days) from stored analytics.
        /// Currently returns an empty list and does not query the database.
        /// </summary>
        /// <param name="repositoryId">Repository ID to aggregate.</param>
        /// <param name="periods">List of day windows to aggregate.</param>
        /// <returns>Empty list until implemented.</returns>
        public async Task<List<GitHubAnalytics>> GetAggregatedAnalyticsAsync(Guid repositoryId, List<int> periods)
        {
            _logger.LogInformation("Getting aggregated analytics for repository {RepositoryId} with periods: {Periods}", 
                repositoryId, string.Join(", ", periods));

            // This would typically query the database for existing analytics
            // For now, return an empty list
            return new List<GitHubAnalytics>();
        }

        /// <summary>
        /// Placeholder for long-term trend analysis (time-series) across the specified period.
        /// Currently returns a simple anonymous object.
        /// </summary>
        /// <param name="repositoryId">Repository ID to analyze.</param>
        /// <param name="daysPeriod">Period window for trends (default 365).</param>
        /// <returns>Placeholder trends payload.</returns>
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

        /// <summary>
        /// Heuristic repository health score based on activity, engagement, issue/PR handling, and recency.
        /// Not a scientific metric; useful for relative comparisons in the UI.
        /// </summary>
        /// <param name="analytics">Computed analytics snapshot.</param>
        /// <returns>Health score in the range [0, 100].</returns>
        public async Task<double> CalculateRepositoryHealthScoreAsync(GitHubAnalytics analytics)
        {
            try
            {
                var score = 0.0;

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

        /// <summary>
        /// Heuristic user contribution score based on commit activity, code change size, consistency, and collaboration.
        /// </summary>
        /// <param name="contribution">User contribution snapshot.</param>
        /// <returns>Contribution score in the range [0, 100].</returns>
        public async Task<double> CalculateUserContributionScoreAsync(GitHubContribution contribution)
        {
            try
            {
                var score = 0.0;

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

        /// <summary>
        /// Generates a human-readable activity summary combining repository analytics and user contributions.
        /// Currently builds a textual report and optionally computes a health score.
        /// </summary>
        /// <param name="analytics">Repository analytics snapshot.</param>
        /// <param name="contributions">List of user contributions to include.</param>
        /// <returns>Textual summary for display or AI enrichment.</returns>
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

        /// <summary>
        /// Placeholder for pruning analytics older than the specified retention window.
        /// Not yet implemented (no-op).
        /// </summary>
        /// <param name="retentionDays">Number of days to retain.</param>
        /// <returns>0 (no rows affected) until implemented.</returns>
        public async Task<int> CleanupOldAnalyticsAsync(int retentionDays = 365)
        {
            _logger.LogInformation("Cleaning up analytics data older than {RetentionDays} days", retentionDays);

            // This would typically delete old analytics data from the database
            // For now, return 0 as no cleanup was performed
            return 0;
        }

        /// <summary>
        /// Placeholder for exporting analytics in JSON/CSV, etc.
        /// Currently returns a minimal JSON payload describing the export request.
        /// </summary>
        /// <param name="repositoryId">Repository to export.</param>
        /// <param name="format">Export format (e.g., json, csv).</param>
        /// <returns>Byte array of the serialized export.</returns>
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
            // Include current month and previous 11 months
            var startMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-11);
            for (int i = 0; i < 12; i++)
            {
                var monthStart = startMonth.AddMonths(i);
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
            var tz = ResolveIsraelTimeZone();

            foreach (var commit in commits)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(commit.CommittedDate, DateTimeKind.Utc), tz);
                var dayOfWeek = local.DayOfWeek.ToString();
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
            var tz = ResolveIsraelTimeZone();

            foreach (var commit in commits)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(commit.CommittedDate, DateTimeKind.Utc), tz);
                var hour = local.Hour.ToString("00");
                if (hourCounts.ContainsKey(hour))
                {
                    hourCounts[hour]++;
                }
            }
            
            return hourCounts;
        }

        private TimeZoneInfo ResolveIsraelTimeZone()
        {
            // Prefer Windows ID on Windows; try IANA as fallback
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");
                }
                catch
                {
                    return TimeZoneInfo.Utc; // fallback to UTC
                }
            }
        }

        private Dictionary<string, int> ProcessActivityByMonth(List<GitHubAnalyticsCommitNode> commits)
        {
            var monthCounts = InitializeMonthlyTracking();
            foreach (var commit in commits)
            {
                var month = new DateTime(commit.CommittedDate.Year, commit.CommittedDate.Month, 1).ToString("yyyy-MM");
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
