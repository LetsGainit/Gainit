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
    /// Uses GitHub REST API for public repository access without authentication requirements
    /// </summary>
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
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="request">Repository link request containing the GitHub repository URL</param>
        /// <returns>Repository information and link confirmation</returns>
        /// <response code="200">Repository successfully linked to the project</response>
        /// <response code="400">Invalid repository URL or request data</response>
        /// <response code="409">Repository already linked or conflict occurred</response>
        /// <response code="500">Internal server error during linking process</response>
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
        [HttpGet("projects/{projectId}/repository")]
        [ProducesResponseType(typeof(GitHubRepositoryInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
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
        [HttpGet("projects/{projectId}/stats")]
        [ProducesResponseType(typeof(GitHubRepositoryStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
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
        [HttpGet("projects/{projectId}/analytics")]
        [ProducesResponseType(typeof(GitHubProjectAnalyticsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
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
        [HttpGet("projects/{projectId}/contributions")]
        [ProducesResponseType(typeof(GitHubUserContributionsResponseDto), StatusCodes.Status200OK)]
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

                _logger.LogDebug("Calling _gitHubService.GetUserContributionsAsync for project {ProjectId} with daysPeriod {DaysPeriod}, force={Force}", projectId, daysPeriod, force);
                var contributions = await _gitHubService.GetUserContributionsAsync(projectId, daysPeriod, force);
                
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
        [HttpGet("projects/{projectId}/users/{userId}/contributions")]
        [ProducesResponseType(typeof(GitHubUserContributionDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserContribution(Guid projectId, Guid userId, [FromQuery] int daysPeriod = 30, [FromQuery] bool force = false)
        {
            try
            {
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                var contribution = await _gitHubService.GetUserContributionAsync(projectId, userId, daysPeriod, force);
                
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
        [HttpGet("projects/{projectId}/users/{userId}/activity")]
        [ProducesResponseType(typeof(GitHubUserActivitySummaryResponseDto), StatusCodes.Status200OK)]
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
        [HttpGet("projects/{projectId}/activity-summary")]
        [ProducesResponseType(typeof(GitHubActivitySummaryBaseDto), StatusCodes.Status200OK)]
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
        /// Resolves a GitHub repository (owner/name) to the linked projectId, if any
        /// </summary>
        [HttpGet("repositories/{owner}/{name}/project")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
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
