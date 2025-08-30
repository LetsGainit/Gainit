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
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <returns>The task details.</returns>
        [HttpGet("{i_TaskId}")]
        public async Task<ActionResult<ProjectTaskViewModel>> GetTask(Guid i_ProjectId, Guid i_TaskId)
        {
            r_Logger.LogInformation("Getting task: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.GetTaskAsync(i_ProjectId, i_TaskId, userId);

                if (task == null)
                {
                    r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                    return NotFound(new { Message = "Task not found." });
                }

                r_Logger.LogInformation("Task retrieved successfully: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting task: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the task." });
            }
        }

        /// <summary>
        /// Gets the current user's active tasks for a project.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="query">Query parameters for filtering and sorting.</param>
        /// <returns>List of active tasks assigned to the user.</returns>
        [HttpGet("my-tasks")]
        public async Task<ActionResult<IReadOnlyList<ProjectTaskListItemViewModel>>> GetMyTasks(Guid i_ProjectId, [FromQuery] TaskListQueryDto query)
        {
            r_Logger.LogInformation("Getting my tasks: ProjectId={ProjectId}", i_ProjectId);

            if (i_ProjectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID: ProjectId={ProjectId}", i_ProjectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var tasks = await r_TaskService.ListMyTasksAsync(i_ProjectId, userId, query);

                r_Logger.LogInformation("My tasks retrieved successfully: ProjectId={ProjectId}, Count={Count}", i_ProjectId, tasks.Count);
                return Ok(tasks);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, Error={Error}", i_ProjectId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting my tasks: ProjectId={ProjectId}", i_ProjectId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving tasks." });
            }
        }

        /// <summary>
        /// Gets all tasks for a project (board view).
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="query">Query parameters for filtering, sorting, and pagination.</param>
        /// <returns>List of tasks for the project.</returns>
        [HttpGet("board")]
        public async Task<ActionResult<IReadOnlyList<ProjectTaskListItemViewModel>>> GetBoardTasks(Guid i_ProjectId, [FromQuery] TaskBoardQueryDto query)
        {
            r_Logger.LogInformation("Getting board tasks: ProjectId={ProjectId}, IncludeCompleted={IncludeCompleted}", i_ProjectId, query.IncludeCompleted);

            if (i_ProjectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID: ProjectId={ProjectId}", i_ProjectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                var tasks = await r_TaskService.ListBoardAsync(i_ProjectId, query);

                r_Logger.LogInformation("Board tasks retrieved successfully: ProjectId={ProjectId}, Count={Count}", i_ProjectId, tasks.Count);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting board tasks: ProjectId={ProjectId}", i_ProjectId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving board tasks." });
            }
        }

        #endregion

        #region Task CRUD Operations

        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="taskCreateDto">The task creation data.</param>
        /// <returns>The created task.</returns>
        [HttpPost]
        public async Task<ActionResult<ProjectTaskViewModel>> CreateTask(Guid i_ProjectId, [FromBody] ProjectTaskCreateDto taskCreateDto)
        {
            r_Logger.LogInformation("Creating task: ProjectId={ProjectId}, Title={Title}", i_ProjectId, taskCreateDto.Title);

            if (i_ProjectId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid project ID: ProjectId={ProjectId}", i_ProjectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for task creation: ProjectId={ProjectId}", i_ProjectId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.CreateAsync(i_ProjectId, taskCreateDto, userId);

                r_Logger.LogInformation("Task created successfully: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}", 
                    i_ProjectId, task.TaskId, task.Title);

                return CreatedAtAction(nameof(GetTask), new { i_ProjectId, i_TaskId = task.TaskId }, task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, Error={Error}", i_ProjectId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Resource not found: ProjectId={ProjectId}, Error={Error}", i_ProjectId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error creating task: ProjectId={ProjectId}, Title={Title}", i_ProjectId, taskCreateDto.Title);
                return StatusCode(500, new { Message = "An unexpected error occurred while creating the task." });
            }
        }

        /// <summary>
        /// Updates an existing task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="taskUpdateDto">The task update data.</param>
        /// <returns>The updated task.</returns>
        [HttpPut("{i_TaskId}")]
        public async Task<ActionResult<ProjectTaskViewModel>> UpdateTask(Guid i_ProjectId, Guid i_TaskId, [FromBody] ProjectTaskUpdateDto taskUpdateDto)
        {
            r_Logger.LogInformation("Updating task: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for task update: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.UpdateAsync(i_ProjectId, i_TaskId, taskUpdateDto, userId);

                r_Logger.LogInformation("Task updated successfully: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating task: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the task." });
            }
        }

        /// <summary>
        /// Deletes a task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{i_TaskId}")]
        public async Task<ActionResult> DeleteTask(Guid i_ProjectId, Guid i_TaskId)
        {
            r_Logger.LogInformation("Deleting task: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.DeleteAsync(i_ProjectId, i_TaskId, userId);

                r_Logger.LogInformation("Task deleted successfully: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting task: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting the task." });
            }
        }

        #endregion

        #region Task Status and Ordering

        /// <summary>
        /// Changes the status of a task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="newStatus">The new status.</param>
        /// <returns>The updated task.</returns>
        [HttpPut("{i_TaskId}/status")]
        public async Task<ActionResult<ProjectTaskViewModel>> ChangeTaskStatus(Guid i_ProjectId, Guid i_TaskId, [FromBody] eTaskStatus newStatus)
        {
            r_Logger.LogInformation("Changing task status: ProjectId={ProjectId}, TaskId={TaskId}, NewStatus={NewStatus}", i_ProjectId, i_TaskId, newStatus);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.ChangeStatusAsync(i_ProjectId, i_TaskId, newStatus, userId);

                r_Logger.LogInformation("Task status changed successfully: ProjectId={ProjectId}, TaskId={TaskId}, NewStatus={NewStatus}", 
                    i_ProjectId, i_TaskId, newStatus);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error changing task status: ProjectId={ProjectId}, TaskId={TaskId}, NewStatus={NewStatus}", 
                    i_ProjectId, i_TaskId, newStatus);
                return StatusCode(500, new { Message = "An unexpected error occurred while changing the task status." });
            }
        }

        /// <summary>
        /// Reorders a task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="newOrderIndex">The new order index.</param>
        /// <returns>The updated task.</returns>
        [HttpPut("{i_TaskId}/order")]
        public async Task<ActionResult<ProjectTaskViewModel>> ReorderTask(Guid i_ProjectId, Guid i_TaskId, [FromBody] int newOrderIndex)
        {
            r_Logger.LogInformation("Reordering task: ProjectId={ProjectId}, TaskId={TaskId}, NewOrderIndex={NewOrderIndex}", i_ProjectId, i_TaskId, newOrderIndex);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var task = await r_TaskService.ReorderAsync(i_ProjectId, i_TaskId, newOrderIndex, userId);

                r_Logger.LogInformation("Task reordered successfully: ProjectId={ProjectId}, TaskId={TaskId}, NewOrderIndex={NewOrderIndex}", 
                    i_ProjectId, i_TaskId, newOrderIndex);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error reordering task: ProjectId={ProjectId}, TaskId={TaskId}, NewOrderIndex={NewOrderIndex}", 
                    i_ProjectId, i_TaskId, newOrderIndex);
                return StatusCode(500, new { Message = "An unexpected error occurred while reordering the task." });
            }
        }

        #endregion

        #region Subtasks

        /// <summary>
        /// Gets all subtasks for a task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <returns>List of subtasks.</returns>
        [HttpGet("{i_TaskId}/subtasks")]
        public async Task<ActionResult<IReadOnlyList<ProjectSubtaskViewModel>>> GetSubtasks(Guid i_ProjectId, Guid i_TaskId)
        {
            r_Logger.LogInformation("Getting subtasks: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var subtasks = await r_TaskService.ListSubtasksAsync(i_ProjectId, i_TaskId);

                r_Logger.LogInformation("Subtasks retrieved successfully: ProjectId={ProjectId}, TaskId={TaskId}, Count={Count}", 
                    i_ProjectId, i_TaskId, subtasks.Count);
                return Ok(subtasks);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting subtasks: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving subtasks." });
            }
        }

        /// <summary>
        /// Adds a subtask to a task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="subtaskCreateDto">The subtask creation data.</param>
        /// <returns>The created subtask.</returns>
        [HttpPost("{i_TaskId}/subtasks")]
        public async Task<ActionResult<ProjectSubtaskViewModel>> AddSubtask(Guid i_ProjectId, Guid i_TaskId, [FromBody] SubtaskCreateDto subtaskCreateDto)
        {
            r_Logger.LogInformation("Adding subtask: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}", i_ProjectId, i_TaskId, subtaskCreateDto.Title);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for subtask creation: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var subtask = await r_TaskService.AddSubtaskAsync(i_ProjectId, i_TaskId, subtaskCreateDto, userId);

                r_Logger.LogInformation("Subtask added successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    i_ProjectId, i_TaskId, subtask.SubtaskId);
                return Ok(subtask);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error adding subtask: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}", 
                    i_ProjectId, i_TaskId, subtaskCreateDto.Title);
                return StatusCode(500, new { Message = "An unexpected error occurred while adding the subtask." });
            }
        }

        /// <summary>
        /// Updates a subtask.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="subtaskId">The subtask ID.</param>
        /// <param name="subtaskUpdateDto">The subtask update data.</param>
        /// <returns>The updated subtask.</returns>
        [HttpPut("{i_TaskId}/subtasks/{subtaskId}")]
        public async Task<ActionResult<ProjectSubtaskViewModel>> UpdateSubtask(Guid i_ProjectId, Guid i_TaskId, Guid subtaskId, [FromBody] SubtaskUpdateDto subtaskUpdateDto)
        {
            r_Logger.LogInformation("Updating subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", i_ProjectId, i_TaskId, subtaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty || subtaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", i_ProjectId, i_TaskId, subtaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and Subtask ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var subtask = await r_TaskService.UpdateSubtaskAsync(i_ProjectId, i_TaskId, subtaskId, subtaskUpdateDto, userId);

                r_Logger.LogInformation("Subtask updated successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    i_ProjectId, i_TaskId, subtaskId);
                return Ok(subtask);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    i_ProjectId, i_TaskId, subtaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Subtask not found: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    i_ProjectId, i_TaskId, subtaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    i_ProjectId, i_TaskId, subtaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while updating the subtask." });
            }
        }

        /// <summary>
        /// Removes a subtask.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="subtaskId">The subtask ID.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{i_TaskId}/subtasks/{subtaskId}")]
        public async Task<ActionResult> RemoveSubtask(Guid i_ProjectId, Guid i_TaskId, Guid subtaskId)
        {
            r_Logger.LogInformation("Removing subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", i_ProjectId, i_TaskId, subtaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty || subtaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", i_ProjectId, i_TaskId, subtaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and Subtask ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.RemoveSubtaskAsync(i_ProjectId, i_TaskId, subtaskId, userId);

                r_Logger.LogInformation("Subtask removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    i_ProjectId, i_TaskId, subtaskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    i_ProjectId, i_TaskId, subtaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Subtask not found: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    i_ProjectId, i_TaskId, subtaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error removing subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    i_ProjectId, i_TaskId, subtaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while removing the subtask." });
            }
        }

        /// <summary>
        /// Toggles the completion status of a subtask.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="subtaskId">The subtask ID.</param>
        /// <param name="isDone">Whether the subtask is done.</param>
        /// <returns>The updated subtask.</returns>
        [HttpPut("{i_TaskId}/subtasks/{subtaskId}/toggle")]
        public async Task<ActionResult<ProjectSubtaskViewModel>> ToggleSubtask(Guid i_ProjectId, Guid i_TaskId, Guid subtaskId, [FromBody] bool isDone)
        {
            r_Logger.LogInformation("Toggling subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}", 
                i_ProjectId, i_TaskId, subtaskId, isDone);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty || subtaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", i_ProjectId, i_TaskId, subtaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and Subtask ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var subtask = await r_TaskService.ToggleSubtaskAsync(i_ProjectId, i_TaskId, subtaskId, isDone, userId);

                r_Logger.LogInformation("Subtask toggled successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}", 
                    i_ProjectId, i_TaskId, subtaskId, isDone);
                return Ok(subtask);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    i_ProjectId, i_TaskId, subtaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Subtask not found: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, Error={Error}", 
                    i_ProjectId, i_TaskId, subtaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error toggling subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}", 
                    i_ProjectId, i_TaskId, subtaskId, isDone);
                return StatusCode(500, new { Message = "An unexpected error occurred while toggling the subtask." });
            }
        }

        #endregion

        #region Task Dependencies

        /// <summary>
        /// Adds a dependency between tasks.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="dependsOnTaskId">The ID of the task this task depends on.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("{i_TaskId}/dependencies")]
        public async Task<ActionResult> AddDependency(Guid i_ProjectId, Guid i_TaskId, [FromBody] Guid dependsOnTaskId)
        {
            r_Logger.LogInformation("Adding task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                i_ProjectId, i_TaskId, dependsOnTaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty || dependsOnTaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    i_ProjectId, i_TaskId, dependsOnTaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and DependsOn Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.AddDependencyAsync(i_ProjectId, i_TaskId, dependsOnTaskId, userId);

                r_Logger.LogInformation("Task dependency added successfully: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    i_ProjectId, i_TaskId, dependsOnTaskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                r_Logger.LogWarning("Invalid operation: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error adding task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    i_ProjectId, i_TaskId, dependsOnTaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while adding the task dependency." });
            }
        }

        /// <summary>
        /// Removes a dependency between tasks.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="dependsOnTaskId">The ID of the task this task depends on.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{i_TaskId}/dependencies/{dependsOnTaskId}")]
        public async Task<ActionResult> RemoveDependency(Guid i_ProjectId, Guid i_TaskId, Guid dependsOnTaskId)
        {
            r_Logger.LogInformation("Removing task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                i_ProjectId, i_TaskId, dependsOnTaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty || dependsOnTaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    i_ProjectId, i_TaskId, dependsOnTaskId);
                return BadRequest(new { Message = "Project ID, Task ID, and DependsOn Task ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.RemoveDependencyAsync(i_ProjectId, i_TaskId, dependsOnTaskId, userId);

                r_Logger.LogInformation("Task dependency removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    i_ProjectId, i_TaskId, dependsOnTaskId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Dependency not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error removing task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    i_ProjectId, i_TaskId, dependsOnTaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while removing the task dependency." });
            }
        }

        #endregion

        #region Task References

        /// <summary>
        /// Gets all references for a task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <returns>List of task references.</returns>
        [HttpGet("{i_TaskId}/references")]
        public async Task<ActionResult<IReadOnlyList<ProjectTaskReferenceViewModel>>> GetReferences(Guid i_ProjectId, Guid i_TaskId)
        {
            r_Logger.LogInformation("Getting task references: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            try
            {
                var references = await r_TaskService.ListReferencesAsync(i_ProjectId, i_TaskId);

                r_Logger.LogInformation("Task references retrieved successfully: ProjectId={ProjectId}, TaskId={TaskId}, Count={Count}", 
                    i_ProjectId, i_TaskId, references.Count);
                return Ok(references);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting task references: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving task references." });
            }
        }

        /// <summary>
        /// Adds a reference to a task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="referenceCreateDto">The reference creation data.</param>
        /// <returns>The created reference.</returns>
        [HttpPost("{i_TaskId}/references")]
        public async Task<ActionResult<ProjectTaskReferenceViewModel>> AddReference(Guid i_ProjectId, Guid i_TaskId, [FromBody] TaskReferenceCreateDto referenceCreateDto)
        {
            r_Logger.LogInformation("Adding task reference: ProjectId={ProjectId}, TaskId={TaskId}, Type={Type}, Url={Url}", 
                i_ProjectId, i_TaskId, referenceCreateDto.Type, referenceCreateDto.Url);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(new { Message = "Project ID and Task ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                r_Logger.LogWarning("Invalid model state for reference creation: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                var reference = await r_TaskService.AddReferenceAsync(i_ProjectId, i_TaskId, referenceCreateDto, userId);

                r_Logger.LogInformation("Task reference added successfully: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    i_ProjectId, i_TaskId, reference.ReferenceId);
                return Ok(reference);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error adding task reference: ProjectId={ProjectId}, TaskId={TaskId}, Type={Type}, Url={Url}", 
                    i_ProjectId, i_TaskId, referenceCreateDto.Type, referenceCreateDto.Url);
                return StatusCode(500, new { Message = "An unexpected error occurred while adding the task reference." });
            }
        }

        /// <summary>
        /// Removes a reference from a task.
        /// </summary>
        /// <param name="i_ProjectId">The project ID.</param>
        /// <param name="i_TaskId">The task ID.</param>
        /// <param name="referenceId">The reference ID.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{i_TaskId}/references/{referenceId}")]
        public async Task<ActionResult> RemoveReference(Guid i_ProjectId, Guid i_TaskId, Guid referenceId)
        {
            r_Logger.LogInformation("Removing task reference: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                i_ProjectId, i_TaskId, referenceId);

            if (i_ProjectId == Guid.Empty || i_TaskId == Guid.Empty || referenceId == Guid.Empty)
            {
                r_Logger.LogWarning("Invalid parameters: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    i_ProjectId, i_TaskId, referenceId);
                return BadRequest(new { Message = "Project ID, Task ID, and Reference ID cannot be empty." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await r_TaskService.RemoveReferenceAsync(i_ProjectId, i_TaskId, referenceId, userId);

                r_Logger.LogInformation("Task reference removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    i_ProjectId, i_TaskId, referenceId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, TaskId={TaskId}, Error={Error}", i_ProjectId, i_TaskId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Reference not found: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}, Error={Error}", 
                    i_ProjectId, i_TaskId, referenceId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error removing task reference: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    i_ProjectId, i_TaskId, referenceId);
                return StatusCode(500, new { Message = "An unexpected error occurred while removing the task reference." });
            }
        }

        #endregion

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
