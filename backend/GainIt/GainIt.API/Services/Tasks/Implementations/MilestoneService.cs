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
    public class MilestoneService : IMilestoneService
    {   
        private readonly GainItDbContext r_Db;
        private readonly ITaskNotificationService r_Notifications;
        private readonly ILogger<MilestoneService> r_Logger;

        public MilestoneService(
            GainItDbContext i_Db, 
            ITaskNotificationService i_Notifications,
            ILogger<MilestoneService> i_Logger)
        {
            r_Db = i_Db;
            r_Notifications = i_Notifications;
            r_Logger = i_Logger;
        }

        public async Task<ProjectMilestoneViewModel> CreateAsync(Guid i_ProjectId, ProjectMilestoneCreateDto i_MilestoneCreateRequest, Guid i_ActorUserId)
        {
            r_Logger.LogInformation("Creating milestone: ProjectId={ProjectId}, Title={Title}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_MilestoneCreateRequest.Title, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var project = await r_Db.Projects
                    .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId)
                    ?? throw new KeyNotFoundException("Project not found.");

                var entity = new ProjectMilestone
                {
                    ProjectId = i_ProjectId,
                    Project = project,                      
                    Title = i_MilestoneCreateRequest.Title,
                    Description = i_MilestoneCreateRequest.Description,
                    Status = i_MilestoneCreateRequest.Status
                };

                r_Db.ProjectMilestones.Add(entity);
                await r_Db.SaveChangesAsync();

                var viewModel = new ProjectMilestoneViewModel
                {
                    MilestoneId = entity.MilestoneId,
                    Title = entity.Title,
                    Description = entity.Description,
                    Status = entity.Status,
                    OrderIndex = entity.OrderIndex,
                    TargetDateUtc = entity.TargetDateUtc,
                    TasksCount = 0,
                    DoneTasksCount = 0
                };

                r_Logger.LogInformation("Milestone created successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}, Title={Title}", 
                    i_ProjectId, entity.MilestoneId, entity.Title);

                return viewModel;
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error creating milestone: ProjectId={ProjectId}, Title={Title}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_MilestoneCreateRequest.Title, i_ActorUserId);
                throw;
            }
        }


        public async Task DeleteAsync(Guid i_ProjectId, Guid i_MilestoneId, Guid i_ActorUserId)
        {
            r_Logger.LogInformation("Deleting milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_MilestoneId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var entity = await r_Db.ProjectMilestones
                    .FirstOrDefaultAsync(m => m.ProjectId == i_ProjectId && m.MilestoneId == i_MilestoneId)
                    ?? throw new KeyNotFoundException("Milestone not found.");

                var tasks = await r_Db.ProjectTasks
                    .Where(t => t.ProjectId == i_ProjectId && t.MilestoneId == i_MilestoneId)
                    .ToListAsync();

                foreach (var t in tasks)
                    t.MilestoneId = null;

                r_Db.ProjectMilestones.Remove(entity);
                await r_Db.SaveChangesAsync();

                r_Logger.LogInformation("Milestone deleted successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}", 
                    i_ProjectId, i_MilestoneId);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_MilestoneId, i_ActorUserId);
                throw;
            }
        }


        public async Task<IReadOnlyList<ProjectMilestoneViewModel>> GetMilestonesListAsync(Guid i_ProjectId, Guid i_ActorUserId)
        {
            r_Logger.LogInformation("Getting milestones list: ProjectId={ProjectId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_ActorUserId);

            try
            {
                // Only members can view
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);

                var list = await r_Db.ProjectMilestones
                    .Where(m => m.ProjectId == i_ProjectId)
                    .Select(m => new ProjectMilestoneViewModel
                    {
                        MilestoneId = m.MilestoneId,
                        Title = m.Title,
                        Description = m.Description,
                        Status = m.Status,
                        OrderIndex = m.OrderIndex,
                        TargetDateUtc = m.TargetDateUtc,
                        TasksCount = r_Db.ProjectTasks.Count(t => t.ProjectId == i_ProjectId && t.MilestoneId == m.MilestoneId),
                        DoneTasksCount = r_Db.ProjectTasks.Count(t => t.ProjectId == i_ProjectId && t.MilestoneId == m.MilestoneId && t.Status == eTaskStatus.Done)
                    })
                    .OrderBy(vm => vm.OrderIndex)
                    .ThenBy(vm => vm.Title)
                    .ToListAsync();

                r_Logger.LogInformation("Milestones list retrieved: ProjectId={ProjectId}, Count={Count}", 
                    i_ProjectId, list.Count);

                return list;
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting milestones list: ProjectId={ProjectId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_ActorUserId);
                throw;
            }
        }

        public async Task<ProjectMilestoneViewModel?> GetMilestoneAsync(Guid i_ProjectId, Guid i_MilestoneId, Guid i_ActorUserId)
        {
            r_Logger.LogInformation("Getting milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_MilestoneId, i_ActorUserId);

            try
            {
                // Only members can view
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);

                var milestone = await r_Db.ProjectMilestones
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ProjectId == i_ProjectId && m.MilestoneId == i_MilestoneId);

                if (milestone == null)
                {
                    r_Logger.LogWarning("Milestone not found: ProjectId={ProjectId}, MilestoneId={MilestoneId}", 
                        i_ProjectId, i_MilestoneId);
                    return null;
                }

                var tasksCount = await r_Db.ProjectTasks.CountAsync(t => t.ProjectId == i_ProjectId && t.MilestoneId == milestone.MilestoneId);
                var doneCount = await r_Db.ProjectTasks.CountAsync(t => t.ProjectId == i_ProjectId && t.MilestoneId == milestone.MilestoneId && t.Status == eTaskStatus.Done);

                r_Logger.LogInformation("Milestone retrieved successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}", 
                    i_ProjectId, i_MilestoneId);

                return new ProjectMilestoneViewModel
                {
                    MilestoneId = milestone.MilestoneId,
                    Title = milestone.Title,
                    Description = milestone.Description,
                    Status = milestone.Status,
                    OrderIndex = milestone.OrderIndex,
                    TargetDateUtc = milestone.TargetDateUtc,
                    TasksCount = tasksCount,
                    DoneTasksCount = doneCount
                };
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_MilestoneId, i_ActorUserId);
                throw;
            }
        }

        public async Task<ProjectMilestoneViewModel> UpdateAsync(Guid i_ProjectId, Guid i_MilestoneId, ProjectMilestoneUpdateDto i_MilestoneUpdateRequest, Guid i_ActorUserId)
        {
            r_Logger.LogInformation("Updating milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_MilestoneId, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var entity = await r_Db.ProjectMilestones
                    .FirstOrDefaultAsync(m => m.ProjectId == i_ProjectId && m.MilestoneId == i_MilestoneId)
                    ?? throw new KeyNotFoundException("Milestone not found.");

                var oldStatus = entity.Status;

                if (i_MilestoneUpdateRequest.Title is { Length: > 0 })
                    entity.Title = i_MilestoneUpdateRequest.Title!;
                if (i_MilestoneUpdateRequest.Description != null)
                    entity.Description = i_MilestoneUpdateRequest.Description;
                if (i_MilestoneUpdateRequest.Status.HasValue)
                    entity.Status = i_MilestoneUpdateRequest.Status.Value;

                await r_Db.SaveChangesAsync();

                var tasksCount = await r_Db.ProjectTasks.CountAsync(t => t.ProjectId == i_ProjectId && t.MilestoneId == entity.MilestoneId);
                var doneCount = await r_Db.ProjectTasks.CountAsync(t => t.ProjectId == i_ProjectId && t.MilestoneId == entity.MilestoneId && t.Status == eTaskStatus.Done);

                var milestoneViewModel =  new ProjectMilestoneViewModel
                {
                    MilestoneId = entity.MilestoneId,
                    Title = entity.Title,
                    Description = entity.Description,
                    Status = entity.Status,
                    OrderIndex = entity.OrderIndex,
                    TargetDateUtc = entity.TargetDateUtc,
                    TasksCount = tasksCount,
                    DoneTasksCount = doneCount
                };

                // notify on first transition to Completed
                if (oldStatus != eMilestoneStatus.Completed && entity.Status == eMilestoneStatus.Completed)
                {
                    await r_Notifications.MilestoneCompletedAsync(i_ProjectId, milestoneViewModel);
                }

                r_Logger.LogInformation("Milestone updated successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}", 
                    i_ProjectId, i_MilestoneId);

                return milestoneViewModel;
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating milestone: ProjectId={ProjectId}, MilestoneId={MilestoneId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_MilestoneId, i_ActorUserId);
                throw;
            }
        }

        public async Task<ProjectMilestoneViewModel> ChangeStatusAsync(Guid i_ProjectId, Guid i_MilestoneId, eMilestoneStatus i_NewStatus, Guid i_ActorUserId)
        {
            r_Logger.LogInformation("Changing milestone status: ProjectId={ProjectId}, MilestoneId={MilestoneId}, NewStatus={NewStatus}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_MilestoneId, i_NewStatus, i_ActorUserId);

            try
            {
                await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);
                await ensureActorCanManagePlanningAsync(i_ProjectId, i_ActorUserId);

                var entity = await r_Db.ProjectMilestones
                    .FirstOrDefaultAsync(m => m.ProjectId == i_ProjectId && m.MilestoneId == i_MilestoneId)
                    ?? throw new KeyNotFoundException("Milestone not found.");

                var oldStatus = entity.Status;
                entity.Status = i_NewStatus;

                await r_Db.SaveChangesAsync();

                var tasksCount = await r_Db.ProjectTasks.CountAsync(t => t.ProjectId == i_ProjectId && t.MilestoneId == entity.MilestoneId);
                var doneCount = await r_Db.ProjectTasks.CountAsync(t => t.ProjectId == i_ProjectId && t.MilestoneId == entity.MilestoneId && t.Status == eTaskStatus.Done);

                var milestoneViewModel = new ProjectMilestoneViewModel
                {
                    MilestoneId = entity.MilestoneId,
                    Title = entity.Title,
                    Description = entity.Description,
                    Status = entity.Status,
                    OrderIndex = entity.OrderIndex,
                    TargetDateUtc = entity.TargetDateUtc,
                    TasksCount = tasksCount,
                    DoneTasksCount = doneCount
                };

                // notify on first transition to Completed
                if (oldStatus != eMilestoneStatus.Completed && entity.Status == eMilestoneStatus.Completed)
                {
                    await r_Notifications.MilestoneCompletedAsync(i_ProjectId, milestoneViewModel);
                }

                r_Logger.LogInformation("Milestone status changed successfully: ProjectId={ProjectId}, MilestoneId={MilestoneId}, OldStatus={OldStatus}, NewStatus={NewStatus}", 
                    i_ProjectId, i_MilestoneId, oldStatus, i_NewStatus);

                return milestoneViewModel;
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error changing milestone status: ProjectId={ProjectId}, MilestoneId={MilestoneId}, NewStatus={NewStatus}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_MilestoneId, i_NewStatus, i_ActorUserId);
                throw;
            }
        }

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

            // allow project-level admins
            var isProjectAdmin = await r_Db.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == i_ProjectId
                             && pm.UserId == i_ActorUserId
                             && pm.IsAdmin
                             && pm.LeftAtUtc == null);
            if (isProjectAdmin) return;

            throw new UnauthorizedAccessException("Only project admins or mentors can perform this action.");
        }
    }
}
