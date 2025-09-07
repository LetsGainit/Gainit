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
    [Route("api/projects/{projectId}/tasks")]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService r_TaskService;
        private readonly ILogger<TasksController> r_Logger;
        private readonly GainItDbContext r_DbContext;

        public TasksController(
            ITaskService taskService,
            ILogger<TasksController> logger,
            GainItDbContext dbContext)
        {
            r_TaskService = taskService;
            r_Logger = logger;
            r_DbContext = dbContext;
        }

        #region Task Queries

        /// <summary>
        /// Gets a specific task by ID.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <returns>The task details.</returns>
        [HttpGet("{taskId}")]
        public async Task<ActionResult<ProjectTaskViewModel>> GetTask([FromRoute] Guid projectId, [FromRoute] Guid taskId)
        {
            r_Logger.LogInformation("Getting task: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.GetTaskAsync(projectId, taskId, userId);

                if (task == null)
                {
                    r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                    return NotFound(new { Message = "Task not found." });
                }

                r_Logger.LogInformation("Task retrieved successfully: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting task: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the task." });
            }
        }

        /// <summary>
        /// Gets the current user's active tasks for a project.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="query">Query parameters for filtering and sorting.</param>
        /// <returns>List of active tasks assigned to the user.</returns>
        [HttpGet("my-tasks")]
        public async Task<ActionResult<IReadOnlyList<ProjectTaskListItemViewModel>>> GetMyTasks([FromRoute] Guid projectId, [FromQuery] TaskListQueryDto query)
        {
            r_Logger.LogInformation("Getting my tasks: ProjectId={ProjectId}", projectId);

            if (projectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var tasks = await r_TaskService.ListMyTasksAsync(projectId, userId, query);

                r_Logger.LogInformation("My tasks retrieved successfully: ProjectId={ProjectId}, Count={Count}", projectId, tasks.Count);
                return Ok(tasks);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting my tasks: ProjectId={ProjectId}", projectId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving tasks." });
            }
        }

        /// <summary>
        /// Gets all tasks for a project (board view).
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="query">Query parameters for filtering, sorting, and pagination.</param>
        /// <returns>List of tasks for the project.</returns>
        [HttpGet("board")]
        public async Task<ActionResult<IReadOnlyList<ProjectTaskListItemViewModel>>> GetBoardTasks([FromRoute] Guid projectId, [FromQuery] TaskBoardQueryDto query)
        {
            r_Logger.LogInformation("Getting board tasks: ProjectId={ProjectId}, IncludeCompleted={IncludeCompleted}", projectId, query.IncludeCompleted);

            if (projectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                var tasks = await r_TaskService.ListBoardAsync(projectId, query);

                r_Logger.LogInformation("Board tasks retrieved successfully: ProjectId={ProjectId}, Count={Count}", projectId, tasks.Count);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting board tasks: ProjectId={ProjectId}", projectId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving board tasks." });
            }
        }

        #endregion

        #region Task CRUD Operations

        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskCreateDto">The task creation data.</param>
        /// <returns>The created task.</returns>
        [HttpPost]
        public async Task<ActionResult<ProjectTaskViewModel>> CreateTask([FromRoute] Guid projectId, [FromBody] ProjectTaskCreateDto taskCreateDto)
        {
            r_Logger.LogInformation("Creating task: ProjectId={ProjectId}, Title={Title}", projectId, taskCreateDto.Title);

            if (projectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for task creation: ProjectId={ProjectId}", projectId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.CreateAsync(projectId, taskCreateDto, userId);

                r_Logger.LogInformation("Task created successfully: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}", 
                    projectId, task.TaskId, task.Title);

                return CreatedAtAction(nameof(GetTask), new { projectId, taskId = task.TaskId }, task);
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
                r_Logger.LogError(ex, "Error creating task: ProjectId={ProjectId}, Title={Title}", projectId, taskCreateDto.Title);
                return StatusCode(500, new { Message = "An unexpected error occurred while creating the task." });
            }
        }

        /// <summary>
        /// Updates an existing task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="taskUpdateDto">The task update data.</param>
        /// <returns>The updated task.</returns>
        [HttpPut("{taskId}")]
        public async Task<ActionResult<ProjectTaskViewModel>> UpdateTask([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] ProjectTaskUpdateDto taskUpdateDto)
        {
            r_Logger.LogInformation("Updating task: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for task update: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.UpdateAsync(projectId, taskId, taskUpdateDto, userId);

                r_Logger.LogInformation("Task updated successfully: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating task: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the task." });
            }
        }

        /// <summary>
        /// Deletes a task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{taskId}")]
        public async Task<ActionResult> DeleteTask([FromRoute] Guid projectId, [FromRoute] Guid taskId)
        {
            r_Logger.LogInformation("Deleting task: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.DeleteAsync(projectId, taskId, userId);

                r_Logger.LogInformation("Task deleted successfully: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting task: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the task." });
            }
        }

        #endregion

        #region Task Status and Ordering

        /// <summary>
        /// Changes the status of a task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="newStatus">The new status.</param>
        /// <returns>The updated task.</returns>
        [HttpPut("{taskId}/status")]
        public async Task<ActionResult<ProjectTaskViewModel>> ChangeTaskStatus([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] eTaskStatus newStatus)
        {
            r_Logger.LogInformation("Changing task status: ProjectId={ProjectId}, TaskId={TaskId}, NewStatus={NewStatus}", projectId, taskId, newStatus);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.ChangeStatusAsync(projectId, taskId, newStatus, userId);

                r_Logger.LogInformation("Task status changed successfully: ProjectId={ProjectId}, TaskId={TaskId}, NewStatus={NewStatus}", 
                    projectId, taskId, newStatus);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error changing task status: ProjectId={ProjectId}, TaskId={TaskId}, NewStatus={NewStatus}", 
                    projectId, taskId, newStatus);
                return StatusCode(500, new { Message = "An unexpected error occurred while changing the task status." });
            }
        }

        /// <summary>
        /// Reorders a task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="newOrderIndex">The new order index.</param>
        /// <returns>The updated task.</returns>
        [HttpPut("{taskId}/order")]
        public async Task<ActionResult<ProjectTaskViewModel>> ReorderTask([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] int newOrderIndex)
        {
            r_Logger.LogInformation("Reordering task: ProjectId={ProjectId}, TaskId={TaskId}, NewOrderIndex={NewOrderIndex}", projectId, taskId, newOrderIndex);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.ReorderAsync(projectId, taskId, newOrderIndex, userId);

                r_Logger.LogInformation("Task reordered successfully: ProjectId={ProjectId}, TaskId={TaskId}, NewOrderIndex={NewOrderIndex}", 
                    projectId, taskId, newOrderIndex);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error reordering task: ProjectId={ProjectId}, TaskId={TaskId}, NewOrderIndex={NewOrderIndex}", 
                    projectId, taskId, newOrderIndex);
                return StatusCode(500, new { Message = "An unexpected error occurred while reordering the task." });
            }
        }

        #endregion

        #region Subtasks

        /// <summary>
        /// Gets all subtasks for a task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <returns>List of subtasks.</returns>
        [HttpGet("{taskId}/subtasks")]
        public async Task<ActionResult<IReadOnlyList<ProjectSubtaskViewModel>>> GetSubtasks([FromRoute] Guid projectId, [FromRoute] Guid taskId)
        {
            r_Logger.LogInformation("Getting subtasks: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var subtasks = await r_TaskService.ListSubtasksAsync(projectId, taskId);

                r_Logger.LogInformation("Subtasks retrieved successfully: ProjectId={ProjectId}, TaskId={TaskId}, Count={Count}", 
                    projectId, taskId, subtasks.Count);
                return Ok(subtasks);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting subtasks: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving subtasks." });
            }
        }

        /// <summary>
        /// Adds a subtask to a task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="subtaskCreateDto">The subtask creation data.</param>
        /// <returns>The created subtask.</returns>
        [HttpPost("{taskId}/subtasks")]
        public async Task<ActionResult<ProjectSubtaskViewModel>> AddSubtask([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] SubtaskCreateDto subtaskCreateDto)
        {
            r_Logger.LogInformation("Adding subtask: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}", projectId, taskId, subtaskCreateDto.Title);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for subtask creation: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var subtask = await r_TaskService.AddSubtaskAsync(projectId, taskId, subtaskCreateDto, userId);

                r_Logger.LogInformation("Subtask added successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    projectId, taskId, subtask.SubtaskId);
                return Ok(subtask);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error adding subtask: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}", 
                    projectId, taskId, subtaskCreateDto.Title);
                return StatusCode(500, new { Message = "An unexpected error occurred while adding the subtask." });
            }
        }

        /// <summary>
        /// Updates a subtask.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="subtaskId">The subtask ID.</param>
        /// <param name="subtaskUpdateDto">The subtask update data.</param>
        /// <returns>The updated subtask.</returns>
        [HttpPut("{taskId}/subtasks/{subtaskId}")]
        public async Task<ActionResult<ProjectSubtaskViewModel>> UpdateSubtask([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid subtaskId, [FromBody] SubtaskUpdateDto subtaskUpdateDto)
        {
            r_Logger.LogInformation("Updating subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", projectId, taskId, subtaskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty || subtaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", projectId, taskId, subtaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and Subtask ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var subtask = await r_TaskService.UpdateSubtaskAsync(projectId, taskId, subtaskId, subtaskUpdateDto, userId);

                r_Logger.LogInformation("Subtask updated successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    projectId, taskId, subtaskId);
                return Ok(subtask);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    projectId, taskId, subtaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Subtask not found: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    projectId, taskId, subtaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    projectId, taskId, subtaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the subtask." });
            }
        }

        /// <summary>
        /// Removes a subtask.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="subtaskId">The subtask ID.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{taskId}/subtasks/{subtaskId}")]
        public async Task<ActionResult> RemoveSubtask([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid subtaskId)
        {
            r_Logger.LogInformation("Removing subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", projectId, taskId, subtaskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty || subtaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", projectId, taskId, subtaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and Subtask ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.RemoveSubtaskAsync(projectId, taskId, subtaskId, userId);

                r_Logger.LogInformation("Subtask removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    projectId, taskId, subtaskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    projectId, taskId, subtaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Subtask not found: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    projectId, taskId, subtaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error removing subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    projectId, taskId, subtaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while removing the subtask." });
            }
        }

        /// <summary>
        /// Toggles the completion status of a subtask.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="subtaskId">The subtask ID.</param>
        /// <param name="isDone">Whether the subtask is done.</param>
        /// <returns>The updated subtask.</returns>
        [HttpPut("{taskId}/subtasks/{subtaskId}/toggle")]
        public async Task<ActionResult<ProjectSubtaskViewModel>> ToggleSubtask([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid subtaskId, [FromBody] bool isDone)
        {
            r_Logger.LogInformation("Toggling subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}", 
                projectId, taskId, subtaskId, isDone);

            if (projectId == Guid.Empty || taskId == Guid.Empty || subtaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", projectId, taskId, subtaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and Subtask ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var subtask = await r_TaskService.ToggleSubtaskAsync(projectId, taskId, subtaskId, isDone, userId);

                r_Logger.LogInformation("Subtask toggled successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}", 
                    projectId, taskId, subtaskId, isDone);
                return Ok(subtask);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    projectId, taskId, subtaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Subtask not found: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    projectId, taskId, subtaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error toggling subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}", 
                    projectId, taskId, subtaskId, isDone);
                return StatusCode(500, new { Message = "An unexpected error occurred while toggling the subtask." });
            }
        }

        #endregion

        #region Task Dependencies

        /// <summary>
        /// Adds a dependency between tasks.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="dependsOnTaskId">The ID of the task this task depends on.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("{taskId}/dependencies")]
        public async Task<ActionResult> AddDependency([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] Guid dependsOnTaskId)
        {
            r_Logger.LogInformation("Adding task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                projectId, taskId, dependsOnTaskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty || dependsOnTaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    projectId, taskId, dependsOnTaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and DependsOn Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.AddDependencyAsync(projectId, taskId, dependsOnTaskId, userId);

                r_Logger.LogInformation("Task dependency added successfully: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    projectId, taskId, dependsOnTaskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                r_Logger.LogWarning("Invalid operation: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error adding task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    projectId, taskId, dependsOnTaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while adding the task dependency." });
            }
        }

        /// <summary>
        /// Removes a dependency between tasks.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="dependsOnTaskId">The ID of the task this task depends on.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{taskId}/dependencies/{dependsOnTaskId}")]
        public async Task<ActionResult> RemoveDependency([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid dependsOnTaskId)
        {
            r_Logger.LogInformation("Removing task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                projectId, taskId, dependsOnTaskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty || dependsOnTaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    projectId, taskId, dependsOnTaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and DependsOn Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.RemoveDependencyAsync(projectId, taskId, dependsOnTaskId, userId);

                r_Logger.LogInformation("Task dependency removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    projectId, taskId, dependsOnTaskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Dependency not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error removing task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    projectId, taskId, dependsOnTaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while removing the task dependency." });
            }
        }

        #endregion

        #region Task References

        /// <summary>
        /// Gets all references for a task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <returns>List of task references.</returns>
        [HttpGet("{taskId}/references")]
        public async Task<ActionResult<IReadOnlyList<ProjectTaskReferenceViewModel>>> GetReferences([FromRoute] Guid projectId, [FromRoute] Guid taskId)
        {
            r_Logger.LogInformation("Getting task references: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var references = await r_TaskService.ListReferencesAsync(projectId, taskId);

                r_Logger.LogInformation("Task references retrieved successfully: ProjectId={ProjectId}, TaskId={TaskId}, Count={Count}", 
                    projectId, taskId, references.Count);
                return Ok(references);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting task references: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving task references." });
            }
        }

        /// <summary>
        /// Adds a reference to a task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="referenceCreateDto">The reference creation data.</param>
        /// <returns>The created reference.</returns>
        [HttpPost("{taskId}/references")]
        public async Task<ActionResult<ProjectTaskReferenceViewModel>> AddReference([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromBody] TaskReferenceCreateDto referenceCreateDto)
        {
            r_Logger.LogInformation("Adding task reference: ProjectId={ProjectId}, TaskId={TaskId}, Type={Type}, Url={Url}", 
                projectId, taskId, referenceCreateDto.Type, referenceCreateDto.Url);

            if (projectId == Guid.Empty || taskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for reference creation: ProjectId={ProjectId}, TaskId={TaskId}", projectId, taskId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var reference = await r_TaskService.AddReferenceAsync(projectId, taskId, referenceCreateDto, userId);

                r_Logger.LogInformation("Task reference added successfully: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    projectId, taskId, reference.ReferenceId);
                return Ok(reference);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error adding task reference: ProjectId={ProjectId}, TaskId={TaskId}, Type={Type}, Url={Url}", 
                    projectId, taskId, referenceCreateDto.Type, referenceCreateDto.Url);
                return StatusCode(500, new { Message = "An unexpected error occurred while adding the task reference." });
            }
        }

        /// <summary>
        /// Removes a reference from a task.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="taskId">The task ID.</param>
        /// <param name="referenceId">The reference ID.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{taskId}/references/{referenceId}")]
        public async Task<ActionResult> RemoveReference([FromRoute] Guid projectId, [FromRoute] Guid taskId, [FromRoute] Guid referenceId)
        {
            r_Logger.LogInformation("Removing task reference: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                projectId, taskId, referenceId);

            if (projectId == Guid.Empty || taskId == Guid.Empty || referenceId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    projectId, taskId, referenceId);
                return BadRequest(new { Message = "Project ID, Task ID, and Reference ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.RemoveReferenceAsync(projectId, taskId, referenceId, userId);

                r_Logger.LogInformation("Task reference removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    projectId, taskId, referenceId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", projectId, taskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Reference not found: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}, Error={Error}", 
                    projectId, taskId, referenceId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error removing task reference: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    projectId, taskId, referenceId);
                return StatusCode(500, new { Message = "An unexpected error occurred while removing the task reference." });
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
