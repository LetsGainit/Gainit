using GainIt.API.Services.GitHub.Interfaces;
using GainIt.API.DTOs.Requests.GitHub;
using GainIt.API.DTOs.ViewModels.GitHub;
using GainIt.API.DTOs.ViewModels.GitHub.Base;
using GainIt.API.DTOs.ViewModels.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GainIt.API.Controllers.Projects
{
    /// <summary>
    /// GitHub integration controller for managing repository links, analytics, and data synchronization
    /// </summary>
    /// <remarks>
    /// This controller provides comprehensive GitHub integration capabilities including:
    /// - Repository linking and management
    /// - Analytics and statistics tracking
    /// - User contribution analysis
    /// - Activity summaries and insights
    /// - Data synchronization and validation
    /// 
    /// Uses GitHub REST API for public repository access without authentication requirements.
    /// All endpoints support parallel data fetching for optimal performance.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Allow all endpoints for testing REST API integration
    [Produces("application/json")]
    public class GitHubController : ControllerBase
    {
        private readonly IGitHubService _gitHubService;
        private readonly ILogger<GitHubController> _logger;

        public GitHubController(
            IGitHubService gitHubService,
            ILogger<GitHubController> logger)
        {
            _gitHubService = gitHubService;
            _logger = logger;
        }

        /// <summary>
        /// Links a GitHub repository to a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project to link the repository to</param>
        /// <param name="request">Repository link request containing the GitHub repository URL</param>
        /// <returns>Repository information and link confirmation</returns>
        /// <response code="200">Repository successfully linked to the project</response>
        /// <response code="400">Invalid repository URL or request data</response>
        /// <response code="409">Repository already linked or conflict occurred</response>
        /// <response code="500">Internal server error during linking process</response>
        /// <example>
        /// POST /api/github/projects/12345678-1234-1234-1234-123456789012/link
        /// {
        ///   "repositoryUrl": "https://github.com/owner/repository"
        /// }
        /// </example>
        [HttpPost("projects/{projectId}/link")]
        [ProducesResponseType(typeof(GitHubRepositoryLinkResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LinkRepository(Guid projectId, [FromBody] GitHubRepositoryLinkDto request)
        {
            _logger.LogDebug("LinkRepository called for project {ProjectId} with URL: {RepositoryUrl}", projectId, request?.RepositoryUrl);
            
            try
            {
                if (string.IsNullOrWhiteSpace(request?.RepositoryUrl))
                {
                    _logger.LogWarning("Repository URL is null or empty for project {ProjectId}", projectId);
                    return BadRequest(new ErrorResponseDto { Error = "Repository URL is required" });
                }

                _logger.LogDebug("Calling _gitHubService.LinkRepositoryAsync for project {ProjectId} with URL: {RepositoryUrl}", projectId, request.RepositoryUrl);
                var result = await _gitHubService.LinkRepositoryAsync(projectId, request.RepositoryUrl);
                
                _logger.LogInformation("GitHub repository linked successfully to project {ProjectId}: {RepositoryUrl}", projectId, request.RepositoryUrl);
                _logger.LogDebug("Link result: Success={Success}, Message={Message}, RepositoryId={RepositoryId}", 
                    result.Success, result.Message, result.RepositoryId);
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("ArgumentException in LinkRepository for project {ProjectId}: {Message}", projectId, ex.Message);
                return BadRequest(new ErrorResponseDto { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("InvalidOperationException in LinkRepository for project {ProjectId}: {Message}", projectId, ex.Message);
                return Conflict(new ErrorResponseDto { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking GitHub repository to project {ProjectId} with URL: {RepositoryUrl}", projectId, request?.RepositoryUrl);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while linking the repository" });
            }
        }

        /// <summary>
        /// Gets the GitHub repository linked to a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <returns>Repository information including name, owner, description, and basic stats</returns>
        /// <response code="200">Repository information retrieved successfully</response>
        /// <response code="404">No repository linked to this project</response>
        /// <response code="500">Internal server error during retrieval</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/repository
        /// </example>
        [HttpGet("projects/{projectId}/repository")]
        [ProducesResponseType(typeof(GitHubRepositoryInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLinkedRepository(Guid projectId)
        {
            _logger.LogDebug("GetLinkedRepository called for project {ProjectId}", projectId);
            
            try
            {
                _logger.LogDebug("Calling _gitHubService.GetRepositoryAsync for project {ProjectId}", projectId);
                var repository = await _gitHubService.GetRepositoryAsync(projectId);
                
                if (repository == null)
                {
                    _logger.LogWarning("No repository found for project {ProjectId}", projectId);
                    return NotFound(new ErrorResponseDto { Error = "No repository linked to this project" });
                }

                _logger.LogDebug("Repository found for project {ProjectId}: {RepositoryName}", projectId, repository.RepositoryName);
                
                var dto = new GitHubRepositoryInfoDto
                {
                    RepositoryId = repository.RepositoryId.ToString(),
                    RepositoryName = repository.RepositoryName,
                    OwnerName = repository.OwnerName,
                    FullName = repository.FullName,
                    Description = repository.Description,
                    IsPublic = repository.IsPublic,
                    PrimaryLanguage = repository.PrimaryLanguage,
                    Languages = repository.Languages,
                    StarsCount = repository.StarsCount ?? 0,
                    ForksCount = repository.ForksCount ?? 0
                };

                _logger.LogInformation("Successfully retrieved repository info for project {ProjectId}: {RepositoryName}", projectId, repository.RepositoryName);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting linked repository for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving the repository" });
            }
        }

        /// <summary>
        /// Gets repository statistics for a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <returns>Detailed repository statistics including stars, forks, issues, and contributors</returns>
        /// <response code="200">Repository statistics retrieved successfully</response>
        /// <response code="404">Repository statistics not found for this project</response>
        /// <response code="500">Internal server error during retrieval</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/stats
        /// </example>
        [HttpGet("projects/{projectId}/stats")]
        [ProducesResponseType(typeof(GitHubRepositoryStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRepositoryStats(Guid projectId)
        {
            _logger.LogDebug("GetRepositoryStats called for project {ProjectId}", projectId);
            
            try
            {
                _logger.LogDebug("Calling _gitHubService.GetRepositoryStatsAsync for project {ProjectId}", projectId);
                var stats = await _gitHubService.GetRepositoryStatsAsync(projectId);
                
                if (stats == null)
                {
                    _logger.LogWarning("Repository stats not found for project {ProjectId}", projectId);
                    return NotFound(new ErrorResponseDto { Error = "Repository stats not found" });
                }

                _logger.LogDebug("Repository stats found for project {ProjectId}: {RepositoryName}", projectId, stats.RepositoryName);
                _logger.LogInformation("Successfully retrieved repository stats for project {ProjectId}: {RepositoryName}", projectId, stats.RepositoryName);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository stats for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving repository stats" });
            }
        }

        /// <summary>
        /// Gets GitHub analytics for a project with automatic data refresh
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="daysPeriod">Number of days to analyze (1-365, default: 30)</param>
        /// <param name="force">Force refresh of analytics data (default: false)</param>
        /// <returns>Comprehensive analytics data including commits, issues, pull requests, and contributor activity</returns>
        /// <response code="200">Analytics data retrieved or generated successfully</response>
        /// <response code="400">Invalid days period parameter</response>
        /// <response code="500">Failed to retrieve or generate analytics data</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/analytics?daysPeriod=30&amp;force=false
        /// </example>
        [HttpGet("projects/{projectId}/analytics")]
        [ProducesResponseType(typeof(GitHubProjectAnalyticsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectAnalytics(Guid projectId, [FromQuery] int daysPeriod = 30, [FromQuery] bool force = false)
        {
            _logger.LogDebug("GetProjectAnalytics called for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
            
            try
            {
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    _logger.LogWarning("Invalid daysPeriod {DaysPeriod} for project {ProjectId}", daysPeriod, projectId);
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                _logger.LogDebug("Calling _gitHubService.GetProjectAnalyticsAsync for project {ProjectId} with daysPeriod {DaysPeriod}, force={Force}", projectId, daysPeriod, force);
                var analytics = await _gitHubService.GetProjectAnalyticsAsync(projectId, daysPeriod, force);
                
                if (analytics == null)
                {
                    _logger.LogWarning("Failed to retrieve or create analytics for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
                    return StatusCode(500, new ErrorResponseDto { Error = "Failed to retrieve analytics data. Please try again later." });
                }

                _logger.LogDebug("Analytics data retrieved/created for project {ProjectId}: CalculatedAt={CalculatedAt}", projectId, analytics.CalculatedAtUtc);
                
                var response = new GitHubProjectAnalyticsResponseDto
                {
                    ProjectId = projectId,
                    DaysPeriod = daysPeriod,
                    CalculatedAt = analytics.CalculatedAtUtc,
                    Analytics = analytics
                };

                _logger.LogInformation("Successfully retrieved analytics for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project analytics for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving analytics" });
            }
        }

        /// <summary>
        /// Gets user contributions for a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="daysPeriod">Number of days to analyze (1-365, default: 30)</param>
        /// <param name="force">Force refresh of contribution data (default: false)</param>
        /// <returns>List of user contributions including commits, issues, pull requests, and reviews</returns>
        /// <response code="200">User contributions retrieved successfully</response>
        /// <response code="400">Invalid days period parameter</response>
        /// <response code="500">Internal server error during retrieval</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/contributions?daysPeriod=30&amp;force=false
        /// </example>
        [HttpGet("projects/{projectId}/contributions")]
        [ProducesResponseType(typeof(GitHubUserContributionsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserContributions(Guid projectId, [FromQuery] int daysPeriod = 30, [FromQuery] bool force = false)
        {
            _logger.LogDebug("GetUserContributions called for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
            
            try
            {
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    _logger.LogWarning("Invalid daysPeriod {DaysPeriod} for project {ProjectId}", daysPeriod, projectId);
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                _logger.LogDebug("Calling _gitHubService.ListProjectMembersContributionsAsync for project {ProjectId} with daysPeriod {DaysPeriod}, force={Force}", projectId, daysPeriod, force);
                var contributions = await _gitHubService.ListProjectMembersContributionsAsync(projectId, daysPeriod, force);
                
                _logger.LogDebug("Retrieved {ContributionsCount} contributions for project {ProjectId}", contributions.Count, projectId);
                
                var response = new GitHubUserContributionsResponseDto
                {
                    ProjectId = projectId,
                    DaysPeriod = daysPeriod,
                    Contributors = contributions.Count,
                    Contributions = contributions.Select(c => new GitHubUserContributionDto
                    {
                        UserId = c.UserId,
                        GitHubUsername = c.GitHubUsername,
                        TotalCommits = c.TotalCommits,
                        TotalLinesChanged = c.TotalLinesChanged,
                        TotalIssuesCreated = c.TotalIssuesCreated,
                        TotalPullRequestsCreated = c.TotalPullRequestsCreated,
                        TotalReviews = c.TotalReviews,
                        UniqueDaysWithCommits = c.UniqueDaysWithCommits,
                        CalculatedAtUtc = c.CalculatedAtUtc
                    }).ToList()
                };

                _logger.LogInformation("Successfully retrieved {ContributionsCount} user contributions for project {ProjectId}", contributions.Count, projectId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contributions for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving contributions" });
            }
        }

        /// <summary>
        /// Gets contribution analytics for a specific user in a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="daysPeriod">Number of days to analyze (1-365, default: 30)</param>
        /// <param name="force">Force refresh of contribution data (default: false)</param>
        /// <returns>Detailed contribution analytics for the specified user</returns>
        /// <response code="200">User contribution analytics retrieved successfully</response>
        /// <response code="400">Invalid days period parameter</response>
        /// <response code="404">No contribution data available for this user</response>
        /// <response code="500">Internal server error during retrieval</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/users/87654321-4321-4321-4321-210987654321/contributions?daysPeriod=30
        /// </example>
        [HttpGet("projects/{projectId}/users/{userId}/contributions")]
        [ProducesResponseType(typeof(GitHubUserContributionDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserContribution(Guid projectId, Guid userId, [FromQuery] int daysPeriod = 30, [FromQuery] bool force = false)
        {
            try
            {
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                var contribution = await _gitHubService.GetProjectMemberContributionAsync(projectId, userId, daysPeriod, force);
                
                if (contribution == null)
                {
                    return NotFound(new ErrorResponseDto { Error = "No contribution data available for this user" });
                }

                var response = new GitHubUserContributionDetailResponseDto
                {
                    ProjectId = projectId,
                    UserId = userId,
                    DaysPeriod = daysPeriod,
                    Contribution = new GitHubDetailedContributionDto
                    {
                        GitHubUsername = contribution.GitHubUsername,
                        TotalCommits = contribution.TotalCommits,
                        TotalAdditions = contribution.TotalAdditions,
                        TotalDeletions = contribution.TotalDeletions,
                        TotalLinesChanged = contribution.TotalLinesChanged,
                        TotalIssuesCreated = contribution.TotalIssuesCreated,
                        TotalPullRequestsCreated = contribution.TotalPullRequestsCreated,
                        TotalReviews = contribution.TotalReviews,
                        UniqueDaysWithCommits = contribution.UniqueDaysWithCommits,
                        FilesModified = contribution.FilesModified.ToString(),
                        LanguagesContributed = contribution.LanguagesContributed,
                        CalculatedAtUtc = contribution.CalculatedAtUtc,
                        // Real breakdowns mapped from entity
                        PullRequestsOpened = contribution.OpenPullRequestsCreated,
                        PullRequestsMerged = contribution.MergedPullRequestsCreated,
                        PullRequestsClosed = contribution.ClosedPullRequestsCreated,
                        IssuesOpened = contribution.OpenIssuesCreated,
                        IssuesClosed = contribution.ClosedIssuesCreated,
                        LatestPullRequestTitle = contribution.LatestPullRequestTitle,
                        LatestPullRequestNumber = contribution.LatestPullRequestNumber,
                        LatestPullRequestCreatedAt = contribution.LatestPullRequestCreatedAt,
                        LatestCommitMessage = contribution.LatestCommitMessage,
                        LatestCommitDate = contribution.LatestCommitDate
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contribution for user {UserId} in project {ProjectId}", userId, projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving user contribution" });
            }
        }

        /// <summary>
        /// Gets user activity summary for AI context
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="daysPeriod">Number of days to analyze (1-365, default: 30)</param>
        /// <returns>AI-generated activity summary for the specified user</returns>
        /// <response code="200">User activity summary generated successfully</response>
        /// <response code="400">Invalid days period parameter</response>
        /// <response code="500">Internal server error during generation</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/users/87654321-4321-4321-4321-210987654321/activity?daysPeriod=30
        /// </example>
        [HttpGet("projects/{projectId}/users/{userId}/activity")]
        [ProducesResponseType(typeof(GitHubUserActivitySummaryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserActivitySummary(Guid projectId, Guid userId, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                var summary = await _gitHubService.GetUserActivitySummaryAsync(projectId, userId, daysPeriod);
                
                var response = new GitHubUserActivitySummaryResponseDto
                {
                    ProjectId = projectId,
                    UserId = userId,
                    DaysPeriod = daysPeriod,
                    Summary = summary
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity summary for user {UserId} in project {ProjectId}", userId, projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving user activity summary" });
            }
        }

        /// <summary>
        /// Gets project activity summary for AI context
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="daysPeriod">Number of days to analyze (1-365, default: 30)</param>
        /// <returns>AI-generated activity summary for the entire project</returns>
        /// <response code="200">Project activity summary generated successfully</response>
        /// <response code="400">Invalid days period parameter</response>
        /// <response code="500">Internal server error during generation</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/activity-summary?daysPeriod=30
        /// </example>
        [HttpGet("projects/{projectId}/activity-summary")]
        [ProducesResponseType(typeof(GitHubActivitySummaryBaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectActivitySummary(Guid projectId, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                var summary = await _gitHubService.GetProjectActivitySummaryAsync(projectId, daysPeriod);
                
                var response = new GitHubActivitySummaryBaseDto
                {
                    ProjectId = projectId,
                    DaysPeriod = daysPeriod,
                    Summary = summary
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project activity summary for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving project activity summary" });
            }
        }

        /// <summary>
        /// Gets personalized GitHub analytics insights based on a user's specific query
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="userQuery">The user's specific query for personalized insights</param>
        /// <param name="daysPeriod">Number of days to analyze (1-365, default: 30)</param>
        /// <returns>AI-generated personalized insights based on the user query</returns>
        /// <response code="200">Personalized insights generated successfully</response>
        /// <response code="400">Invalid query or days period parameter</response>
        /// <response code="500">Internal server error during generation</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/insights?userQuery=What are the most active contributors?&amp;daysPeriod=30
        /// </example>
        [HttpGet("projects/{projectId}/insights")]
        [ProducesResponseType(typeof(GitHubActivitySummaryBaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPersonalizedInsights(Guid projectId, [FromQuery] string userQuery, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userQuery))
                {
                    return BadRequest(new ErrorResponseDto { Error = "User query is required" });
                }

                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                var insights = await _gitHubService.GetPersonalizedAnalyticsInsightsAsync(projectId, userQuery, daysPeriod);
                
                var response = new GitHubActivitySummaryBaseDto
                {
                    ProjectId = projectId,
                    DaysPeriod = daysPeriod,
                    Summary = insights
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized insights for project {ProjectId} with query: {UserQuery}", projectId, userQuery);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while generating personalized insights" });
            }
        }

        /// <summary>
        /// Manually syncs GitHub data for a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="syncType">Type of sync to perform: 'repository', 'analytics', or 'all' (default: 'all')</param>
        /// <returns>Sync operation result with status and message</returns>
        /// <response code="200">Data synced successfully</response>
        /// <response code="400">Invalid sync type or sync operation failed</response>
        /// <response code="500">Internal server error during sync</response>
        /// <example>
        /// POST /api/github/projects/12345678-1234-1234-1234-123456789012/sync?syncType=all
        /// </example>
        [HttpPost("projects/{projectId}/sync")]
        [ProducesResponseType(typeof(GitHubSyncResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncProjectData(Guid projectId, [FromQuery] string syncType = "all")
        {
            try
            {
                var validSyncTypes = new[] { "repository", "analytics", "all" };
                if (!validSyncTypes.Contains(syncType.ToLower()))
                {
                    return BadRequest(new ErrorResponseDto { Error = "Invalid sync type. Must be 'repository', 'analytics', or 'all'" });
                }

                bool success;
                
                switch (syncType.ToLower())
                {
                    case "repository":
                        success = await _gitHubService.SyncRepositoryDataAsync(projectId);
                        break;
                    case "analytics":
                        success = await _gitHubService.SyncAnalyticsAsync(projectId);
                        break;
                    case "all":
                    default:
                        var repoSuccess = await _gitHubService.SyncRepositoryDataAsync(projectId);
                        var analyticsSuccess = await _gitHubService.SyncAnalyticsAsync(projectId);
                        success = repoSuccess && analyticsSuccess;
                        break;
                }

                if (success)
                {
                    _logger.LogInformation("GitHub data synced successfully for project {ProjectId}", projectId);
                    return Ok(new GitHubSyncResponseDto { Message = "Data synced successfully", SyncType = syncType });
                }
                else
                {
                    return BadRequest(new ErrorResponseDto { Error = "Failed to sync data" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing GitHub data for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while syncing data" });
            }
        }

        /// <summary>
        /// Gets the sync status for a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <returns>Current sync status including progress and any error messages</returns>
        /// <response code="200">Sync status retrieved successfully</response>
        /// <response code="404">No sync history found for this project</response>
        /// <response code="500">Internal server error during retrieval</response>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/sync-status
        /// </example>
        [HttpGet("projects/{projectId}/sync-status")]
        [ProducesResponseType(typeof(SyncStatusResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSyncStatus(Guid projectId)
        {
            try
            {
                var syncStatus = await _gitHubService.GetLastSyncStatusAsync(projectId);
                
                if (syncStatus == null)
                {
                    return NotFound(new ErrorResponseDto { Error = "No sync history found for this project" });
                }

                var response = new SyncStatusResponseDto
                {
                    ProjectId = projectId,
                    SyncStatus = new GitHubSyncStatusDto
                    {
                        SyncType = syncStatus.SyncType,
                        Status = syncStatus.Status,
                        StartedAtUtc = syncStatus.StartedAtUtc,
                        CompletedAtUtc = syncStatus.CompletedAtUtc,
                        ItemsProcessed = syncStatus.ItemsProcessed ?? 0,
                        TotalItems = syncStatus.TotalItems ?? 0,
                        ErrorMessage = syncStatus.ErrorMessage
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync status for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving sync status" });
            }
        }

        /// <summary>
        /// Validates a GitHub repository URL
        /// </summary>
        /// <param name="request">Repository URL validation request</param>
        /// <returns>Validation result indicating if the URL is valid and accessible</returns>
        /// <response code="200">URL validation completed successfully</response>
        /// <response code="400">Repository URL is required</response>
        /// <response code="500">Internal server error during validation</response>
        /// <example>
        /// POST /api/github/validate-url
        /// {
        ///   "repositoryUrl": "https://github.com/owner/repository"
        /// }
        /// </example>
        [HttpPost("validate-url")]
        [ProducesResponseType(typeof(UrlValidationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ValidateRepositoryUrl([FromBody] GitHubUrlValidationDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RepositoryUrl))
                {
                    return BadRequest(new ErrorResponseDto { Error = "Repository URL is required" });
                }

                var isValid = await _gitHubService.ValidateRepositoryUrlAsync(request.RepositoryUrl);
                
                var response = new UrlValidationResponseDto
                {
                    RepositoryUrl = request.RepositoryUrl,
                    IsValid = isValid,
                    Message = isValid ? "Repository URL is valid and accessible" : "Repository URL is invalid or inaccessible"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating repository URL: {RepositoryUrl}", request.RepositoryUrl);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while validating the URL" });
            }
        }

        /// <summary>
        /// Gets comprehensive GitHub project overview including repository, stats, analytics, contributions, and activity summary
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="daysPeriod">Number of days to analyze (1-365, default: 30)</param>
        /// <returns>Comprehensive project overview with all GitHub-related data aggregated</returns>
        /// <response code="200">Project overview generated successfully</response>
        /// <response code="400">Invalid days period parameter</response>
        /// <response code="404">No GitHub repository linked to this project</response>
        /// <response code="500">Internal server error during generation</response>
        /// <remarks>
        /// This endpoint provides a comprehensive overview by fetching data from multiple sources in parallel:
        /// - Repository information and basic stats
        /// - Detailed repository statistics
        /// - Project analytics and metrics
        /// - User contributions and activity
        /// - AI-generated activity summary
        /// - Current sync status
        /// 
        /// All data is fetched concurrently for optimal performance.
        /// </remarks>
        /// <example>
        /// GET /api/github/projects/12345678-1234-1234-1234-123456789012/overview?daysPeriod=30
        /// </example>
        [HttpGet("projects/{projectId}/overview")]
        [ProducesResponseType(typeof(GitHubProjectOverviewResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectOverview(Guid projectId, [FromQuery] int daysPeriod = 30)
        {
            _logger.LogDebug("GetProjectOverview called for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
            
            try
            {
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    _logger.LogWarning("Invalid daysPeriod {DaysPeriod} for project {ProjectId}", daysPeriod, projectId);
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                // Get data sequentially to avoid DbContext concurrency issues
                var repository = await _gitHubService.GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    _logger.LogWarning("No repository found for project {ProjectId}", projectId);
                    return NotFound(new ErrorResponseDto { Error = "No GitHub repository linked to this project" });
                }

                var stats = await _gitHubService.GetRepositoryStatsAsync(projectId);
                var analytics = await _gitHubService.GetProjectAnalyticsAsync(projectId, daysPeriod);
                var contributions = await _gitHubService.ListProjectMembersContributionsAsync(projectId, daysPeriod, true);
                var activitySummary = await _gitHubService.GetProjectActivitySummaryAsync(projectId, daysPeriod);
                var syncStatus = await _gitHubService.GetLastSyncStatusAsync(projectId);

                _logger.LogDebug("Overview data retrieved - Contributions count: {ContributionsCount}", contributions.Count);

                // Create comprehensive overview response
                var response = new GitHubProjectOverviewResponseDto
                {
                    ProjectId = projectId,
                    DaysPeriod = daysPeriod,
                    GeneratedAt = DateTime.UtcNow,
                    
                    // Repository information
                    Repository = new GitHubRepositoryOverviewDto
                    {
                        RepositoryId = repository.RepositoryId,
                        RepositoryName = repository.RepositoryName,
                        OwnerName = repository.OwnerName,
                        FullName = repository.FullName,
                        Description = repository.Description,
                        IsPublic = repository.IsPublic,
                        PrimaryLanguage = repository.PrimaryLanguage,
                        Languages = repository.Languages,
                        StarsCount = repository.StarsCount ?? 0,
                        ForksCount = repository.ForksCount ?? 0,
                        OpenIssuesCount = repository.OpenIssuesCount ?? 0,
                        OpenPullRequestsCount = repository.OpenPullRequestsCount ?? 0,
                        DefaultBranch = repository.DefaultBranch,
                        LastActivityAtUtc = repository.LastActivityAtUtc,
                        LastSyncedAtUtc = repository.LastSyncedAtUtc,
                        Branches = repository.Branches ?? new List<string>()
                    },

                    // Repository statistics
                    Stats = stats != null ? new GitHubRepositoryStatsOverviewDto
                    {
                        StarsCount = stats.StarsCount ?? 0,
                        ForksCount = stats.ForksCount ?? 0,
                        IssueCount = stats.IssueCount,
                        PullRequestCount = stats.PullRequestCount,
                        BranchCount = stats.BranchCount,
                        ReleaseCount = stats.ReleaseCount,
                        Contributors = stats.Contributors,
                        TopContributors = stats.TopContributors?.Select(tc => new TopContributorOverviewDto
                        {
                            GitHubUsername = tc.GitHubUsername,
                            TotalCommits = tc.TotalCommits,
                            TotalLinesChanged = tc.TotalLinesChanged,
                            UniqueDaysWithCommits = tc.UniqueDaysWithCommits
                        }).ToList() ?? new List<TopContributorOverviewDto>()
                    } : null,

                    // Analytics data
                    Analytics = analytics != null ? new GitHubAnalyticsOverviewDto
                    {
                        CalculatedAt = analytics.CalculatedAtUtc,
                        TotalCommits = analytics.TotalCommits,
                        TotalAdditions = analytics.TotalAdditions,
                        TotalDeletions = analytics.TotalDeletions,
                        TotalLinesChanged = analytics.TotalLinesChanged,
                        TotalIssues = analytics.TotalIssues,
                        OpenIssues = analytics.OpenIssues,
                        ClosedIssues = analytics.ClosedIssues,
                        TotalPullRequests = analytics.TotalPullRequests,
                        OpenPullRequests = analytics.OpenPullRequests,
                        MergedPullRequests = analytics.MergedPullRequests,
                        ClosedPullRequests = analytics.ClosedPullRequests,
                        ActiveContributors = analytics.ActiveContributors,
                        TotalContributors = analytics.TotalContributors,
                        FirstCommitDate = analytics.FirstCommitDate,
                        LastCommitDate = analytics.LastCommitDate,
                        TotalStars = analytics.TotalStars,
                        TotalForks = analytics.TotalForks,
                        LanguageStats = analytics.LanguageStats,
                        WeeklyCommits = analytics.WeeklyCommits,
                        MonthlyCommits = analytics.MonthlyCommits
                    } : null,

                    // User contributions
                    Contributions = contributions.Select(c => new GitHubContributionOverviewDto
                    {
                        UserId = c.UserId,
                        GitHubUsername = c.GitHubUsername,
                        TotalCommits = c.TotalCommits,
                        TotalLinesChanged = c.TotalLinesChanged,
                        TotalIssuesCreated = c.TotalIssuesCreated,
                        TotalPullRequestsCreated = c.TotalPullRequestsCreated,
                        TotalReviews = c.TotalReviews,
                        UniqueDaysWithCommits = c.UniqueDaysWithCommits,
                        FilesModified = c.FilesModified,
                        LanguagesContributed = c.LanguagesContributed,
                        LongestStreak = c.LongestStreak,
                        CurrentStreak = c.CurrentStreak,
                        CalculatedAtUtc = c.CalculatedAtUtc
                    }).ToList(),

                    // AI-generated activity summary
                    ActivitySummary = activitySummary,

                    // Sync status
                    SyncStatus = syncStatus != null ? new GitHubSyncStatusOverviewDto
                    {
                        SyncType = syncStatus.SyncType,
                        Status = syncStatus.Status,
                        StartedAtUtc = syncStatus.StartedAtUtc,
                        CompletedAtUtc = syncStatus.CompletedAtUtc,
                        ItemsProcessed = syncStatus.ItemsProcessed ?? 0,
                        TotalItems = syncStatus.TotalItems ?? 0,
                        ErrorMessage = syncStatus.ErrorMessage
                    } : null
                };

                _logger.LogInformation("Successfully generated comprehensive overview for project {ProjectId} with {ContributionsCount} contributions", 
                    projectId, contributions.Count);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project overview for project {ProjectId} with daysPeriod {DaysPeriod}", projectId, daysPeriod);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving project overview" });
            }
        }

        /// <summary>
        /// Resolves a GitHub repository (owner/name) to the linked projectId, if any
        /// </summary>
        /// <param name="owner">The GitHub repository owner (username or organization)</param>
        /// <param name="name">The GitHub repository name</param>
        /// <returns>Project ID if the repository is linked to a project</returns>
        /// <response code="200">Repository link resolved successfully</response>
        /// <response code="404">Repository is not linked to any project</response>
        /// <response code="500">Internal server error during resolution</response>
        /// <example>
        /// GET /api/github/repositories/microsoft/vscode/project
        /// </example>
        [HttpGet("repositories/{owner}/{name}/project")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLinkedProjectByRepository(string owner, string name)
        {
            try
            {
                var projectId = await _gitHubService.GetProjectIdForRepositoryAsync(owner, name);
                if (projectId == null)
                {
                    return NotFound(new ErrorResponseDto { Error = "Repository is not linked to any project" });
                }

                return Ok(new { projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving project for repository {Owner}/{Name}", owner, name);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while resolving the repository link" });
            }
        }
    }
}
