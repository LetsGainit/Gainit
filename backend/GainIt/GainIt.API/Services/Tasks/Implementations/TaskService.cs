using GainIt.API.Data;
using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Models.Enums.Tasks;
using GainIt.API.Models.Tasks;
using GainIt.API.Services.Tasks.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.Tasks.Implementations
{
    public class TaskService : ITaskService
    {
        private readonly GainItDbContext r_Db;
        private readonly ITaskNotificationService r_Notifications;
        private readonly ILogger<TaskService> r_Log;

        public TaskService(
            GainItDbContext db,
            ITaskNotificationService notifications,
            ILogger<TaskService> log)
        {
            r_Db = db;
            r_Notifications = notifications;
            r_Log = log;
        }

        public async Task<ProjectTaskViewModel?> GetTaskAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Getting task: ProjectId={ProjectId}, TaskId={TaskId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);

                var task = await r_Db.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Milestone)
                    .Include(t => t.Subtasks.OrderBy(s => s.OrderIndex))
                    .Include(t => t.References)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId);

                if (task == null)
                {
                    r_Log.LogWarning("Task not found: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                    return null;
                }

                var viewModel = await mapToTaskViewModelAsync(task);
                r_Log.LogInformation("Task retrieved successfully: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                return viewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error getting task: ProjectId={ProjectId}, TaskId={TaskId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_ActorUserId);
                throw;
            }
        }

        public async Task<IReadOnlyList<ProjectTaskListItemViewModel>> ListMyTasksAsync(Guid i_ProjectId, Guid i_UserId, TaskListQueryDto i_TaskListQuery)
        {
            r_Log.LogInformation("Listing my active tasks: ProjectId={ProjectId}, UserId={UserId}", i_ProjectId, i_UserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_UserId);

                var query = r_Db.ProjectTasks
                    .Include(t => t.Milestone)
                    .Include(t => t.Subtasks.OrderBy(s => s.OrderIndex))
                    .Where(t => t.ProjectId == i_ProjectId && 
                               t.Status != eTaskStatus.Done && t.Status != eTaskStatus.Blocked);

                // Apply sorting
                query = i_TaskListQuery.SortBy?.ToLower() switch
                {
                    "createdatutc" => i_TaskListQuery.SortDescending ? query.OrderByDescending(t => t.CreatedAtUtc) : query.OrderBy(t => t.CreatedAtUtc),
                    "dueatutc" => i_TaskListQuery.SortDescending ? query.OrderByDescending(t => t.DueAtUtc) : query.OrderBy(t => t.DueAtUtc),
                    "priority" => i_TaskListQuery.SortDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
                    _ => i_TaskListQuery.SortDescending ? query.OrderByDescending(t => t.OrderIndex) : query.OrderBy(t => t.OrderIndex)
                };

                var tasks = await query.AsNoTracking().ToListAsync();
                var viewModels = tasks.Select(mapToTaskListItemViewModel).ToList();

                r_Log.LogInformation("Active tasks retrieved: ProjectId={ProjectId}, UserId={UserId}, Count={Count}", 
                    i_ProjectId, i_UserId, viewModels.Count);
                return viewModels;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error listing my tasks: ProjectId={ProjectId}, UserId={UserId}", i_ProjectId, i_UserId);
                throw;
            }
        }

        public async Task<IReadOnlyList<ProjectTaskListItemViewModel>> ListBoardAsync(Guid i_ProjectId, TaskBoardQueryDto i_TaskBoardQuery)
        {
            r_Log.LogInformation("Listing board tasks: ProjectId={ProjectId}, IncludeCompleted={IncludeCompleted}", 
                i_ProjectId, i_TaskBoardQuery.IncludeCompleted);

            try
            {
                var query = r_Db.ProjectTasks
                    .Include(t => t.Milestone)
                    .Where(t => t.ProjectId == i_ProjectId);

                // Apply filters
                if (i_TaskBoardQuery.Type.HasValue)
                    query = query.Where(t => t.Type == i_TaskBoardQuery.Type.Value);

                if (i_TaskBoardQuery.Priority.HasValue)
                    query = query.Where(t => t.Priority == i_TaskBoardQuery.Priority.Value);

                if (i_TaskBoardQuery.MilestoneId.HasValue)
                    query = query.Where(t => t.MilestoneId == i_TaskBoardQuery.MilestoneId.Value);

                if (!string.IsNullOrEmpty(i_TaskBoardQuery.AssignedRole))
                    query = query.Where(t => t.AssignedRole == i_TaskBoardQuery.AssignedRole);

                if (i_TaskBoardQuery.AssignedUserId.HasValue)
                    query = query.Where(t => t.AssignedUserId == i_TaskBoardQuery.AssignedUserId.Value);

                if (i_TaskBoardQuery.IsBlocked.HasValue)
                    query = query.Where(t => t.IsBlocked == i_TaskBoardQuery.IsBlocked.Value);

                if (!string.IsNullOrEmpty(i_TaskBoardQuery.SearchTerm))
                {
                    var searchTerm = i_TaskBoardQuery.SearchTerm.ToLower();
                    query = query.Where(t => t.Title.ToLower().Contains(searchTerm) || 
                                           (t.Description != null && t.Description.ToLower().Contains(searchTerm)));
                }

                // Filter out completed tasks unless explicitly requested
                if (!i_TaskBoardQuery.IncludeCompleted)
                    query = query.Where(t => t.Status != eTaskStatus.Done);

                // Apply sorting
                query = i_TaskBoardQuery.SortBy?.ToLower() switch
                {
                    "createdatutc" => i_TaskBoardQuery.SortDescending ? query.OrderByDescending(t => t.CreatedAtUtc) : query.OrderBy(t => t.CreatedAtUtc),
                    "dueatutc" => i_TaskBoardQuery.SortDescending ? query.OrderByDescending(t => t.DueAtUtc) : query.OrderBy(t => t.DueAtUtc),
                    "priority" => i_TaskBoardQuery.SortDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
                    _ => i_TaskBoardQuery.SortDescending ? query.OrderByDescending(t => t.OrderIndex) : query.OrderBy(t => t.OrderIndex)
                };

                var tasks = await query.AsNoTracking().ToListAsync();
                var viewModels = tasks.Select(mapToTaskListItemViewModel).ToList();

                r_Log.LogInformation("Board tasks retrieved: ProjectId={ProjectId}, Count={Count}", i_ProjectId, viewModels.Count);
                return viewModels;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error listing board tasks: ProjectId={ProjectId}", i_ProjectId);
                throw;
            }
        }

        public async Task<ProjectTaskViewModel> CreateAsync(Guid i_ProjectId, ProjectTaskCreateDto i_TaskCreateModel, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Creating task: ProjectId={ProjectId}, Title={Title}, Type={Type}, Priority={Priority}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskCreateModel.Title, i_TaskCreateModel.Type, i_TaskCreateModel.Priority, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var project = await r_Db.Projects
                    .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId)
                    ?? throw new KeyNotFoundException("Project not found.");

                // Determine order index
                var maxOrderIndex = await r_Db.ProjectTasks
                    .Where(t => t.ProjectId == i_ProjectId)
                    .MaxAsync(t => (int?)t.OrderIndex) ?? -1;

                var task = new ProjectTask
                {
                    ProjectId = i_ProjectId,
                    Project = project,
                    Title = i_TaskCreateModel.Title,
                    Description = i_TaskCreateModel.Description,
                    Type = i_TaskCreateModel.Type,
                    Priority = i_TaskCreateModel.Priority,
                    DueAtUtc = i_TaskCreateModel.DueAtUtc,
                    MilestoneId = i_TaskCreateModel.MilestoneId,
                    AssignedRole = i_TaskCreateModel.AssignedRole,
                    AssignedUserId = i_TaskCreateModel.AssignedUserId,
                    OrderIndex = i_TaskCreateModel.OrderIndex ?? (maxOrderIndex + 1),
                    CreatedByUserId = i_ActorUserId,
                    Subtasks = new List<ProjectSubtask>()
                };

                r_Db.ProjectTasks.Add(task);

                // Create subtasks if provided
                if (i_TaskCreateModel.Subtasks != null && i_TaskCreateModel.Subtasks.Any())
                {
                    foreach (var subtaskModel in i_TaskCreateModel.Subtasks)
                    {
                        var subtask = new ProjectSubtask
                        {
                            TaskId = task.TaskId,
                            Task = task,
                            Title = subtaskModel.Title,
                            Description = subtaskModel.Description,
                            OrderIndex = subtaskModel.OrderIndex ?? 0,
                            IsDone = false,
                            CreatedByUserId = i_ActorUserId
                        };
                        task.Subtasks.Add(subtask);
                    }
                }

                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Task created successfully: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}, SubtaskCount={SubtaskCount}", 
                    i_ProjectId, task.TaskId, task.Title, task.Subtasks.Count);

                // Send notification
                var taskViewModel = await mapToTaskViewModelAsync(task);
                await r_Notifications.TaskCreatedAsync(i_ProjectId, taskViewModel);

                return taskViewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error creating task: ProjectId={ProjectId}, Title={Title}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskCreateModel.Title, i_ActorUserId);
                throw;
            }
        }

        public async Task<ProjectTaskViewModel> UpdateAsync(Guid i_ProjectId, Guid i_TaskId, ProjectTaskUpdateDto i_TaskUpdateModel, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Updating task: ProjectId={ProjectId}, TaskId={TaskId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var task = await r_Db.ProjectTasks
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId)
                    ?? throw new KeyNotFoundException("Task not found.");

                // Update fields if provided
                if (!string.IsNullOrEmpty(i_TaskUpdateModel.Title))
                    task.Title = i_TaskUpdateModel.Title;

                if (i_TaskUpdateModel.Description != null)
                    task.Description = i_TaskUpdateModel.Description;

                if (i_TaskUpdateModel.Type.HasValue)
                    task.Type = i_TaskUpdateModel.Type.Value;

                if (i_TaskUpdateModel.Priority.HasValue)
                    task.Priority = i_TaskUpdateModel.Priority.Value;

                if (i_TaskUpdateModel.DueAtUtc != null)
                    task.DueAtUtc = i_TaskUpdateModel.DueAtUtc;

                if (i_TaskUpdateModel.MilestoneId != null)
                    task.MilestoneId = i_TaskUpdateModel.MilestoneId;

                if (i_TaskUpdateModel.AssignedRole != null)
                    task.AssignedRole = i_TaskUpdateModel.AssignedRole;

                if (i_TaskUpdateModel.AssignedUserId != null)
                    task.AssignedUserId = i_TaskUpdateModel.AssignedUserId;

                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Task updated successfully: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

                var taskViewModel = await mapToTaskViewModelAsync(task);
                return taskViewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error updating task: ProjectId={ProjectId}, TaskId={TaskId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_ActorUserId);
                throw;
            }
        }

        public async Task DeleteAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Deleting task: ProjectId={ProjectId}, TaskId={TaskId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var task = await r_Db.ProjectTasks
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId)
                    ?? throw new KeyNotFoundException("Task not found.");

                r_Db.ProjectTasks.Remove(task);
                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Task deleted successfully: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error deleting task: ProjectId={ProjectId}, TaskId={TaskId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_ActorUserId);
                throw;
            }
        }

        public async Task<ProjectTaskViewModel> ChangeStatusAsync(Guid i_ProjectId, Guid i_TaskId, eTaskStatus i_NewStatus, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Changing task status: ProjectId={ProjectId}, TaskId={TaskId}, NewStatus={NewStatus}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_NewStatus, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);

                var task = await r_Db.ProjectTasks
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId)
                    ?? throw new KeyNotFoundException("Task not found.");

                var oldStatus = task.Status;
                task.Status = i_NewStatus;

                // Handle blocked status
                if (i_NewStatus == eTaskStatus.Blocked)
                    task.IsBlocked = true;
                else if (oldStatus == eTaskStatus.Blocked && i_NewStatus != eTaskStatus.Blocked)
                {
                    task.IsBlocked = false;
                    await r_Notifications.TaskUnblockedAsync(i_ProjectId, i_TaskId);
                }

                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Task status changed: ProjectId={ProjectId}, TaskId={TaskId}, OldStatus={OldStatus}, NewStatus={NewStatus}", 
                    i_ProjectId, i_TaskId, oldStatus, i_NewStatus);

                // Send notification for completion
                if (oldStatus != eTaskStatus.Done && i_NewStatus == eTaskStatus.Done)
                {
                    var taskViewModel = await mapToTaskViewModelAsync(task);
                    await r_Notifications.TaskCompletedAsync(i_ProjectId, i_TaskId, oldStatus.ToString(), i_NewStatus.ToString());
                }

                var resultViewModel = await mapToTaskViewModelAsync(task);
                return resultViewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error changing task status: ProjectId={ProjectId}, TaskId={TaskId}, NewStatus={NewStatus}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_NewStatus, i_ActorUserId);
                throw;
            }
        }

        public async Task<ProjectTaskViewModel> ReorderAsync(Guid i_ProjectId, Guid i_TaskId, int i_NewOrderIndex, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Reordering task: ProjectId={ProjectId}, TaskId={TaskId}, NewOrderIndex={NewOrderIndex}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_NewOrderIndex, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var task = await r_Db.ProjectTasks
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId)
                    ?? throw new KeyNotFoundException("Task not found.");

                var oldOrderIndex = task.OrderIndex;
                task.OrderIndex = i_NewOrderIndex;

                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Task reordered: ProjectId={ProjectId}, TaskId={TaskId}, OldOrderIndex={OldOrderIndex}, NewOrderIndex={NewOrderIndex}", 
                    i_ProjectId, i_TaskId, oldOrderIndex, i_NewOrderIndex);

                var taskViewModel = await mapToTaskViewModelAsync(task);
                return taskViewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error reordering task: ProjectId={ProjectId}, TaskId={TaskId}, NewOrderIndex={NewOrderIndex}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_NewOrderIndex, i_ActorUserId);
                throw;
            }
        }

        // Helper methods for authorization and mapping
        private async Task ensureActorInProjectAsync(Guid i_ProjectId, Guid i_ActorUserId)
        {
            var isMember = await r_Db.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_ActorUserId && pm.LeftAtUtc == null);

            if (!isMember)
                throw new UnauthorizedAccessException("User is not a member of the project.");
        }

        private async Task ensureActorCanManagePlanningAsync(Guid i_ProjectId, Guid i_ActorUserId)
        {
            var isMentor = await r_Db.Mentors.AnyAsync(m => m.UserId == i_ActorUserId);
            if (isMentor) return;

            var isProjectAdmin = await r_Db.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_ActorUserId && pm.IsAdmin && pm.LeftAtUtc == null);
            
            if (isProjectAdmin) return;

            throw new UnauthorizedAccessException("Only project admins or mentors can perform this action.");
        }

        private async Task<ProjectTaskViewModel> mapToTaskViewModelAsync(ProjectTask task)
        {
            var viewModel = new ProjectTaskViewModel
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                Type = task.Type,
                IsBlocked = task.IsBlocked,
                OrderIndex = task.OrderIndex,
                CreatedAtUtc = task.CreatedAtUtc,
                DueAtUtc = task.DueAtUtc,
                AssignedRole = task.AssignedRole,
                AssignedUserId = task.AssignedUserId,
                MilestoneId = task.MilestoneId,
                MilestoneTitle = task.Milestone?.Title,
                Subtasks = task.Subtasks.Select(s => new ProjectSubtaskViewModel
                {
                    SubtaskId = s.SubtaskId,
                    Title = s.Title,
                    Description = s.Description,
                    IsDone = s.IsDone,
                    OrderIndex = s.OrderIndex,
                    CompletedAtUtc = s.CompletedAtUtc
                }).ToList(),
                References = task.References.Select(r => new ProjectTaskReferenceViewModel
                {
                    ReferenceId = r.ReferenceId,
                    Type = r.Type,
                    Url = r.Url,
                    Title = r.Title,
                    CreatedAtUtc = r.CreatedAtUtc
                }).ToList(),
                Dependencies = task.Dependencies.Select(d => new TaskDependencyViewModel
                {
                    TaskId = d.TaskId,
                    DependsOnTaskId = d.DependsOnTaskId,
                    DependsOnTitle = d.DependsOn.Title,
                    DependsOnStatus = d.DependsOn.Status
                }).ToList()
            };

            return viewModel;
        }

        private ProjectTaskListItemViewModel mapToTaskListItemViewModel(ProjectTask task)
        {
            return new ProjectTaskListItemViewModel
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Status = task.Status,
                Priority = task.Priority,
                Type = task.Type,
                IsBlocked = task.IsBlocked,
                OrderIndex = task.OrderIndex,
                CreatedAtUtc = task.CreatedAtUtc,
                DueAtUtc = task.DueAtUtc,
                AssignedRole = task.AssignedRole,
                AssignedUserId = task.AssignedUserId,
                MilestoneId = task.MilestoneId,
                MilestoneTitle = task.Milestone?.Title,
                SubtaskCount = task.Subtasks?.Count ?? 0,
                CompletedSubtaskCount = task.Subtasks?.Count(s => s.IsDone) ?? 0
            };
        }

        // Dependencies
        public async Task AddDependencyAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_DependsOnTaskId, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Adding task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_DependsOnTaskId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                // Validate both tasks exist and belong to the project
                var task = await r_Db.ProjectTasks
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId)
                    ?? throw new KeyNotFoundException("Task not found.");

                var dependsOnTask = await r_Db.ProjectTasks
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_DependsOnTaskId)
                    ?? throw new KeyNotFoundException("Dependency task not found.");

                // Check for circular dependencies
                if (i_TaskId == i_DependsOnTaskId)
                    throw new InvalidOperationException("A task cannot depend on itself.");

                // Check if dependency already exists
                var existingDependency = await r_Db.TaskDependencies
                    .FirstOrDefaultAsync(d => d.TaskId == i_TaskId && d.DependsOnTaskId == i_DependsOnTaskId);

                if (existingDependency != null)
                    throw new InvalidOperationException("Dependency already exists.");

                var dependency = new TaskDependency
                {
                    TaskId = i_TaskId,
                    Task = task,
                    DependsOnTaskId = i_DependsOnTaskId,
                    DependsOn = dependsOnTask
                };

                r_Db.TaskDependencies.Add(dependency);
                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Task dependency added successfully: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    i_ProjectId, i_TaskId, i_DependsOnTaskId);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error adding task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_DependsOnTaskId, i_ActorUserId);
                throw;
            }
        }

        public async Task RemoveDependencyAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_DependsOnTaskId, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Removing task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_DependsOnTaskId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var dependency = await r_Db.TaskDependencies
                    .FirstOrDefaultAsync(d => d.TaskId == i_TaskId && d.DependsOnTaskId == i_DependsOnTaskId)
                    ?? throw new KeyNotFoundException("Dependency not found.");

                r_Db.TaskDependencies.Remove(dependency);
                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Task dependency removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}", 
                    i_ProjectId, i_TaskId, i_DependsOnTaskId);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error removing task dependency: ProjectId={ProjectId}, TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_DependsOnTaskId, i_ActorUserId);
                throw;
            }
        }

        // Subtasks
        public async Task<IReadOnlyList<ProjectSubtaskViewModel>> ListSubtasksAsync(Guid i_ProjectId, Guid i_TaskId)
        {
            r_Log.LogInformation("Listing subtasks: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

            try
            {
                var subtasks = await r_Db.ProjectSubtasks
                    .Where(s => s.Task.ProjectId == i_ProjectId && s.TaskId == i_TaskId)
                    .OrderBy(s => s.OrderIndex)
                    .AsNoTracking()
                    .ToListAsync();

                var viewModels = subtasks.Select(s => new ProjectSubtaskViewModel
                {
                    SubtaskId = s.SubtaskId,
                    Title = s.Title,
                    Description = s.Description,
                    IsDone = s.IsDone,
                    OrderIndex = s.OrderIndex,
                    CompletedAtUtc = s.CompletedAtUtc
                }).ToList();

                r_Log.LogInformation("Subtasks retrieved: ProjectId={ProjectId}, TaskId={TaskId}, Count={Count}", i_ProjectId, i_TaskId, viewModels.Count);
                return viewModels;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error listing subtasks: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                throw;
            }
        }

        public async Task<ProjectSubtaskViewModel> AddSubtaskAsync(Guid i_ProjectId, Guid i_TaskId, SubtaskCreateDto i_SubtaskCreateModel, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Adding subtask: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_SubtaskCreateModel.Title, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var task = await r_Db.ProjectTasks
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId)
                    ?? throw new KeyNotFoundException("Task not found.");

                var maxOrderIndex = await r_Db.ProjectSubtasks
                    .Where(s => s.TaskId == i_TaskId)
                    .MaxAsync(s => (int?)s.OrderIndex) ?? -1;

                var subtask = new ProjectSubtask
                {
                    TaskId = i_TaskId,
                    Task = task,
                    Title = i_SubtaskCreateModel.Title,
                    Description = i_SubtaskCreateModel.Description,
                    OrderIndex = maxOrderIndex + 1
                };

                r_Db.ProjectSubtasks.Add(subtask);
                await r_Db.SaveChangesAsync();

                var viewModel = new ProjectSubtaskViewModel
                {
                    SubtaskId = subtask.SubtaskId,
                    Title = subtask.Title,
                    Description = subtask.Description,
                    IsDone = subtask.IsDone,
                    OrderIndex = subtask.OrderIndex,
                    CompletedAtUtc = subtask.CompletedAtUtc
                };

                r_Log.LogInformation("Subtask added successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    i_ProjectId, i_TaskId, subtask.SubtaskId);

                return viewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error adding subtask: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_SubtaskCreateModel.Title, i_ActorUserId);
                throw;
            }
        }

        public async Task<ProjectSubtaskViewModel> UpdateSubtaskAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_SubtaskId, SubtaskUpdateDto i_SubtaskUpdateModel, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Updating subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_SubtaskId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var subtask = await r_Db.ProjectSubtasks
                    .FirstOrDefaultAsync(s => s.Task.ProjectId == i_ProjectId && s.TaskId == i_TaskId && s.SubtaskId == i_SubtaskId)
                    ?? throw new KeyNotFoundException("Subtask not found.");

                if (!string.IsNullOrEmpty(i_SubtaskUpdateModel.Title))
                    subtask.Title = i_SubtaskUpdateModel.Title;

                if (i_SubtaskUpdateModel.Description != null)
                    subtask.Description = i_SubtaskUpdateModel.Description;

                await r_Db.SaveChangesAsync();

                var viewModel = new ProjectSubtaskViewModel
                {
                    SubtaskId = subtask.SubtaskId,
                    Title = subtask.Title,
                    Description = subtask.Description,
                    IsDone = subtask.IsDone,
                    OrderIndex = subtask.OrderIndex,
                    CompletedAtUtc = subtask.CompletedAtUtc
                };

                r_Log.LogInformation("Subtask updated successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    i_ProjectId, i_TaskId, i_SubtaskId);

                return viewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error updating subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_SubtaskId, i_ActorUserId);
                throw;
            }
        }

        public async Task RemoveSubtaskAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_SubtaskId, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Removing subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_SubtaskId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var subtask = await r_Db.ProjectSubtasks
                    .FirstOrDefaultAsync(s => s.Task.ProjectId == i_ProjectId && s.TaskId == i_TaskId && s.SubtaskId == i_SubtaskId)
                    ?? throw new KeyNotFoundException("Subtask not found.");

                r_Db.ProjectSubtasks.Remove(subtask);
                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Subtask removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}", 
                    i_ProjectId, i_TaskId, i_SubtaskId);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error removing subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_SubtaskId, i_ActorUserId);
                throw;
            }
        }

        public async Task<ProjectSubtaskViewModel> ToggleSubtaskAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_SubtaskId, bool i_IsDone, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Toggling subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_SubtaskId, i_IsDone, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);

                var subtask = await r_Db.ProjectSubtasks
                    .FirstOrDefaultAsync(s => s.Task.ProjectId == i_ProjectId && s.TaskId == i_TaskId && s.SubtaskId == i_SubtaskId)
                    ?? throw new KeyNotFoundException("Subtask not found.");

                subtask.IsDone = i_IsDone;
                subtask.CompletedAtUtc = i_IsDone ? DateTime.UtcNow : null;

                await r_Db.SaveChangesAsync();

                var viewModel = new ProjectSubtaskViewModel
                {
                    SubtaskId = subtask.SubtaskId,
                    Title = subtask.Title,
                    Description = subtask.Description,
                    IsDone = subtask.IsDone,
                    OrderIndex = subtask.OrderIndex,
                    CompletedAtUtc = subtask.CompletedAtUtc
                };

                r_Log.LogInformation("Subtask toggled successfully: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}", 
                    i_ProjectId, i_TaskId, i_SubtaskId, i_IsDone);

                return viewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error toggling subtask: ProjectId={ProjectId}, TaskId={TaskId}, SubtaskId={SubtaskId}, IsDone={IsDone}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_SubtaskId, i_IsDone, i_ActorUserId);
                throw;
            }
        }

        // References
        public async Task<IReadOnlyList<ProjectTaskReferenceViewModel>> ListReferencesAsync(Guid i_ProjectId, Guid i_TaskId)
        {
            r_Log.LogInformation("Listing task references: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);

            try
            {
                var references = await r_Db.ProjectTaskReferences
                    .Where(r => r.Task.ProjectId == i_ProjectId && r.TaskId == i_TaskId)
                    .OrderBy(r => r.CreatedAtUtc)
                    .AsNoTracking()
                    .ToListAsync();

                var viewModels = references.Select(r => new ProjectTaskReferenceViewModel
                {
                    ReferenceId = r.ReferenceId,
                    Type = r.Type,
                    Url = r.Url,
                    Title = r.Title,
                    CreatedAtUtc = r.CreatedAtUtc
                }).ToList();

                r_Log.LogInformation("Task references retrieved: ProjectId={ProjectId}, TaskId={TaskId}, Count={Count}", i_ProjectId, i_TaskId, viewModels.Count);
                return viewModels;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error listing task references: ProjectId={ProjectId}, TaskId={TaskId}", i_ProjectId, i_TaskId);
                throw;
            }
        }

        public async Task<ProjectTaskReferenceViewModel> AddReferenceAsync(Guid i_ProjectId, Guid i_TaskId, TaskReferenceCreateDto i_ReferenceCreateModel, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Adding task reference: ProjectId={ProjectId}, TaskId={TaskId}, Type={Type}, Url={Url}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_ReferenceCreateModel.Type, i_ReferenceCreateModel.Url, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var task = await r_Db.ProjectTasks
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId)
                    ?? throw new KeyNotFoundException("Task not found.");

                var reference = new ProjectTaskReference
                {
                    TaskId = i_TaskId,
                    Task = task,
                    Type = i_ReferenceCreateModel.Type,
                    Url = i_ReferenceCreateModel.Url,
                    Title = i_ReferenceCreateModel.Title
                };

                r_Db.ProjectTaskReferences.Add(reference);
                await r_Db.SaveChangesAsync();

                var viewModel = new ProjectTaskReferenceViewModel
                {
                    ReferenceId = reference.ReferenceId,
                    Type = reference.Type,
                    Url = reference.Url,
                    Title = reference.Title,
                    CreatedAtUtc = reference.CreatedAtUtc
                };

                r_Log.LogInformation("Task reference added successfully: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    i_ProjectId, i_TaskId, reference.ReferenceId);

                return viewModel;
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error adding task reference: ProjectId={ProjectId}, TaskId={TaskId}, Type={Type}, Url={Url}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_ReferenceCreateModel.Type, i_ReferenceCreateModel.Url, i_ActorUserId);
                throw;
            }
        }

        public async Task RemoveReferenceAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_ReferenceId, Guid i_ActorUserId)
        {
            r_Log.LogInformation("Removing task reference: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_ReferenceId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var reference = await r_Db.ProjectTaskReferences
                    .FirstOrDefaultAsync(r => r.Task.ProjectId == i_ProjectId && r.TaskId == i_TaskId && r.ReferenceId == i_ReferenceId)
                    ?? throw new KeyNotFoundException("Task reference not found.");

                r_Db.ProjectTaskReferences.Remove(reference);
                await r_Db.SaveChangesAsync();

                r_Log.LogInformation("Task reference removed successfully: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}", 
                    i_ProjectId, i_TaskId, i_ReferenceId);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error removing task reference: ProjectId={ProjectId}, TaskId={TaskId}, ReferenceId={ReferenceId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_ReferenceId, i_ActorUserId);
                throw;
            }
        }
    }
}
