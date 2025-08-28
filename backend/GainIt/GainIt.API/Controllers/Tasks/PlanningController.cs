using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Services.Tasks.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Controllers.Tasks
{
    [ApiController]
    [Route("api/projects/{projectId}/planning")]
    public class PlanningController : ControllerBase
    {
        private readonly IPlanningService r_PlanningService;
        private readonly ILogger<PlanningController> r_Logger;

        public PlanningController(
            IPlanningService planningService,
            ILogger<PlanningController> logger)
        {
            r_PlanningService = planningService;
            r_Logger = logger;
        }

        /// <summary>
        /// Generates an AI-powered roadmap for a project when the team presses "Start Project".
        /// The roadmap is based on the actual team members and their roles.
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userId">The ID of the user requesting the roadmap</param>
        /// <param name="planRequest">The planning request with goals and constraints</param>
        /// <returns>The generated roadmap with milestones and tasks</returns>
        [HttpPost("generate-roadmap")]
        public async Task<ActionResult<PlanApplyResultViewModel>> GenerateRoadmap(
            Guid projectId,
            [FromQuery] Guid userId,
            [FromBody] PlanRequestDto planRequest)
        {
            r_Logger.LogInformation("Generating AI roadmap for project: ProjectId={ProjectId}, UserId={UserId}", projectId, userId);

            if (projectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID provided: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (userId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid user ID provided: UserId={UserId}", userId);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            if (planRequest == null)
            {
                r_Logger.LogWarning("Plan request is null: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Plan request cannot be null." });
            }

            try
            {
                var result = await r_PlanningService.GenerateForProjectAsync(
                    projectId,
                    planRequest,
                    userId);

                r_Logger.LogInformation("AI roadmap generated successfully: ProjectId={ProjectId}, UserId={UserId}, Milestones={MilestoneCount}, Tasks={TaskCount}", 
                    projectId, userId, result.CreatedMilestones.Count, result.CreatedTasks.Count);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Project not found: ProjectId={ProjectId}, UserId={UserId}, Error={Error}", projectId, userId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error generating AI roadmap: ProjectId={ProjectId}, UserId={UserId}", projectId, userId);
                throw;
            }
        }

        /// <summary>
        /// Elaborates a specific task with detailed guidance and instructions.
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="taskId">The ID of the task to elaborate</param>
        /// <param name="userId">The ID of the user requesting the elaboration</param>
        /// <param name="elaborationRequest">The elaboration request with additional context</param>
        /// <returns>Detailed guidance and instructions for the task</returns>
        [HttpPost("{taskId}/elaborate")]
        public async Task<ActionResult<TaskElaborationResultViewModel>> ElaborateTask(
            Guid projectId,
            Guid taskId,
            [FromQuery] Guid userId,
            [FromBody] TaskElaborationRequestDto elaborationRequest)
        {
            r_Logger.LogInformation("Elaborating task: ProjectId={ProjectId}, TaskId={TaskId}, UserId={UserId}", projectId, taskId, userId);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters provided: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            if (userId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid user ID provided: UserId={UserId}", userId);
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            if (elaborationRequest == null)
            {
                r_Logger.LogWarning("Elaboration request is null: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Elaboration request cannot be null." });
            }

            try
            {
                var result = await r_PlanningService.ElaborateTaskAsync(
                    projectId,
                    taskId,
                    elaborationRequest,
                    userId);

                r_Logger.LogInformation("Task elaboration completed: ProjectId={ProjectId}, TaskId={TaskId}, UserId={UserId}, NotesCount={NotesCount}", 
                    projectId, taskId, userId, result.Notes.Count);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Project or task not found: ProjectId={ProjectId}, TaskId={TaskId}, UserId={UserId}, Error={Error}", projectId, taskId, userId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error elaborating task: ProjectId={ProjectId}, TaskId={TaskId}, UserId={UserId}", projectId, taskId, userId);
                throw;
            }
        }
    }
}
