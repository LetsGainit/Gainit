using GainIt.API.Data;
using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Models.Enums.Tasks;
using GainIt.API.Models.Tasks;
using GainIt.API.Services.Tasks.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GainIt.API.Services.Tasks.Implementations
{
    public class MilestoneService : IMilestoneService
    {   

        private readonly GainItDbContext r_Db;
        private readonly ITaskNotificationService r_Notifications;

        public MilestoneService(GainItDbContext i_Db, ITaskNotificationService i_Notifications)
        {
            r_Db = i_Db;
            r_Notifications = i_Notifications;
        }

        public async Task<ProjectMilestoneViewModel> CreateAsync( Guid i_ProjectId, ProjectMilestoneCreateDto i_MilestoneCreateRequest, Guid i_ActorUserId)
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

            return new ProjectMilestoneViewModel
            {
                MilestoneId = entity.MilestoneId,
                Title = entity.Title,
                Description = entity.Description,
                Status = entity.Status,
                TasksCount = 0,
                DoneTasksCount = 0
            };
        }


        public async Task DeleteAsync(Guid i_ProjectId, Guid i_MilestoneId, Guid i_ActorUserId)
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
        }


        public async Task<IReadOnlyList<ProjectMilestoneViewModel>> GetMilestionesListAsync(Guid i_ProjectId, Guid i_ActorUserId)
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
                    TasksCount = r_Db.ProjectTasks.Count(t => t.ProjectId == i_ProjectId && t.MilestoneId == m.MilestoneId),
                    DoneTasksCount = r_Db.ProjectTasks.Count(t => t.ProjectId == i_ProjectId && t.MilestoneId == m.MilestoneId && t.Status == eTaskStatus.Done)
                })
                .OrderBy(vm => vm.Title)
                .ToListAsync();

            return list;
        }

        public async Task<ProjectMilestoneViewModel?> GetMilestoneAsync(Guid i_ProjectId, Guid i_MilestoneId, Guid i_ActorUserId)
        {
            // Only members can view
            await ensureActorInProjectAsync(i_ProjectId, i_ActorUserId);

            var milestone = await r_Db.ProjectMilestones
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ProjectId == i_ProjectId && m.MilestoneId == i_MilestoneId);

            if (milestone == null) return null;

            var tasksCount = await r_Db.ProjectTasks.CountAsync(t => t.ProjectId == i_ProjectId && t.MilestoneId == milestone.MilestoneId);
            var doneCount = await r_Db.ProjectTasks.CountAsync(t => t.ProjectId == i_ProjectId && t.MilestoneId == milestone.MilestoneId && t.Status == eTaskStatus.Done);

            return new ProjectMilestoneViewModel
            {
                MilestoneId = milestone.MilestoneId,
                Title = milestone.Title,
                Description = milestone.Description,
                Status = milestone.Status,
                TasksCount = tasksCount,
                DoneTasksCount = doneCount
            };
        }

        public async Task<ProjectMilestoneViewModel> UpdateAsync(Guid i_ProjectId, Guid i_MilestoneId, ProjectMilestoneUpdateDto i_MilestoneUpdateRequest, Guid i_ActorUserId)
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
                TasksCount = tasksCount,
                DoneTasksCount = doneCount
            };

            // notify on first transition to Completed
            if (oldStatus != eMilestoneStatus.Completed && entity.Status == eMilestoneStatus.Completed)
            {
                await r_Notifications.MilestoneCompletedAsync(i_ProjectId, milestoneViewModel);
            }

            return milestoneViewModel;
        }

        private async Task ensureActorInProjectAsync(Guid i_ProjectId, Guid i_ActorUserId)
        {
            var isMember = await r_Db.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_ActorUserId && pm.LeftAtUtc == null);

            if (!isMember)
                throw new UnauthorizedAccessException("User is not a member of the project.");
        }
        private async Task ensureActorCanManagePlanningAsync(Guid projectId, Guid actorUserId)
        {
            var isMentor = await r_Db.Mentors.AnyAsync(m => m.UserId == actorUserId);
            if (isMentor) return;

            // allow project-level admins
            var isProjectAdmin = await r_Db.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId
                             && pm.UserId == actorUserId
                             && pm.IsAdmin
                             && pm.LeftAtUtc == null);
            if (isProjectAdmin) return;

            throw new UnauthorizedAccessException("Only project admins or mentors can perform this action.");
        }
    }
}
