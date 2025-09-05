using GainIt.API.DTOs.Requests.Users;
using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Models.Users.Expertise;
using GainIt.API.Models.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Services.Users.Interfaces;
using GainIt.API.Services.FileUpload.Interfaces;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using GainIt.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GainIt.API.Controllers.Users
{
    /// <summary>
    /// Controller for managing user profiles including Gainers, Mentors, and Nonprofit Organizations.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserProfileService r_userProfileService;
        private readonly ILogger<UsersController> r_logger;
        private readonly IUserSummaryService r_userSummaryService;
        private readonly GainItDbContext r_DbContext;
        private readonly IFileUploadService r_FileUploadService;

        public UsersController(IUserProfileService i_userProfileService, ILogger<UsersController> i_logger, GainItDbContext i_DbContext, IFileUploadService i_FileUploadService, IUserSummaryService i_userSummaryService)
        {
            r_userProfileService = i_userProfileService;
            r_logger = i_logger;
            r_DbContext = i_DbContext;
            r_FileUploadService = i_FileUploadService;
            r_userSummaryService = i_userSummaryService;
        }
        private static string? tryGetClaim(ClaimsPrincipal user, params string[] types)
        {
            foreach (var t in types)
            {
                var v = user.FindFirstValue(t);
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            return null;
        }



            /// <summary>
            /// Ensure a local user exists for the current external identity (OID).
            /// Builds the identity DTO from the access-token claims (server-side),
            /// then creates/updates the user and returns a minimal profile.
            /// </summary>
            /// <returns>User profile information after provisioning</returns>
            [HttpPost("me/ensure")]
            [Authorize(Policy = "RequireAccessAsUser")]
            [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<UserProfileDto>> ProvisionCurrentUser()
            {
                var correlationId = HttpContext.TraceIdentifier;
                var startTime = DateTimeOffset.UtcNow;
                
                r_logger.LogInformation("Starting user provisioning process. CorrelationId={CorrelationId}, UserAgent={UserAgent}, RemoteIP={RemoteIP}, AuthenticatedUser={AuthenticatedUser}", 
                    correlationId, Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress, User.Identity?.Name);
                
                // Log all available claims for security monitoring
                var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                r_logger.LogDebug("All available claims for user provisioning: CorrelationId={CorrelationId}, Claims={Claims}", 
                    correlationId, string.Join(", ", allClaims));
                
                try
                {
                    
                    var externalId =
                        tryGetClaim(User, "oid", ClaimTypes.NameIdentifier)
                    ?? tryGetClaim(User, "sub")
                    ?? tryGetClaim(User, ClaimTypes.NameIdentifier)
                    ?? tryGetClaim(User, "http://schemas.microsoft.com/identity/claims/objectidentifier")
                    ?? tryGetClaim(User, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                    if (string.IsNullOrEmpty(externalId))
                    {
                        return Unauthorized(new { Message = "Missing external identity claim (oid/sub)" });
                    }

                    r_logger.LogDebug("Extracted subject. CorrelationId={CorrelationId}, Subject={Subject}", correlationId, externalId);

                    // Use preferred_username directly since it contains the real Google email
                    var email = tryGetClaim(User, "preferred_username");

                    // Prefer display name if present, then compose from given + surname, then fallback
                    var displayName = tryGetClaim(User, "name", "displayName");
                    var given = tryGetClaim(User, "given_name");
                    var surname = tryGetClaim(User, "family_name", "surname");
                    var composed = string.Join(' ', new[] { given, surname }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    var name = !string.IsNullOrWhiteSpace(displayName) ? displayName
                            : (!string.IsNullOrWhiteSpace(composed) ? composed
                            : (tryGetClaim(User, "preferred_username") ?? email));

                    var idp = tryGetClaim(User, "idp");

                    r_logger.LogDebug("Extracted user claims. CorrelationId={CorrelationId}, Email={Email}, Name={Name}, IdentityProvider={IdP}", 
                        correlationId, email, name, idp);
                    
                    // Debug logging to see all available claims - this will help us find the correct claim for Google email
                    r_logger.LogInformation("ALL AVAILABLE CLAIMS: CorrelationId={CorrelationId}, Claims={Claims}", 
                        correlationId, string.Join(" | ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
    
                    var dto = new ExternalUserDto
                    {
                        ExternalId = externalId!,
                        Email = email,
                        FullName = string.IsNullOrWhiteSpace(name) ? null : name,
                        IdentityProvider = idp,
                    };

                    r_logger.LogDebug("Created ExternalUserDto for provisioning. CorrelationId={CorrelationId}, ExternalId={ExternalId}, Email={Email}, FullName={FullName}", 
                        correlationId, dto.ExternalId, dto.Email, dto.FullName);

                    var profile = await r_userProfileService.GetOrCreateFromExternalAsync(dto);
                    
                    var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
                    r_logger.LogInformation("Successfully provisioned user. CorrelationId={CorrelationId}, UserId={UserId}, ExternalId={ExternalId}, Email={Email}, ProcessingTime={ProcessingTime}ms, RemoteIP={RemoteIP}", 
                        correlationId, profile.UserId, profile.ExternalId, profile.EmailAddress, processingTime, HttpContext.Connection.RemoteIpAddress);
                    
                    return Ok(profile);
                }
                catch (Exception ex)
                {
                    var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
                    r_logger.LogError(ex, "Error during user provisioning process. CorrelationId={CorrelationId}, ProcessingTime={ProcessingTime}ms, OID: {OID}, Available claims: {ClaimTypes}, RemoteIP={RemoteIP}", 
                        correlationId, processingTime, tryGetClaim(User, "oid", ClaimTypes.NameIdentifier),
                        string.Join(", ", allClaims), HttpContext.Connection.RemoteIpAddress);
                    return StatusCode(500, new { Message = "An error occurred during user provisioning" });
                }
            }

            /// <summary>
            /// Retrieves the current authenticated user's profile.
            /// Uses JWT claims to identify the user without requiring their ID.
            /// </summary>
            /// <returns>User profile information for the authenticated user</returns>
            [HttpGet("me")]
            [Authorize(Policy = "RequireAccessAsUser")]
            [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
            {
                var correlationId = HttpContext.TraceIdentifier;
                var startTime = DateTimeOffset.UtcNow;
                
                r_logger.LogInformation("Getting current user profile. CorrelationId={CorrelationId}, UserAgent={UserAgent}, RemoteIP={RemoteIP}, AuthenticatedUser={AuthenticatedUser}", 
                    correlationId, Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress, User.Identity?.Name);
                
                try
                {
                    // Debug: Log all available claims
                    var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                    r_logger.LogDebug("All available claims for GetCurrentUser: CorrelationId={CorrelationId}, Claims={Claims}", 
                        correlationId, string.Join(", ", allClaims));

                    // Use the same robust claim extraction as provisioning endpoint
                    var externalId =
                        tryGetClaim(User, "oid", ClaimTypes.NameIdentifier)
                    ?? tryGetClaim(User, "sub")
                    ?? tryGetClaim(User, ClaimTypes.NameIdentifier)
                    ?? tryGetClaim(User, "http://schemas.microsoft.com/identity/claims/objectidentifier")
                    ?? tryGetClaim(User, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                    

                    if (string.IsNullOrEmpty(externalId))
                    {
                        r_logger.LogWarning("Missing external identity claim (oid/sub) for current user. CorrelationId={CorrelationId}", correlationId);
                        return Unauthorized(new { Message = "Missing external identity claim (oid/sub)" });
                    }

                    r_logger.LogDebug("Extracted external ID for current user. CorrelationId={CorrelationId}, ExternalId={ExternalId}", correlationId, externalId);

                    // Find the user in the database by external ID (EXACTLY like ForumController)
                    var user = await r_DbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.ExternalId == externalId);

                    if (user == null)
                    {
                        r_logger.LogWarning("Current user not found in database. CorrelationId={CorrelationId}, ExternalId={ExternalId}", correlationId, externalId);
                        return NotFound(new { Message = "User profile not found. Please ensure your profile is created first." });
                    }

                    // Check if user has completed their profile
                    var hasCompletedProfile = await r_userProfileService.CheckIfUserHasCompletedProfileAsync(user.UserId);

                    // Create UserProfileDto from the user entity
                    var profileDto = new UserProfileDto
                    {
                        UserId = user.UserId,
                        ExternalId = user.ExternalId,
                        EmailAddress = user.EmailAddress,
                        FullName = user.FullName,
                        Country = user.Country,
                        GitHubUsername = user.GitHubUsername,
                        IsNewUser = !hasCompletedProfile
                    };

                    var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
                    r_logger.LogInformation("Successfully retrieved current user profile. CorrelationId={CorrelationId}, UserId={UserId}, ExternalId={ExternalId}, ProcessingTime={ProcessingTime}ms, RemoteIP={RemoteIP}", 
                        correlationId, profileDto.UserId, profileDto.ExternalId, processingTime, HttpContext.Connection.RemoteIpAddress);
                    
                    return Ok(profileDto);
            }
            catch (Exception ex)
            {
                var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
                r_logger.LogError(ex, "Error retrieving current user profile. CorrelationId={CorrelationId}, ProcessingTime={ProcessingTime}ms, RemoteIP={RemoteIP}", 
                    correlationId, processingTime, HttpContext.Connection.RemoteIpAddress);
                return StatusCode(500, new { Message = "An error occurred while retrieving the user profile" });
            }
        }

        /// <summary>
        /// Retrieves an AI-generated summary of the user's platform activity and GitHub contributions.
        /// Can be used to get summary for current authenticated user or any specific user by providing userId.
        /// </summary>
        /// <param name="userId">Optional user ID. If not provided, uses the current authenticated user.</param>
        /// <returns>AI-generated summary of user's platform activity</returns>
        [HttpGet("me/summary")]
        [Authorize(Policy = "RequireAccessAsUser")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMySummary([FromQuery] Guid? userId = null)
        {
            var correlationId = HttpContext.TraceIdentifier;
            var startTime = DateTimeOffset.UtcNow;
            
            r_logger.LogInformation("Getting user summary. CorrelationId={CorrelationId}, UserAgent={UserAgent}, RemoteIP={RemoteIP}, AuthenticatedUser={AuthenticatedUser}", 
                correlationId, Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress, User.Identity?.Name);

            try
            {
                User? user;
                
                if (userId.HasValue)
                {
                    // Use the provided userId
                    user = await r_DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId.Value);
                    if (user == null)
                    {
                        r_logger.LogWarning("User not found for summary request with provided userId. CorrelationId={CorrelationId}, UserId={UserId}", correlationId, userId.Value);
                        return NotFound(new { Message = "User not found" });
                    }
                    r_logger.LogInformation("Getting summary for provided userId. CorrelationId={CorrelationId}, UserId={UserId}", correlationId, userId.Value);
                }
                else
                {
                    // Use current authenticated user
                    var externalId = tryGetClaim(User, "oid", ClaimTypes.NameIdentifier)
                                  ?? tryGetClaim(User, "sub")
                                  ?? tryGetClaim(User, ClaimTypes.NameIdentifier)
                                  ?? tryGetClaim(User, "http://schemas.microsoft.com/identity/claims/objectidentifier")
                                  ?? tryGetClaim(User, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                    if (string.IsNullOrEmpty(externalId))
                    {
                        r_logger.LogWarning("Missing external identity claim for summary request. CorrelationId={CorrelationId}", correlationId);
                        return Unauthorized(new { Message = "Missing external identity claim (oid/sub)" });
                    }

                    user = await r_DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.ExternalId == externalId);
                    if (user == null)
                    {
                        r_logger.LogWarning("User not found for summary request. CorrelationId={CorrelationId}, ExternalId={ExternalId}", correlationId, externalId);
                        return NotFound(new { Message = "User not found" });
                    }
                    r_logger.LogInformation("Getting summary for current authenticated user. CorrelationId={CorrelationId}, UserId={UserId}", correlationId, user.UserId);
                }

                if (user == null)
                {
                    r_logger.LogError("User is null after retrieval. CorrelationId={CorrelationId}", correlationId);
                    return StatusCode(500, new { Message = "An error occurred while retrieving user information." });
                }

                var summary = await r_userSummaryService.GetUserSummaryAsync(user.UserId);
                
                var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
                r_logger.LogInformation("Successfully retrieved user summary. CorrelationId={CorrelationId}, UserId={UserId}, ProcessingTime={ProcessingTime}ms, SummaryLength={Length}, RemoteIP={RemoteIP}", 
                    correlationId, user.UserId, processingTime, summary.Length, HttpContext.Connection.RemoteIpAddress);
                
                return Ok(new { overallSummary = summary });
            }
            catch (Exception ex)
            {
                var processingTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
                r_logger.LogError(ex, "Error retrieving user summary. CorrelationId={CorrelationId}, ProcessingTime={ProcessingTime}ms, RemoteIP={RemoteIP}", 
                    correlationId, processingTime, HttpContext.Connection.RemoteIpAddress);
                return StatusCode(500, new { Message = "An error occurred while generating the user summary." });
            }
        }





        /// <summary>
        /// Get user dashboard analytics data
        /// Returns structured analytics data including commits timeline, collaboration metrics, completion rates, 
        /// refactoring rates, skill distribution, collaboration activity, achievements, and community engagement.
        /// </summary>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>Dashboard analytics data with raw counts and calculated percentages</returns>
        [HttpGet("{userId}/dashboard")]
        [Authorize(Policy = "RequireAccessAsUser")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserDashboardData(Guid userId)
        {
            try
            {
                var dashboard = await r_userSummaryService.GetUserDashboardAsync(userId);
                return Ok(dashboard);
            }
            catch (KeyNotFoundException)
            {
                r_logger.LogWarning("User not found for dashboard: UserId={UserId}", userId);
                return NotFound(new { Message = "User not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving dashboard data for UserId={UserId}", userId);
                return StatusCode(500, new { Message = "An error occurred while retrieving the dashboard data." });
            }
        }



        /// <summary>
        /// Retrieves a complete Gainer profile by their ID.
        /// </summary>
        /// <param name="id">The unique identifier of the Gainer.</param>
        /// <returns>
        /// The complete Gainer profile including personal information, projects, and achievements.
        /// Returns 200 OK if found, 400 Bad Request if ID is invalid, or 404 Not Found if Gainer doesn't exist.
        /// </returns>

        [HttpGet("gainer/{id}/profile")]
        [ProducesResponseType(typeof(FullGainerViewModel), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetGainerProfile(Guid id)
        {
            r_logger.LogInformation("Getting Gainer profile: UserId={UserId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Gainer ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            try
            {
                Gainer gainer = await r_userProfileService.GetGainerByIdAsync(id);
                if (gainer == null)
                {
                    r_logger.LogWarning("Gainer not found: {UserId}", id);
                    return NotFound(new { Message = $"Gainer with ID {id} not found" });
                }

                var projects = await r_userProfileService.GetUserProjectsAsync(id);
                var achievements = (await r_userProfileService.GetUserAchievementsAsync(id)).ToList();
                FullGainerViewModel gainerViewModel = new FullGainerViewModel(gainer, projects, achievements);
                
                r_logger.LogInformation("Successfully retrieved Gainer profile: UserId={UserId}, ProjectsCount={ProjectsCount}, AchievementsCount={AchievementsCount}", 
                    id, projects.Count(), achievements.Count);
                return Ok(gainerViewModel);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Gainer not found: UserId={UserId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Gainer with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Gainer profile: UserId={UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the Gainer profile" });
            }
        }

        /// <summary>
        /// Retrieves a complete Mentor profile by their ID.
        /// </summary>
        /// <param name="id">The unique identifier of the Mentor.</param>
        /// <returns>
        /// The complete Mentor profile including personal information, projects, and achievements.
        /// Returns 200 OK if found, 400 Bad Request if ID is invalid, or 404 Not Found if Mentor doesn't exist.
        /// </returns>
        
        [HttpGet("mentor/{id}/profile")]
        [ProducesResponseType(typeof(FullMentorViewModel), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMentorProfile(Guid id)
        {
            r_logger.LogInformation("Getting Mentor profile: UserId={UserId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Mentor ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            try
            {
                Mentor mentor = await r_userProfileService.GetMentorByIdAsync(id);
                if (mentor == null)
                {
                    r_logger.LogWarning("Mentor not found: {UserId}", id);
                    return NotFound(new { Message = $"Mentor with ID {id} not found" });
                }

                var projects = await r_userProfileService.GetUserProjectsAsync(id);
                var achievements = (await r_userProfileService.GetUserAchievementsAsync(id)).ToList();
                FullMentorViewModel mentorViewModel = new FullMentorViewModel(mentor, projects, achievements, true, true);
                
                r_logger.LogInformation("Successfully retrieved Mentor profile: UserId={UserId}, ProjectsCount={ProjectsCount}, AchievementsCount={AchievementsCount}", 
                    id, projects.Count(), achievements.Count);
                return Ok(mentorViewModel);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Mentor not found: UserId={UserId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Mentor with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Mentor profile: UserId={UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the Mentor profile" });
            }
        }

        /// <summary>
        /// Retrieves a complete Nonprofit Organization profile by their ID.
        /// </summary>
        /// <param name="id">The unique identifier of the Nonprofit Organization.</param>
        /// <returns>
        /// The complete Nonprofit profile including organization information and owned projects.
        /// Returns 200 OK if found, 400 Bad Request if ID is invalid, or 404 Not Found if Nonprofit doesn't exist.
        /// </returns>
    
        [HttpGet("nonprofit/{id}/profile")]
        [ProducesResponseType(typeof(FullNonprofitViewModel), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetNonprofitProfile(Guid id)
        {
            r_logger.LogInformation("Getting Nonprofit profile: NonprofitId={NonprofitId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Nonprofit ID provided: {NonprofitId}", id);
                return BadRequest(new { Message = "Nonprofit ID cannot be empty." });
            }

            try
            {
                NonprofitOrganization nonprofit = await r_userProfileService.GetNonprofitByIdAsync(id);
                if (nonprofit == null)
                {
                    r_logger.LogWarning("Nonprofit not found: {NonprofitId}", id);
                    return NotFound(new { Message = $"Nonprofit with ID {id} not found" });
                }
                
                var projects = await r_userProfileService.GetNonprofitOwnedProjectsAsync(id);
                FullNonprofitViewModel nonprofitViewModel = new FullNonprofitViewModel(nonprofit, projects);
                
                r_logger.LogInformation("Successfully retrieved Nonprofit profile: NonprofitId={NonprofitId}, ProjectsCount={ProjectsCount}", 
                    id, projects.Count());
                return Ok(nonprofitViewModel);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Nonprofit not found: NonprofitId={NonprofitId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Nonprofit with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Nonprofit profile: NonprofitId={NonprofitId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the Nonprofit profile" });
            }
        }

        #region Expertise Management

        /// <summary>
        /// Adds technical expertise to a Gainer user.
        /// </summary>
        /// <param name="id">The unique identifier of the Gainer.</param>
        /// <param name="expertiseDto">The technical expertise to add.</param>
        /// <returns>The updated technical expertise.</returns>
        [HttpPost("gainer/{id}/expertise")]
        [ProducesResponseType(typeof(TechExpertise), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddExpertiseToGainer(Guid id, [FromBody] AddTechExpertiseDto expertiseDto)
        {
            r_logger.LogInformation("Adding expertise to Gainer: UserId={UserId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Gainer ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            if (expertiseDto == null)
            {
                r_logger.LogWarning("Expertise data is null for Gainer: UserId={UserId}", id);
                return BadRequest(new { Message = "Expertise data cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                r_logger.LogWarning("Invalid expertise data for Gainer: UserId={UserId}, ModelStateErrors={ModelStateErrors}", id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            try
            {
                var result = await r_userProfileService.AddExpertiseToGainerAsync(id, expertiseDto);
                r_logger.LogInformation("Successfully added expertise to Gainer: UserId={UserId}, ExpertiseId={ExpertiseId}", id, result.ExpertiseId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Gainer not found: UserId={UserId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Gainer with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding expertise to Gainer: UserId={UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while adding expertise to the Gainer" });
            }
        }

        /// <summary>
        /// Adds technical expertise to a Mentor user.
        /// </summary>
        /// <param name="id">The unique identifier of the Mentor.</param>
        /// <param name="expertiseDto">The technical expertise to add.</param>
        /// <returns>The updated technical expertise.</returns>
        [HttpPost("mentor/{id}/expertise")]
        [ProducesResponseType(typeof(TechExpertise), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddExpertiseToMentor(Guid id, [FromBody] AddTechExpertiseDto expertiseDto)
        {
            r_logger.LogInformation("Adding expertise to Mentor: UserId={UserId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Mentor ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            if (expertiseDto == null)
            {
                r_logger.LogWarning("Expertise data is null for Mentor: UserId={UserId}", id);
                return BadRequest(new { Message = "Expertise data cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                r_logger.LogWarning("Invalid expertise data for Mentor: UserId={UserId}, ModelStateErrors={ModelStateErrors}", id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            try
            {
                var result = await r_userProfileService.AddExpertiseToMentorAsync(id, expertiseDto);
                r_logger.LogInformation("Successfully added expertise to Mentor: UserId={UserId}, ExpertiseId={ExpertiseId}", id, result.ExpertiseId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Mentor not found: UserId={UserId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Mentor with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding expertise to Mentor: UserId={UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while adding expertise to the Mentor" });
            }
        }

        /// <summary>
        /// Adds nonprofit expertise to a Nonprofit Organization.
        /// </summary>
        /// <param name="id">The unique identifier of the Nonprofit Organization.</param>
        /// <param name="expertiseDto">The nonprofit expertise to add.</param>
        /// <returns>The updated nonprofit expertise.</returns>
        [HttpPost("nonprofit/{id}/expertise")]
        [ProducesResponseType(typeof(NonprofitExpertise), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddExpertiseToNonprofit(Guid id, [FromBody] AddNonprofitExpertiseDto expertiseDto)
        {
            r_logger.LogInformation("Adding expertise to Nonprofit: NonprofitId={NonprofitId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Nonprofit ID provided: {NonprofitId}", id);
                return BadRequest(new { Message = "Nonprofit ID cannot be empty." });
            }

            if (expertiseDto == null)
            {
                r_logger.LogWarning("Expertise data is null for Nonprofit: NonprofitId={NonprofitId}", id);
                return BadRequest(new { Message = "Expertise data cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                r_logger.LogWarning("Invalid expertise data for Nonprofit: NonprofitId={NonprofitId}, ModelStateErrors={ModelStateErrors}", id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            try
            {
                var result = await r_userProfileService.AddExpertiseToNonprofitAsync(id, expertiseDto);
                r_logger.LogInformation("Successfully added expertise to Nonprofit: NonprofitId={NonprofitId}, ExpertiseId={ExpertiseId}", id, result.ExpertiseId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Nonprofit not found: NonprofitId={NonprofitId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Nonprofit with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding expertise to Nonprofit: NonprofitId={NonprofitId}", id);
                return StatusCode(500, new { Message = "An error occurred while adding expertise to the Nonprofit" });
            }
        }

        #endregion

        #region Achievement Management

        /// <summary>
        /// Adds an achievement to a Gainer user.
        /// </summary>
        /// <param name="id">The unique identifier of the Gainer.</param>
        /// <param name="achievementTemplateId">The ID of the achievement template to award.</param>
        /// <returns>The awarded achievement.</returns>
        [HttpPost("gainer/{id}/achievements")]
        [ProducesResponseType(typeof(UserAchievement), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddAchievementToGainer(Guid id, [FromBody] Guid achievementTemplateId)
        {
            r_logger.LogInformation("Adding achievement to Gainer: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", id, achievementTemplateId);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Gainer ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            if (achievementTemplateId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Achievement Template ID provided: {AchievementTemplateId}", achievementTemplateId);
                return BadRequest(new { Message = "Achievement Template ID cannot be empty." });
            }

            try
            {
                var result = await r_userProfileService.AddAchievementToGainerAsync(id, achievementTemplateId);
                r_logger.LogInformation("Successfully added achievement to Gainer: UserId={UserId}, AchievementId={AchievementId}", id, result.Id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                
                r_logger.LogWarning("Gainer or Achievement Template not found: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}, Error={Error}", id, achievementTemplateId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding achievement to Gainer: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", id, achievementTemplateId);
                return StatusCode(500, new { Message = "An error occurred while adding achievement to the Gainer" });
            }
        }

        /// <summary>
        /// Adds an achievement to a Mentor user.
        /// </summary>
        /// <param name="id">The unique identifier of the Mentor.</param>
        /// <param name="achievementTemplateId">The ID of the achievement template to award.</param>
        /// <returns>The awarded achievement.</returns>
        [HttpPost("mentor/{id}/achievements")]
        [ProducesResponseType(typeof(UserAchievement), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddAchievementToMentor(Guid id, [FromBody] Guid achievementTemplateId)
        {
            r_logger.LogInformation("Adding achievement to Mentor: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", id, achievementTemplateId);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Mentor ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            if (achievementTemplateId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Achievement Template ID provided: {AchievementTemplateId}", achievementTemplateId);
                return BadRequest(new { Message = "Achievement Template ID cannot be empty." });
            }

            try
            {
                var result = await r_userProfileService.AddAchievementToMentorAsync(id, achievementTemplateId);
                r_logger.LogInformation("Successfully added achievement to Mentor: UserId={UserId}, AchievementId={AchievementId}", id, result.Id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Mentor or Achievement Template not found: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}, Error={Error}", id, achievementTemplateId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding achievement to Mentor: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", id, achievementTemplateId);
                return StatusCode(500, new { Message = "An error occurred while adding achievement to the Mentor" });
            }
        }

        /// <summary>
        /// Adds an achievement to a Nonprofit Organization.
        /// </summary>
        /// <param name="id">The unique identifier of the Nonprofit Organization.</param>
        /// <param name="achievementTemplateId">The ID of the achievement template to award.</param>
        /// <returns>The awarded achievement.</returns>
        [HttpPost("nonprofit/{id}/achievements")]
        [ProducesResponseType(typeof(UserAchievement), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddAchievementToNonprofit(Guid id, [FromBody] Guid achievementTemplateId)
        {
            r_logger.LogInformation("Adding achievement to Nonprofit: NonprofitId={NonprofitId}, AchievementTemplateId={AchievementTemplateId}", id, achievementTemplateId);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Nonprofit ID provided: {NonprofitId}", id);
                return BadRequest(new { Message = "Nonprofit ID cannot be empty." });
            }

            if (achievementTemplateId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Achievement Template ID provided: {AchievementTemplateId}", achievementTemplateId);
                return BadRequest(new { Message = "Achievement Template ID cannot be empty." });
            }

            try
            {
                var result = await r_userProfileService.AddAchievementToNonprofitAsync(id, achievementTemplateId);
                r_logger.LogInformation("Successfully added achievement to Nonprofit: NonprofitId={NonprofitId}, AchievementId={AchievementId}", id, result.Id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Nonprofit or Achievement Template not found: NonprofitId={NonprofitId}, AchievementTemplateId={AchievementTemplateId}, Error={Error}", id, achievementTemplateId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding achievement to Nonprofit: NonprofitId={NonprofitId}, AchievementTemplateId={AchievementTemplateId}", id, achievementTemplateId);
                return StatusCode(500, new { Message = "An error occurred while adding achievement to the Nonprofit" });
            }
        }

        #endregion

        #region Profile Updates

        /// <summary>
        /// Updates a Gainer profile.
        /// </summary>
        /// <param name="id">The unique identifier of the Gainer.</param>
        /// <param name="updateDto">The profile update data.</param>
        /// <returns>The updated Gainer profile.</returns>
        [HttpPut("gainer/{id}/profile")]
        [ProducesResponseType(typeof(Gainer), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateGainerProfile(Guid id, [FromBody] GainerProfileUpdateDTO updateDto)
        {
            r_logger.LogInformation("Updating Gainer profile: UserId={UserId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Gainer ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            if (updateDto == null)
            {
                r_logger.LogWarning("Update data is null for Gainer: UserId={UserId}", id);
                return BadRequest(new { Message = "Update data cannot be null." });
            }

            try
            {
                // Use the new create-or-update method
                var result = await r_userProfileService.CreateOrUpdateGainerProfileAsync(id, updateDto);
                
                // Check if any expertise strings were provided and add expertise
                if (updateDto.ProgrammingLanguages?.Any() == true || updateDto.Technologies?.Any() == true || updateDto.Tools?.Any() == true)
                {
                    r_logger.LogInformation("Adding expertise to Gainer during profile update: UserId={UserId}", id);
                    var expertiseDto = new AddTechExpertiseDto
                    {
                        ProgrammingLanguages = updateDto.ProgrammingLanguages ?? new List<string>(),
                        Technologies = updateDto.Technologies ?? new List<string>(),
                        Tools = updateDto.Tools ?? new List<string>()
                    };
                    await r_userProfileService.AddExpertiseToGainerAsync(id, expertiseDto);
                }
                r_logger.LogInformation("Successfully created or updated Gainer profile: UserId={UserId}", id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Gainer not found: UserId={UserId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Gainer with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating Gainer profile: UserId={UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating the Gainer profile" });
            }
        }

        /// <summary>
        /// Updates a Mentor profile.
        /// </summary>
        /// <param name="id">The unique identifier of the Mentor.</param>
        /// <param name="updateDto">The profile update data.</param>
        /// <returns>The updated Mentor profile.</returns>
        [HttpPut("mentor/{id}/profile")]
        [ProducesResponseType(typeof(Mentor), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateMentorProfile(Guid id, [FromBody] MentorProfileUpdateDTO updateDto)
        {
            r_logger.LogInformation("Updating Mentor profile: UserId={UserId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Mentor ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            if (updateDto == null)
            {
                r_logger.LogWarning("Update data is null for Mentor: UserId={UserId}", id);
                return BadRequest(new { Message = "Update data cannot be null." });
            }

            try
            {
                var result = await r_userProfileService.CreateOrUpdateMentorProfileAsync(id, updateDto);
                
                // Check if any expertise strings were provided and add expertise
                if (updateDto.ProgrammingLanguages?.Any() == true || updateDto.Technologies?.Any() == true || updateDto.Tools?.Any() == true)
                {
                    r_logger.LogInformation("Adding expertise to Mentor during profile update: UserId={UserId}", id);
                    var expertiseDto = new AddTechExpertiseDto
                    {
                        ProgrammingLanguages = updateDto.ProgrammingLanguages ?? new List<string>(),
                        Technologies = updateDto.Technologies ?? new List<string>(),
                        Tools = updateDto.Tools ?? new List<string>()
                    };
                    await r_userProfileService.AddExpertiseToMentorAsync(id, expertiseDto);
                }
                r_logger.LogInformation("Successfully updated Mentor profile: UserId={UserId}", id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Mentor not found: UserId={UserId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Mentor with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating Mentor profile: UserId={UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating the Mentor profile" });
            }
        }

        /// <summary>
        /// Updates a Nonprofit Organization profile.
        /// </summary>
        /// <param name="id">The unique identifier of the Nonprofit Organization.</param>
        /// <param name="updateDto">The profile update data.</param>
        /// <returns>The updated Nonprofit Organization profile.</returns>
        [HttpPut("nonprofit/{id}/profile")]
        [ProducesResponseType(typeof(NonprofitOrganization), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateNonprofitProfile(Guid id, [FromBody] NonprofitProfileUpdateDTO updateDto)
        {
            r_logger.LogInformation("Updating Nonprofit profile: NonprofitId={NonprofitId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Nonprofit ID provided: {NonprofitId}", id);
                return BadRequest(new { Message = "Nonprofit ID cannot be empty." });
            }

            if (updateDto == null)
            {
                r_logger.LogWarning("Update data is null for Nonprofit: NonprofitId={NonprofitId}", id);
                return BadRequest(new { Message = "Update data cannot be null." });
            }

            try
            {
                var result = await r_userProfileService.CreateOrUpdateNonprofitProfileAsync(id, updateDto);
                
                // Check if any expertise strings were provided and add expertise
                if (!string.IsNullOrWhiteSpace(updateDto.FieldOfWork) || !string.IsNullOrWhiteSpace(updateDto.MissionStatement))
                {
                    r_logger.LogInformation("Adding expertise to Nonprofit during profile update: NonprofitId={NonprofitId}", id);
                    var expertiseDto = new AddNonprofitExpertiseDto
                    {
                        FieldOfWork = updateDto.FieldOfWork ?? string.Empty,
                        MissionStatement = updateDto.MissionStatement ?? string.Empty
                    };
                    await r_userProfileService.AddExpertiseToNonprofitAsync(id, expertiseDto);
                }
                r_logger.LogInformation("Successfully updated Nonprofit profile: NonprofitId={NonprofitId}", id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Nonprofit not found: NonprofitId={NonprofitId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Nonprofit with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating Nonprofit profile: NonprofitId={NonprofitId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating the nonprofit profile" });
            }
        }

        #endregion

        #region Project History

        /// <summary>
        /// Gets the project history for a Gainer user.
        /// </summary>
        /// <param name="id">The unique identifier of the Gainer.</param>
        /// <returns>The list of projects the Gainer has participated in.</returns>
        [HttpGet("gainer/{id}/projects")]
        [ProducesResponseType(typeof(IEnumerable<ConciseUserProjectViewModel>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetGainerProjectHistory(Guid id)
        {
            r_logger.LogInformation("Getting Gainer project history: UserId={UserId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Gainer ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            try
            {
                var projects = await r_userProfileService.GetGainerProjectHistoryAsync(id);
                var projectViewModels = projects.Select(p => new ConciseUserProjectViewModel(p, id)).ToList();
                r_logger.LogInformation("Successfully retrieved Gainer project history: UserId={UserId}, ProjectsCount={ProjectsCount}", id, projectViewModels.Count);
                return Ok(projectViewModels);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Gainer not found: UserId={UserId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Gainer with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Gainer project history: UserId={UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the Gainer project history" });
            }
        }

        /// <summary>
        /// Gets the project history for a Mentor user.
        /// </summary>
        /// <param name="id">The unique identifier of the Mentor.</param>
        /// <returns>The list of projects the Mentor has mentored.</returns>
        [HttpGet("mentor/{id}/projects")]
        [ProducesResponseType(typeof(IEnumerable<ConciseUserProjectViewModel>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetMentorProjectHistory(Guid id)
        {
            r_logger.LogInformation("Getting Mentor project history: UserId={UserId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Mentor ID provided: {UserId}", id);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            try
            {
                var projects = await r_userProfileService.GetMentorProjectHistoryAsync(id);
                var projectViewModels = projects.Select(p => new ConciseUserProjectViewModel(p, id)).ToList();
                r_logger.LogInformation("Successfully retrieved Mentor project history: UserId={UserId}, ProjectsCount={ProjectsCount}", id, projectViewModels.Count);
                return Ok(projectViewModels);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Mentor not found: UserId={UserId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Mentor with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Mentor project history: UserId={UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the Mentor project history" });
            }
        }

        /// <summary>
        /// Gets the project history for a Nonprofit Organization.
        /// </summary>
        /// <param name="id">The unique identifier of the Nonprofit Organization.</param>
        /// <returns>The list of projects owned by the Nonprofit Organization.</returns>
        [HttpGet("nonprofit/{id}/projects")]
        [ProducesResponseType(typeof(IEnumerable<ConciseUserProjectViewModel>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetNonprofitProjectHistory(Guid id)
        {
            r_logger.LogInformation("Getting Nonprofit project history: NonprofitId={NonprofitId}", id);
            
            if (id == Guid.Empty)
            {
                r_logger.LogWarning("Invalid Nonprofit ID provided: {NonprofitId}", id);
                return BadRequest(new { Message = "Nonprofit ID cannot be empty." });
            }

            try
            {
                var projects = await r_userProfileService.GetNonprofitProjectHistoryAsync(id);
                var projectViewModels = projects.Select(p => new ConciseUserProjectViewModel(p, null)).ToList();
                r_logger.LogInformation("Successfully retrieved Nonprofit project history: NonprofitId={NonprofitId}, ProjectsCount={ProjectsCount}", id, projectViewModels.Count);
                return Ok(projectViewModels);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Nonprofit not found: NonprofitId={NonprofitId}, Error={Error}", id, ex.Message);
                return NotFound(new { Message = $"Nonprofit with ID {id} not found" });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Nonprofit project history: NonprofitId={NonprofitId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the Nonprofit project history" });
            }
        }

        #endregion

        #region Profile Picture Management

        /// <summary>
        /// Upload a new profile picture for the current user
        /// </summary>
        /// <param name="request">Profile picture upload request with file and description</param>
        /// <returns>Profile picture upload response with URL and metadata</returns>
        [HttpPost("me/profile-picture")]
        [Authorize(Policy = "RequireAccessAsUser")]
        [ProducesResponseType(typeof(ProfilePictureResponseViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProfilePictureResponseViewModel>> UploadProfilePicture([FromForm] ProfilePictureRequestDto request)
        {
            try
            {
                // Use the existing GetCurrentUser method to get the user profile
                var userProfileResult = await GetCurrentUser();
                if (userProfileResult.Result is NotFoundObjectResult || userProfileResult.Result is UnauthorizedObjectResult)
                {
                    return userProfileResult.Result;
                }

                var userProfile = userProfileResult.Value as UserProfileDto;
                if (userProfile == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var userId = userProfile.UserId;
                r_logger.LogInformation("Profile picture upload requested: UserId={UserId}, FileName={FileName}, Size={Size}KB",
                    userId, request.ProfilePicture.FileName, request.ProfilePicture.Length / 1024);

                // Validate file using image-specific validation (includes MIME type checking)
                if (!r_FileUploadService.IsValidImageFile(request.ProfilePicture))
                {
                    return BadRequest("Invalid image file. Please ensure the file is a valid image format and under 10MB.");
                }

                // Upload to blob storage using generic method
                var blobUrl = await r_FileUploadService.UploadFileAsync(
                    request.ProfilePicture,
                    "profile-pictures",
                    userId.ToString());

                // Update user's profile picture URL in database
                var user = await r_DbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    user.ProfilePictureURL = blobUrl;
                    await r_DbContext.SaveChangesAsync();
                    r_logger.LogInformation("Updated user profile picture URL in database: UserId={UserId}, NewUrl={NewUrl}", userId, blobUrl);
                }

                // Create response
                var response = new ProfilePictureResponseViewModel
                {
                    ProfilePictureUrl = blobUrl,
                    Description = request.Description,
                    UploadedAt = DateTimeOffset.UtcNow,
                    FileSizeInBytes = request.ProfilePicture.Length,
                    FileName = request.ProfilePicture.FileName,
                    ContentType = request.ProfilePicture.ContentType
                };

                r_logger.LogInformation("Profile picture uploaded successfully: UserId={UserId}, BlobUrl={BlobUrl}",
                    userId, blobUrl);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                r_logger.LogWarning("Invalid profile picture upload request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error uploading profile picture");
                return StatusCode(500, "An error occurred while uploading the profile picture");
            }
        }

        /// <summary>
        /// Update the current user's profile picture
        /// </summary>
        [HttpPut("me/profile-picture")]
        [Authorize(Policy = "RequireAccessAsUser")]
        [ProducesResponseType(typeof(ProfilePictureResponseViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProfilePictureResponseViewModel>> UpdateProfilePicture([FromForm] ProfilePictureRequestDto request)
        {
            try
            {
                // Use the existing GetCurrentUser method to get the user profile
                var userProfileResult = await GetCurrentUser();
                if (userProfileResult.Result is NotFoundObjectResult || userProfileResult.Result is UnauthorizedObjectResult)
                {
                    return userProfileResult.Result;
                }

                var userProfile = userProfileResult.Value as UserProfileDto;
                if (userProfile == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var userId = userProfile.UserId;
                // Get current user to find existing profile picture URL
                var currentUser = await r_DbContext.Users.FindAsync(userId);
                if (currentUser == null)
                {
                    return NotFound("User not found");
                }

                r_logger.LogInformation("Profile picture update requested: UserId={UserId}, FileName={FileName}, Size={Size}KB",
                    userId, request.ProfilePicture.FileName, request.ProfilePicture.Length / 1024);

                // Validate file using image-specific validation (includes MIME type checking)
                if (!r_FileUploadService.IsValidImageFile(request.ProfilePicture))
                {
                    return BadRequest("Invalid image file. Please ensure the file is a valid image format and under 10MB.");
                }

                // Update profile picture (delete old, upload new) using generic method
                var success = await r_FileUploadService.UpdateFileAsync(
                    request.ProfilePicture,
                    currentUser.ProfilePictureURL,
                    "profile-pictures",
                    userId.ToString());

                if (!success)
                {
                    return StatusCode(500, "Failed to update profile picture");
                }

                // Get the new blob URL (we need to re-upload to get the new URL)
                var newBlobUrl = await r_FileUploadService.UploadFileAsync(
                    request.ProfilePicture,
                    "profile-pictures",
                    userId.ToString());

                // Update user's profile picture URL in database
                var user = await r_DbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    user.ProfilePictureURL = newBlobUrl;
                    await r_DbContext.SaveChangesAsync();
                    r_logger.LogInformation("Updated user profile picture URL in database: UserId={UserId}, NewUrl={NewUrl}", userId, newBlobUrl);
                }

                var response = new ProfilePictureResponseViewModel
                {
                    ProfilePictureUrl = newBlobUrl,
                    Description = request.Description,
                    UploadedAt = DateTimeOffset.UtcNow,
                    FileSizeInBytes = request.ProfilePicture.Length,
                    FileName = request.ProfilePicture.FileName,
                    ContentType = request.ProfilePicture.ContentType
                };

                r_logger.LogInformation("Profile picture updated successfully: UserId={UserId}, NewBlobUrl={BlobUrl}",
                    userId, newBlobUrl);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                r_logger.LogWarning("Invalid profile picture update request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating profile picture");
                return StatusCode(500, "An error occurred while updating the profile picture");
            }
        }

        /// <summary>
        /// Delete the current user's profile picture
        /// </summary>
        [HttpDelete("me/profile-picture")]
        [Authorize(Policy = "RequireAccessAsUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteProfilePicture()
        {
            try
            {
                // Use the existing GetCurrentUser method to get the user profile
                var userProfileResult = await GetCurrentUser();
                if (userProfileResult.Result is NotFoundObjectResult || userProfileResult.Result is UnauthorizedObjectResult)
                {
                    return userProfileResult.Result;
                }

                var userProfile = userProfileResult.Value as UserProfileDto;
                if (userProfile == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var userId = userProfile.UserId;
                // Get current user to find existing profile picture URL
                var currentUser = await r_DbContext.Users.FindAsync(userId);
                if (currentUser == null)
                {
                    return NotFound("User not found");
                }

                if (string.IsNullOrEmpty(currentUser.ProfilePictureURL))
                {
                    return NotFound("No profile picture found to delete");
                }

                r_logger.LogInformation("Profile picture deletion requested: UserId={UserId}, CurrentUrl={CurrentUrl}",
                    userId, currentUser.ProfilePictureURL);

                // Delete from blob storage using generic method
                var success = await r_FileUploadService.DeleteFileAsync(currentUser.ProfilePictureURL, "profile-pictures");

                if (success)
                {
                    // Clear the profile picture URL from database
                    currentUser.ProfilePictureURL = null;
                    await r_DbContext.SaveChangesAsync();
                    r_logger.LogInformation("Profile picture deleted successfully and URL cleared from database: UserId={UserId}", userId);
                    return Ok("Profile picture deleted successfully");
                }
                else
                {
                    r_logger.LogWarning("Profile picture deletion failed: UserId={UserId}", userId);
                    return StatusCode(500, "Failed to delete profile picture");
                }
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error deleting profile picture");
                return StatusCode(500, "An error occurred while deleting the profile picture");
            }
        }

        /// <summary>
        /// Get the current user's profile picture
        /// Requires authentication
        /// </summary>
        [HttpGet("me/profile-picture")]
        [Authorize(Policy = "RequireAccessAsUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyProfilePicture()
        {
            try
            {
                // Use the existing GetCurrentUser method to get the user profile
                var userProfileResult = await GetCurrentUser();
                if (userProfileResult.Result is NotFoundObjectResult || userProfileResult.Result is UnauthorizedObjectResult)
                {
                    return userProfileResult.Result;
                }

                var userProfile = userProfileResult.Value as UserProfileDto;
                if (userProfile == null)
                {
                    return Unauthorized("User not authenticated");
                }

                // Get the full user entity to access ProfilePictureURL
                var user = await r_DbContext.Users.FindAsync(userProfile.UserId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                if (string.IsNullOrEmpty(user.ProfilePictureURL))
                {
                    return NotFound("No profile picture found");
                }

                // Fetch image from private blob storage using generic method
                var blobResult = await r_FileUploadService.GetFileAsync(user.ProfilePictureURL, "profile-pictures");
                if (blobResult == null)
                {
                    return NotFound("Profile picture not found");
                }

                // Set cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=3600");
                Response.Headers.Add("ETag", $"\"{blobResult.Details.ETag}\"");

                // Stream the image
                return File(blobResult.Content, blobResult.ContentType);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving current user's profile picture");
                return StatusCode(500, "An error occurred while retrieving the profile picture");
            }
        }

        /// <summary>
        /// Get a user's profile picture by user ID
        /// This endpoint acts as a proxy to serve images from private blob storage
        /// </summary>
        [HttpGet("{userId}/profile-picture")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilePicture(Guid userId)
        {
            try
            {
                // Get user to find profile picture URL
                var user = await r_DbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    r_logger.LogWarning("User not found when requesting profile picture: UserId={UserId}", userId);
                    return NotFound("User not found");
                }

                if (string.IsNullOrEmpty(user.ProfilePictureURL))
                {
                    r_logger.LogDebug("User has no profile picture: UserId={UserId}", userId);
                    return NotFound("No profile picture found");
                }

                r_logger.LogDebug("Retrieving profile picture: UserId={UserId}, BlobUrl={BlobUrl}",
                    userId, user.ProfilePictureURL);

                // Fetch image from private blob storage using generic method
                var blobResult = await r_FileUploadService.GetFileAsync(user.ProfilePictureURL, "profile-pictures");
                if (blobResult == null)
                {
                    r_logger.LogWarning("Profile picture blob not found: UserId={UserId}, BlobUrl={BlobUrl}",
                        userId, user.ProfilePictureURL);
                    return NotFound("Profile picture not found");
                }

                // Set cache headers for better performance
                Response.Headers.Add("Cache-Control", "public, max-age=3600"); // 1 hour cache
                Response.Headers.Add("ETag", $"\"{blobResult.Details.ETag}\"");

                // Stream the image to the browser
                return File(blobResult.Content, blobResult.ContentType);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving profile picture: UserId={UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving the profile picture");
            }
        }

        #endregion

        #region Search

        /// <summary>
        /// Searches for Gainers based on a search term.
        /// </summary>
        /// <param name="searchTerm">The search term to filter Gainers.</param>
        /// <returns>A list of Gainers matching the search criteria.</returns>
        [HttpGet("gainer/search")]
        [ProducesResponseType(typeof(IEnumerable<Gainer>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchGainers([FromQuery] string searchTerm)
        {
            r_logger.LogInformation("Searching Gainers: SearchTerm={SearchTerm}", searchTerm);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                r_logger.LogWarning("Empty search term provided for Gainer search");
                return BadRequest(new { Message = "Search term cannot be empty." });
            }

            try
            {
                var results = await r_userProfileService.SearchGainersAsync(searchTerm);
                r_logger.LogInformation("Successfully searched Gainers: SearchTerm={SearchTerm}, ResultsCount={ResultsCount}", searchTerm, results.Count());
                return Ok(results);
            }
            catch (NotImplementedException)
            {
                r_logger.LogWarning("SearchGainersAsync not implemented yet");
                return StatusCode(501, new { Message = "Search functionality not implemented yet." });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error searching Gainers: SearchTerm={SearchTerm}", searchTerm);
                return StatusCode(500, new { Message = "An error occurred while searching Gainers" });
            }
        }

        /// <summary>
        /// Searches for Mentors based on a search term.
        /// </summary>
        /// <param name="searchTerm">The search term to filter Mentors.</param>
        /// <returns>A list of Mentors matching the search criteria.</returns>
        [HttpGet("mentor/search")]
        [ProducesResponseType(typeof(IEnumerable<Mentor>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchMentors([FromQuery] string searchTerm)
        {
            r_logger.LogInformation("Searching Mentors: SearchTerm={SearchTerm}", searchTerm);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                r_logger.LogWarning("Empty search term provided for Mentor search");
                return BadRequest(new { Message = "Search term cannot be empty." });
            }

            try
            {
                var results = await r_userProfileService.SearchMentorsAsync(searchTerm);
                r_logger.LogInformation("Successfully searched Mentors: SearchTerm={SearchTerm}, ResultsCount={ResultsCount}", searchTerm, results.Count());
                return Ok(results);
            }
            catch (NotImplementedException)
            {
                r_logger.LogWarning("SearchMentorsAsync not implemented yet");
                return StatusCode(501, new { Message = "Search functionality not implemented yet." });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error searching Mentors: SearchTerm={SearchTerm}", searchTerm);
                return StatusCode(500, new { Message = "An error occurred while searching Mentors" });
            }
        }

        /// <summary>
        /// Searches for Nonprofit Organizations based on a search term.
        /// </summary>
        /// <param name="searchTerm">The search term to filter Nonprofit Organizations.</param>
        /// <returns>A list of Nonprofit Organizations matching the search criteria.</returns>
        [HttpGet("nonprofit/search")]
        [ProducesResponseType(typeof(IEnumerable<NonprofitOrganization>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchNonprofits([FromQuery] string searchTerm)
        {
            r_logger.LogInformation("Searching Nonprofits: SearchTerm={SearchTerm}", searchTerm);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                r_logger.LogWarning("Empty search term provided for Nonprofit search");
                return BadRequest(new { Message = "Search term cannot be empty." });
            }

            try
            {
                var results = await r_userProfileService.SearchNonprofitsAsync(searchTerm);
                r_logger.LogInformation("Successfully searched Nonprofits: SearchTerm={SearchTerm}, ResultsCount={ResultsCount}", searchTerm, results.Count());
                return Ok(results);
            }
            catch (NotImplementedException)
            {
                r_logger.LogWarning("SearchNonprofitsAsync not implemented yet");
                return StatusCode(501, new { Message = "Search functionality not implemented yet." });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error searching Nonprofits: SearchTerm={SearchTerm}", searchTerm);
                return StatusCode(500, new { Message = "An error occurred while searching Nonprofits" });
            }
        }

        #endregion
    }
}

