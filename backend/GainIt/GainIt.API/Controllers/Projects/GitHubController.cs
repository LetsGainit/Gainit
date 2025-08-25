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
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
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
        [AllowAnonymous]
        [ProducesResponseType(typeof(GitHubRepositoryLinkResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LinkRepository(Guid projectId, [FromBody] GitHubRepositoryLinkDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RepositoryUrl))
                {
                    return BadRequest(new ErrorResponseDto { Error = "Repository URL is required" });
                }

                var repository = await _gitHubService.LinkRepositoryAsync(projectId, request.RepositoryUrl);
                
                _logger.LogInformation("GitHub repository linked successfully to project {ProjectId}", projectId);
                
                var response = new GitHubRepositoryLinkResponseDto
                {
                    Message = "Repository linked successfully",
                    Repository = new GitHubRepositoryInfoDto
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
                    }
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponseDto { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ErrorResponseDto { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking GitHub repository to project {ProjectId}", projectId);
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
            try
            {
                var repository = await _gitHubService.GetRepositoryAsync(projectId);
                if (repository == null)
                {
                    return NotFound(new ErrorResponseDto { Error = "No repository linked to this project" });
                }

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

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting linked repository for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving the repository" });
            }
        }

        /// <summary>
        /// Unlinks a GitHub repository from a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <returns>Unlink confirmation message</returns>
        /// <response code="200">Repository successfully unlinked from the project</response>
        /// <response code="404">No linked repository found for this project</response>
        /// <response code="500">Internal server error during unlinking process</response>
        [HttpDelete("projects/{projectId}/unlink")]
        [ProducesResponseType(typeof(GitHubMessageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnlinkRepository(Guid projectId)
        {
            try
            {
                var success = await _gitHubService.UnlinkRepositoryAsync(projectId);
                
                if (success)
                {
                    _logger.LogInformation("GitHub repository unlinked successfully from project {ProjectId}", projectId);
                    return Ok(new GitHubMessageResponseDto { Message = "Repository unlinked successfully" });
                }
                else
                {
                    return NotFound(new ErrorResponseDto { Error = "No linked repository found for this project" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking GitHub repository from project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while unlinking the repository" });
            }
        }

        /// <summary>
        /// Gets GitHub analytics for a project with automatic data refresh.
        /// 
        /// Use cases:
        /// • Current project analytics without manual intervention
        /// • Automatic data freshness (refreshes if > 1 day old)
        /// • Real-time analytics for reporting and insights
        /// 
        /// Note: For complete data sync including repository metadata,
        /// use POST /api/github/projects/{projectId}/sync
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="daysPeriod">Number of days to analyze (default: 30, max: 365)</param>
        /// <returns>Project analytics data including commits, issues, and pull requests</returns>
        /// <response code="200">Analytics data retrieved successfully</response>
        /// <response code="404">No analytics data available for this project</response>
        /// <response code="500">Internal server error during analytics retrieval</response>
        [HttpGet("projects/{projectId}/analytics")]
        [ProducesResponseType(typeof(GitHubProjectAnalyticsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectAnalytics(Guid projectId, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                // Validate days period
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                var analytics = await _gitHubService.GetProjectAnalyticsAsync(projectId, daysPeriod);
                
                if (analytics == null)
                {
                    return NotFound(new ErrorResponseDto { Error = "No analytics data available for this project" });
                }

                var response = new GitHubProjectAnalyticsResponseDto
                {
                    ProjectId = projectId,
                    DaysPeriod = daysPeriod,
                    CalculatedAt = analytics.CalculatedAtUtc,
                    Analytics = analytics
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project analytics for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving analytics" });
            }
        }

        /// <summary>
        /// Gets user contributions for a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="daysPeriod">Number of days to analyze (default: 30, max: 365)</param>
        /// <returns>List of user contributions with detailed metrics</returns>
        /// <response code="200">User contributions retrieved successfully</response>
        /// <response code="500">Internal server error during contributions retrieval</response>
        [HttpGet("projects/{projectId}/contributions")]
        [ProducesResponseType(typeof(GitHubUserContributionsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserContributions(Guid projectId, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                // Validate days period
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                var contributions = await _gitHubService.GetUserContributionsAsync(projectId, daysPeriod);
                
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contributions for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving contributions" });
            }
        }

        /// <summary>
        /// Gets contribution analytics for a specific user in a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="daysPeriod">Number of days to analyze (default: 30, max: 365)</param>
        /// <returns>Detailed contribution analytics for the specified user</returns>
        /// <response code="200">User contribution data retrieved successfully</response>
        /// <response code="404">No contribution data available for this user</response>
        /// <response code="500">Internal server error during contribution retrieval</response>
        [HttpGet("projects/{projectId}/contributions/{userId}")]
        [ProducesResponseType(typeof(GitHubUserContributionDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserContribution(Guid projectId, Guid userId, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                // Validate days period
                if (daysPeriod <= 0 || daysPeriod > 365)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Days period must be between 1 and 365" });
                }

                var contribution = await _gitHubService.GetUserContributionAsync(projectId, userId, daysPeriod);
                
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
                        CalculatedAtUtc = contribution.CalculatedAtUtc
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
        /// Gets repository statistics for a project
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <returns>Repository statistics including stars, forks, issues, and pull requests</returns>
        /// <response code="200">Repository statistics retrieved successfully</response>
        /// <response code="404">Repository statistics not available</response>
        /// <response code="500">Internal server error during statistics retrieval</response>
        [HttpGet("projects/{projectId}/stats")]
        [ProducesResponseType(typeof(GitHubRepositoryStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRepositoryStats(Guid projectId)
        {
            try
            {
                var stats = await _gitHubService.GetRepositoryStatsAsync(projectId);
                if (stats == null)
                {
                    return NotFound(new ErrorResponseDto { Error = "Repository stats not found" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository stats for project {ProjectId}", projectId);
                return StatusCode(500, new ErrorResponseDto { Error = "An error occurred while retrieving repository stats" });
            }
        }

        /// <summary>
        /// Gets user activity summary for ChatGPT context
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="daysPeriod">Number of days to analyze (default: 30, max: 365)</param>
        /// <returns>Formatted activity summary suitable for AI context</returns>
        /// <response code="200">User activity summary retrieved successfully</response>
        /// <response code="500">Internal server error during summary retrieval</response>
        [HttpGet("projects/{projectId}/users/{userId}/activity-summary")]
        [ProducesResponseType(typeof(GitHubUserActivitySummaryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserActivitySummary(Guid projectId, Guid userId, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                // Validate days period
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
        /// Gets project activity summary for ChatGPT context
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="daysPeriod">Number of days to analyze (default: 30, max: 365)</param>
        /// <returns>Formatted project activity summary suitable for AI context</returns>
        /// <response code="200">Project activity summary retrieved successfully</response>
        /// <response code="500">Internal server error during summary retrieval</response>
        [HttpGet("projects/{projectId}/activity-summary")]
        [ProducesResponseType(typeof(GitHubActivitySummaryBaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectActivitySummary(Guid projectId, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                // Validate days period
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
        /// <param name="userQuery">The user's specific question or area of interest</param>
        /// <param name="daysPeriod">Number of days to analyze (default: 30, max: 365)</param>
        /// <returns>AI-powered insights tailored to the user's query</returns>
        /// <response code="200">Personalized insights retrieved successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="500">Internal server error during insights generation</response>
        [HttpGet("projects/{projectId}/insights")]
        [ProducesResponseType(typeof(GitHubActivitySummaryBaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPersonalizedInsights(Guid projectId, [FromQuery] string userQuery, [FromQuery] int daysPeriod = 30)
        {
            try
            {
                // Validate user query
                if (string.IsNullOrWhiteSpace(userQuery))
                {
                    return BadRequest(new ErrorResponseDto { Error = "User query is required" });
                }

                // Validate days period
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
        /// Manually syncs GitHub data for a project.
        /// 
        /// Use cases:
        /// • Immediate data refresh for time-sensitive operations
        /// • Complete repository metadata updates (stars, forks, description)
        /// • Bulk data synchronization for reporting
        /// • Recovery from data inconsistencies
        /// 
        /// Note: Analytics automatically refresh when > 1 day old.
        /// This manual sync ensures both metadata and analytics are current.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <param name="syncType">Type of sync: 'repository' (metadata), 'analytics' (contributions), or 'all' (complete, default)</param>
        /// <returns>Sync operation result and status</returns>
        /// <response code="200">Data synced successfully</response>
        /// <response code="400">Sync operation failed or invalid sync type</response>
        /// <response code="500">Internal server error during sync operation</response>
        [HttpPost("projects/{projectId}/sync")]
        [ProducesResponseType(typeof(GitHubSyncResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncProjectData(Guid projectId, [FromQuery] string syncType = "all")
        {
            try
            {
                // Validate sync type
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
                        // Sync both repository data and analytics
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
        /// Gets the sync status for a project to monitor synchronization operations.
        /// 
        /// Use cases:
        /// • Monitor sync operation progress and completion
        /// • Debug failed synchronization attempts
        /// • Track data freshness and last update times
        /// • Verify sync operation results
        /// </summary>
        /// <param name="projectId">The unique identifier of the project</param>
        /// <returns>Last sync operation status and details</returns>
        /// <response code="200">Sync status retrieved successfully</response>
        /// <response code="404">No sync history found for this project</response>
        /// <response code="500">Internal server error during status retrieval</response>
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
        /// <returns>Validation result indicating if the repository is accessible</returns>
        /// <response code="200">Repository URL validation completed</response>
        /// <response code="400">Invalid repository URL format</response>
        /// <response code="500">Internal server error during validation</response>
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
    }
}
