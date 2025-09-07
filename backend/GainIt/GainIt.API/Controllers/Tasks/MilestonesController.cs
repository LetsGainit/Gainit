using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Models.Enums.Tasks;
using GainIt.API.Services.Tasks.Interfaces;
using GainIt.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace GainIt.API.Controllers.Tasks
{
    [ApiController]
    [Route("api/projects/{projectId}/milestones")]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class MilestonesController : ControllerBase
    {
        private readonly IMilestoneService r_MilestoneService;
        private readonly ILogger<MilestonesController> r_Logger;
        private readonly GainItDbContext r_DbContext;

        public MilestonesController(
            IMilestoneService milestoneService,
            ILogger<MilestonesController> logger,
            GainItDbContext dbContext)
        {
            r_MilestoneService = milestoneService;
            r_Logger = logger;
            r_DbContext = dbContext;
        }

        #region Milestone Queries

        /// <summary>
        /// Gets all milestones for a project.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <returns>List of milestones for the project.</returns>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProjectMilestoneViewModel>>> GetMilestones([FromRoute] Guid projectId)
        {
            r_Logger.LogInformation("Getting milestones: ProjectId={ProjectId}", projectId);

            if (projectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var milestones = await r_MilestoneService.GetMilestonesListAsync(projectId, userId);

                r_Logger.LogInformation("Milestones retrieved successfully: ProjectId={ProjectId}, Count={Count}", projectId, milestones.Count);
                return Ok(milestones);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting milestones: ProjectId={ProjectId}", projectId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving milestones." });
            }
        }

        /// <summary>
        /// Gets a specific milestone by ID.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="milestoneId">The milestone ID.</param>
        /// <returns>The milestone details.</returns>
        [HttpGet("{milestoneId}")]
        public async Task<ActionResult<ProjectMilestoneViewModel>> GetMilestone([FromRoute] Guid projectId, [FromRoute] Guid milestoneId)
        {
            r_Logger.LogInformation("Getting milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);

            if (projectId == Guid.Empty || milestoneId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return BadRequest(new { Message = "Project ID and Milestone ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var milestone = await r_MilestoneService.GetMilestoneAsync(projectId, milestoneId, userId);

                if (milestone == null)
                {
                    r_Logger.LogWarning("Milestone not found: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                    return NotFound(new { Message = "Milestone not found." });
                }

                r_Logger.LogInformation("Milestone retrieved successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return Ok(milestone);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the milestone." });
            }
        }

        #endregion

        #region Milestone CRUD Operations

        /// <summary>
        /// Creates a new milestone.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="milestoneCreateDto">The milestone creation data.</param>
        /// <returns>The created milestone.</returns>
        [HttpPost]
        public async Task<ActionResult<ProjectMilestoneViewModel>> CreateMilestone([FromRoute] Guid projectId, [FromBody] ProjectMilestoneCreateDto milestoneCreateDto)
        {
            r_Logger.LogInformation("Creating milestone: ProjectId={ProjectId}, Title={Title}", projectId, milestoneCreateDto.Title);

            if (projectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for milestone creation: ProjectId={ProjectId}", projectId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var milestone = await r_MilestoneService.CreateAsync(projectId, milestoneCreateDto, userId);

                r_Logger.LogInformation("Milestone created successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}, Title={Title}", 
                    projectId, milestone.MilestoneId, milestone.Title);

                return CreatedAtAction(nameof(GetMilestone), new { projectId = projectId, milestoneId = milestone.MilestoneId }, milestone);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Resource not found: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error creating milestone: ProjectId={ProjectId}, Title={Title}", projectId, milestoneCreateDto.Title);
                return StatusCode(500, new { Message = "An unexpected error occurred while creating the milestone." });
            }
        }

        /// <summary>
        /// Updates an existing milestone.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="milestoneId">The milestone ID.</param>
        /// <param name="milestoneUpdateDto">The milestone update data.</param>
        /// <returns>The updated milestone.</returns>
        [HttpPut("{milestoneId}")]
        public async Task<ActionResult<ProjectMilestoneViewModel>> UpdateMilestone([FromRoute] Guid projectId, [FromRoute] Guid milestoneId, [FromBody] ProjectMilestoneUpdateDto milestoneUpdateDto)
        {
            r_Logger.LogInformation("Updating milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);

            if (projectId == Guid.Empty || milestoneId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return BadRequest(new { Message = "Project ID and Milestone ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for milestone update: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var milestone = await r_MilestoneService.UpdateAsync(projectId, milestoneId, milestoneUpdateDto, userId);

                r_Logger.LogInformation("Milestone updated successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return Ok(milestone);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, MilestoneId={MilestoneId}, Error={Error}", projectId, milestoneId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Milestone not found: ProjectId={ProjectId}, MilestoneId={MilestoneId}, Error={Error}", projectId, milestoneId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the milestone." });
            }
        }

        /// <summary>
        /// Deletes a milestone.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="milestoneId">The milestone ID.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{milestoneId}")]
        public async Task<ActionResult> DeleteMilestone([FromRoute] Guid projectId, [FromRoute] Guid milestoneId)
        {
            r_Logger.LogInformation("Deleting milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);

            if (projectId == Guid.Empty || milestoneId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return BadRequest(new { Message = "Project ID and Milestone ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_MilestoneService.DeleteAsync(projectId, milestoneId, userId);

                r_Logger.LogInformation("Milestone deleted successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, MilestoneId={MilestoneId}, Error={Error}", projectId, milestoneId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Milestone not found: ProjectId={ProjectId}, MilestoneId={MilestoneId}, Error={Error}", projectId, milestoneId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the milestone." });
            }
        }

        #endregion

        #region Milestone Status Management

        /// <summary>
        /// Changes the status of a milestone.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="milestoneId">The milestone ID.</param>
        /// <param name="newStatus">The new status.</param>
        /// <returns>The updated milestone.</returns>
        [HttpPut("{milestoneId}/status")]
        public async Task<ActionResult<ProjectMilestoneViewModel>> ChangeMilestoneStatus([FromRoute] Guid projectId, [FromRoute] Guid milestoneId, [FromBody] eMilestoneStatus newStatus)
        {
            r_Logger.LogInformation("Changing milestone status: ProjectId={ProjectId}, MilestoneId={MilestoneId}, NewStatus={NewStatus}", 
                projectId, milestoneId, newStatus);

            if (projectId == Guid.Empty || milestoneId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, MilestoneId={MilestoneId}", projectId, milestoneId);
                return BadRequest(new { Message = "Project ID and Milestone ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var milestone = await r_MilestoneService.ChangeStatusAsync(projectId, milestoneId, newStatus, userId);

                r_Logger.LogInformation("Milestone status changed successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}, NewStatus={NewStatus}", 
                    projectId, milestoneId, newStatus);
                return Ok(milestone);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, MilestoneId={MilestoneId}, Error={Error}", 
                    projectId, milestoneId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Milestone not found: ProjectId={ProjectId}, MilestoneId={MilestoneId}, Error={Error}", 
                    projectId, milestoneId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error changing milestone status: ProjectId={ProjectId}, MilestoneId={MilestoneId}, NewStatus={NewStatus}", 
                    projectId, milestoneId, newStatus);
                return StatusCode(500, new { Message = "An unexpected error occurred while changing the milestone status." });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to extract claim values from the user principal.
        /// </summary>
        /// <param name="user">The user principal.</param>
        /// <param name="types">The claim types to try.</param>
        /// <returns>The first non-empty claim value found, or null if none found.</returns>
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
        /// Gets the current user ID from the authentication context.
        /// Extracts the external ID from JWT claims and maps it to the database User ID.
        /// </summary>
        /// <returns>The current user ID from the database.</returns>
        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var externalId =
                tryGetClaim(User, "oid", ClaimTypes.NameIdentifier)
            ?? tryGetClaim(User, "sub")
            ?? tryGetClaim(User, ClaimTypes.NameIdentifier)
            ?? tryGetClaim(User, "http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? tryGetClaim(User, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            if (string.IsNullOrEmpty(externalId))
                throw new UnauthorizedAccessException("User ID not found in token.");

            // Find the user in the database by external ID
            var user = await r_DbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ExternalId == externalId);

            if (user == null)
                throw new UnauthorizedAccessException("User not found in database.");

            return user.UserId;
        }

        #endregion
    }
}
