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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

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

        public UsersController(IUserProfileService i_userProfileService, ILogger<UsersController> i_logger)
        {
            r_userProfileService = i_userProfileService;
            r_logger = i_logger;
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
        [HttpPost("me/ensure")]
        [Authorize(Policy = "RequireAccessAsUser")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
                 ?? tryGetClaim(User, ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(externalId))
                {
                    return Unauthorized(new { Message = "Missing external identity claim (oid/sub)" });
                }

                r_logger.LogDebug("Extracted subject. CorrelationId={CorrelationId}, Subject={Subject}", correlationId, externalId);

                var email = tryGetClaim(User, "emails", ClaimTypes.Email, "email");
                var name = tryGetClaim(User, "name")
                            ?? string.Join(' ',
                                new[] { tryGetClaim(User, "given_name"), tryGetClaim(User, "family_name") }
                                .Where(s => !string.IsNullOrWhiteSpace(s)));

                var idp = tryGetClaim(User, "idp");
                var country = tryGetClaim(User, "country")
                              ?? User.Claims.FirstOrDefault(c =>
                                    c.Type.StartsWith("extension_", StringComparison.OrdinalIgnoreCase) &&
                                    c.Type.EndsWith("country", StringComparison.OrdinalIgnoreCase))?.Value;

                r_logger.LogDebug("Extracted user claims. CorrelationId={CorrelationId}, Email={Email}, Name={Name}, IdentityProvider={IdP}, Country={Country}", 
                    correlationId, email, name, idp, country);

                var dto = new ExternalUserDto
                {
                    ExternalId = externalId!,
                    Email = email,
                    FullName = string.IsNullOrWhiteSpace(name) ? null : name,
                    IdentityProvider = idp,
                    Country = country
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
                var result = await r_userProfileService.UpdateGainerProfileAsync(id, updateDto);
                r_logger.LogInformation("Successfully updated Gainer profile: UserId={UserId}", id);
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
                var result = await r_userProfileService.UpdateMentorProfileAsync(id, updateDto);
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
                var result = await r_userProfileService.UpdateNonprofitProfileAsync(id, updateDto);
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

