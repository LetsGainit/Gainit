using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
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
    [Route("api/projects/{projectId}/planning")]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class PlanningController : ControllerBase
    {
        private readonly IPlanningService r_PlanningService;
        private readonly ILogger<PlanningController> r_Logger;
        private readonly GainItDbContext r_DbContext;

        public PlanningController(
            IPlanningService planningService,
            ILogger<PlanningController> logger,
            GainItDbContext dbContext)
        {
            r_PlanningService = planningService;
            r_Logger = logger;
            r_DbContext = dbContext;
        }

        /// <summary>
        /// Generates an AI-powered roadmap for a project when the team presses "Start Project".
        /// The roadmap is based on the actual team members and their roles.
        /// </summary>
        /// <param name="i_ProjectId">The ID of the project</param>
        /// <param name="planRequest">The planning request with goals and constraints</param>
        /// <returns>The generated roadmap with milestones and tasks</returns>
        [HttpPost("generate-roadmap")]
        public async Task<ActionResult<PlanApplyResultViewModel>> GenerateRoadmap(
            Guid i_ProjectId,
            [FromBody] PlanRequestDto planRequest)
        {
            r_Logger.LogInformation("Generating AI roadmap for project: ProjectId={ProjectId}", i_ProjectId);

            if (i_ProjectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID provided: ProjectId={ProjectId}", i_ProjectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (planRequest == null)
            {
                r_Logger.LogWarning("Plan request is null: ProjectId={ProjectId}", i_ProjectId);
                return BadRequest(new { Message = "Plan request cannot be null." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var result = await r_PlanningService.GenerateForProjectAsync(
                    i_ProjectId,
                    planRequest,
                    userId);

                r_Logger.LogInformation("AI roadmap generated successfully: ProjectId={ProjectId}, UserId={UserId}, Milestones={MilestoneCount}, Tasks={TaskCount}", 
                    i_ProjectId, userId, result.CreatedMilestones.Count, result.CreatedTasks.Count);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Project not found: ProjectId={ProjectId}, Error={Error}", i_ProjectId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error generating AI roadmap: ProjectId={ProjectId}", i_ProjectId);
                throw;
            }
        }

        /// <summary>
        /// Elaborates a specific task with detailed guidance and instructions.
        /// </summary>
        /// <param name="i_ProjectId">The ID of the project</param>
        /// <param name="i_TaskId">The ID of the task to elaborate</param>
        /// <param name="elaborationRequest">The elaboration request with additional context</param>
        /// <returns>Detailed guidance and instructions for the task</returns>
        [HttpPost("{i_TaskId}/elaborate")]
        public async Task<ActionResult<TaskElaborationResultViewModel>> ElaborateTask(
            Guid i_ProjectId,
            Guid i_TaskId,
            [FromBody] TaskElaborationRequestDto elaborationRequest)
        {
            r_Logger.LogInformation("Elaborating task: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters provided: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            if (elaborationRequest == null)
            {
                r_Logger.LogWarning("Elaboration request is null: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Elaboration request cannot be null." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var result = await r_PlanningService.ElaborateTaskAsync(
                    i_ProjectId,
                    i_TaskId,
                    elaborationRequest,
                    userId);

                r_Logger.LogInformation("Task elaboration completed: ProjectId={ProjectId}, TaskId={TaskId}, UserId={UserId}, NotesCount={NotesCount}", 
                    i_ProjectId, i_TaskId, userId, result.Notes.Count);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Project or task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error elaborating task: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                throw;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Gets the current user ID from the authentication context.
        /// Extracts the external ID from JWT claims and maps it to the database User ID.
        /// </summary>
        /// <returns>The current user ID from the database.</returns>
        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var externalId = User.FindFirst("oid")?.Value
                  ?? User.FindFirst("sub")?.Value;

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
