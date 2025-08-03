using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Services.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
                throw;
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
                FullMentorViewModel mentorViewModel = new FullMentorViewModel(mentor, projects, achievements);
                
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
                throw;
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
                throw;
            }
        }
    }
}

