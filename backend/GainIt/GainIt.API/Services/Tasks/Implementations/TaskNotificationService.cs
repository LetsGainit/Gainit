using GainIt.API.Data;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Models.Tasks;
using GainIt.API.Models.Projects;
using GainIt.API.Realtime;
using GainIt.API.Services.Email.Interfaces;
using GainIt.API.Services.Tasks.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.Tasks.Implementations
{
    public class TaskNotificationService : ITaskNotificationService
    {
        private readonly GainItDbContext r_Db;
        private readonly IEmailSender r_Email;
        private readonly IHubContext<NotificationsHub> r_Hub;
        private readonly ILogger<TaskNotificationService> r_Log;

        public TaskNotificationService(
            GainItDbContext db,
            IEmailSender email,
            IHubContext<NotificationsHub> hub,
            ILogger<TaskNotificationService> log)
        {
            r_Db = db;
            r_Email = email;
            r_Hub = hub;
            r_Log = log;
        }

        public async Task TaskCreatedAsync(Guid i_ProjectId, ProjectTaskViewModel i_ProjectTaskViewModel)
        {
            try
            {
                var project = await r_Db.Projects
                    .Include(p => p.ProjectMembers.Where(pm => pm.LeftAtUtc == null))
                    .ThenInclude(pm => pm.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

                if (project == null)
                {
                    r_Log.LogWarning("Project not found for task creation notification: ProjectId={ProjectId}", i_ProjectId);
                    return;
                }

                var task = await r_Db.ProjectTasks
                    .Include(t => t.Project)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TaskId == i_ProjectTaskViewModel.TaskId);

                if (task == null)
                {
                    r_Log.LogWarning("Task not found for creation notification: TaskId={TaskId}", i_ProjectTaskViewModel.TaskId);
                    return;
                }

                // Determine who to notify
                var membersToNotify = new List<ProjectMember>();

                // If task is assigned to a specific user, notify them
                if (task.AssignedUserId.HasValue)
                {
                    var assignedMember = project.ProjectMembers.FirstOrDefault(pm => pm.UserId == task.AssignedUserId.Value);
                    if (assignedMember != null)
                    {
                        membersToNotify.Add(assignedMember);
                    }
                }
                // If task is assigned to a role, notify all members with that role
                else if (!string.IsNullOrEmpty(task.AssignedRole))
                {
                    membersToNotify.AddRange(project.ProjectMembers.Where(pm => pm.UserRole == task.AssignedRole));
                }
                // If no specific assignment, notify project admins
                else
                {
                    membersToNotify.AddRange(project.ProjectMembers.Where(pm => pm.IsAdmin));
                }

                // Send realtime notifications
                foreach (var member in membersToNotify)
                {
                    await r_Hub.Clients.User(member.UserId.ToString())
                        .SendAsync(RealtimeEvents.Tasks.TaskCreated, new TaskCreatedNotificationDto
                        {
                            TaskId = task.TaskId,
                            ProjectId = task.ProjectId,
                            Title = task.Title,
                            Type = task.Type,
                            Priority = task.Priority,
                            AssignedRole = task.AssignedRole,
                            AssignedUserId = task.AssignedUserId,
                            ProjectName = project.ProjectName,
                            CreatedAtUtc = task.CreatedAtUtc
                        });
                }

                // Send email notifications
                foreach (var member in membersToNotify)
                {
                    var assignmentText = task.AssignedUserId.HasValue 
                        ? $"You have been assigned to a new task" 
                        : !string.IsNullOrEmpty(task.AssignedRole) 
                            ? $"A new task for your role '{task.AssignedRole}' has been created"
                            : "A new task has been created in your project";

                    await r_Email.SendAsync(
                        member.User.EmailAddress,
                        $"New task: {task.Title}",
                        $"Hi {member.User.FullName},\n\n{assignmentText} in project '{project.ProjectName}'.\n\nTask: {task.Title}\nType: {task.Type}\nPriority: {task.Priority}\n\nYou can view it in your project dashboard.",
                        $"Hi {member.User.FullName},<br/><br/>{assignmentText} in project <b>{project.ProjectName}</b>.<br/><br/><b>Task:</b> {task.Title}<br/><b>Type:</b> {task.Type}<br/><b>Priority:</b> {task.Priority}<br/><br/>You can view it in your project dashboard.",
                        "GainIt Notifications"
                    );
                }

                r_Log.LogInformation("Task creation notifications sent: TaskId={TaskId}, ProjectId={ProjectId}, NotifiedUsers={Count}", 
                    task.TaskId, i_ProjectId, membersToNotify.Count);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error sending task creation notifications: TaskId={TaskId}, ProjectId={ProjectId}", 
                    i_ProjectTaskViewModel.TaskId, i_ProjectId);
            }
        }

        public async Task TaskCompletedAsync(Guid i_ProjectId, Guid i_TaskId, string i_OldStatus, string i_NewStatus)
        {
            try
            {
                var task = await r_Db.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Project.ProjectMembers.Where(pm => pm.LeftAtUtc == null))
                    .ThenInclude(pm => pm.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TaskId == i_TaskId && t.ProjectId == i_ProjectId);

                if (task == null)
                {
                    r_Log.LogWarning("Task not found for completion notification: TaskId={TaskId}", i_TaskId);
                    return;
                }

                var project = task.Project;
                var allMembers = project.ProjectMembers.ToList();

                // Notify all project members about task completion
                foreach (var member in allMembers)
                {
                    await r_Hub.Clients.User(member.UserId.ToString())
                        .SendAsync(RealtimeEvents.Tasks.TaskCompleted, new TaskCompletedNotificationDto
                        {
                            TaskId = task.TaskId,
                            ProjectId = task.ProjectId,
                            Title = task.Title,
                            Type = task.Type,
                            AssignedRole = task.AssignedRole,
                            AssignedUserId = task.AssignedUserId,
                            ProjectName = project.ProjectName,
                            OldStatus = i_OldStatus,
                            NewStatus = i_NewStatus,
                            CompletedAtUtc = DateTime.UtcNow
                        });
                }

                // Send email to project admins and the assigned user (if different)
                var emailRecipients = new List<ProjectMember>();
                
                // Add project admins
                emailRecipients.AddRange(allMembers.Where(pm => pm.IsAdmin));
                
                // Add assigned user if they're not already in the list
                if (task.AssignedUserId.HasValue)
                {
                    var assignedMember = allMembers.FirstOrDefault(pm => pm.UserId == task.AssignedUserId.Value);
                    if (assignedMember != null && !emailRecipients.Any(r => r.UserId == assignedMember.UserId))
                    {
                        emailRecipients.Add(assignedMember);
                    }
                }

                foreach (var member in emailRecipients)
                {
                    var assignmentText = task.AssignedUserId.HasValue && task.AssignedUserId.Value == member.UserId
                        ? "You have completed a task"
                        : $"A task has been completed in your project";

                    await r_Email.SendAsync(
                        member.User.EmailAddress,
                        $"Task completed: {task.Title}",
                        $"Hi {member.User.FullName},\n\n{assignmentText} in project '{project.ProjectName}'.\n\nTask: {task.Title}\nType: {task.Type}\nPriority: {task.Priority}\n\nGreat work!",
                        $"Hi {member.User.FullName},<br/><br/>{assignmentText} in project <b>{project.ProjectName}</b>.<br/><br/><b>Task:</b> {task.Title}<br/><b>Type:</b> {task.Type}<br/><b>Priority:</b> {task.Priority}<br/><br/>Great work!",
                        "GainIt Notifications"
                    );
                }

                r_Log.LogInformation("Task completion notifications sent: TaskId={TaskId}, ProjectId={ProjectId}, NotifiedUsers={Count}", 
                    i_TaskId, i_ProjectId, allMembers.Count);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error sending task completion notifications: TaskId={TaskId}, ProjectId={ProjectId}", 
                    i_TaskId, i_ProjectId);
            }
        }

        public async Task TaskUnblockedAsync(Guid i_ProjectId, Guid i_TaskId)
        {
            try
            {
                var task = await r_Db.ProjectTasks
                    .Include(t => t.Project)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TaskId == i_TaskId && t.ProjectId == i_ProjectId);

                if (task == null)
                {
                    r_Log.LogWarning("Task not found for unblock notification: TaskId={TaskId}", i_TaskId);
                    return;
                }

                var project = task.Project;

                // Notify the assigned user or role members
                if (task.AssignedUserId.HasValue)
                {
                    // Send to specific assigned user
                    await r_Hub.Clients.User(task.AssignedUserId.Value.ToString())
                        .SendAsync(RealtimeEvents.Tasks.TaskUnblocked, new TaskUnblockedNotificationDto
                        {
                            TaskId = task.TaskId,
                            ProjectId = task.ProjectId,
                            Title = task.Title,
                            ProjectName = project.ProjectName,
                            UnblockedAtUtc = DateTime.UtcNow
                        });

                    var assignedUser = await r_Db.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == task.AssignedUserId.Value);

                    if (assignedUser != null)
                    {
                        await r_Email.SendAsync(
                            assignedUser.EmailAddress,
                            $"Task unblocked: {task.Title}",
                            $"Hi {assignedUser.FullName},\n\nYour task '{task.Title}' in project '{project.ProjectName}' has been unblocked and is ready for you to work on.\n\nYou can now continue with this task.",
                            $"Hi {assignedUser.FullName},<br/><br/>Your task <b>'{task.Title}'</b> in project <b>'{project.ProjectName}'</b> has been unblocked and is ready for you to work on.<br/><br/>You can now continue with this task.",
                            "GainIt Notifications"
                        );
                    }
                }
                else if (!string.IsNullOrEmpty(task.AssignedRole))
                {
                    // Send to all members with the assigned role
                    var roleMembers = await r_Db.ProjectMembers
                        .Include(pm => pm.User)
                        .Where(pm => pm.ProjectId == i_ProjectId && 
                                   pm.UserRole == task.AssignedRole && 
                                   pm.LeftAtUtc == null)
                        .ToListAsync();

                    foreach (var member in roleMembers)
                    {
                        await r_Hub.Clients.User(member.UserId.ToString())
                            .SendAsync(RealtimeEvents.Tasks.TaskUnblocked, new TaskUnblockedNotificationDto
                            {
                                TaskId = task.TaskId,
                                ProjectId = task.ProjectId,
                                Title = task.Title,
                                AssignedRole = task.AssignedRole,
                                ProjectName = project.ProjectName,
                                UnblockedAtUtc = DateTime.UtcNow
                            });

                        await r_Email.SendAsync(
                            member.User.EmailAddress,
                            $"Task unblocked: {task.Title}",
                            $"Hi {member.User.FullName},\n\nA task assigned to your role '{task.AssignedRole}' in project '{project.ProjectName}' has been unblocked.\n\nTask: {task.Title}\n\nYou can now work on this task.",
                            $"Hi {member.User.FullName},<br/><br/>A task assigned to your role <b>'{task.AssignedRole}'</b> in project <b>'{project.ProjectName}'</b> has been unblocked.<br/><br/><b>Task:</b> {task.Title}<br/><br/>You can now work on this task.",
                            "GainIt Notifications"
                        );
                    }
                }

                r_Log.LogInformation("Task unblock notifications sent: TaskId={TaskId}, ProjectId={ProjectId}", 
                    i_TaskId, i_ProjectId);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error sending task unblock notifications: TaskId={TaskId}, ProjectId={ProjectId}", 
                    i_TaskId, i_ProjectId);
            }
        }

        public async Task MilestoneCompletedAsync(Guid i_ProjectId, ProjectMilestoneViewModel i_ProjectMilestoneViewModel)
        {
            try
            {
                var milestone = await r_Db.ProjectMilestones
                    .Include(m => m.Project)
                    .ThenInclude(p => p.ProjectMembers.Where(pm => pm.LeftAtUtc == null))
                    .ThenInclude(pm => pm.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MilestoneId == i_ProjectMilestoneViewModel.MilestoneId && m.ProjectId == i_ProjectId);

                if (milestone == null)
                {
                    r_Log.LogWarning("Milestone not found for completion notification: MilestoneId={MilestoneId}", i_ProjectMilestoneViewModel.MilestoneId);
                    return;
                }

                var project = milestone.Project;
                var allMembers = project.ProjectMembers.ToList();

                // Notify all project members about milestone completion
                foreach (var member in allMembers)
                {
                    await r_Hub.Clients.User(member.UserId.ToString())
                        .SendAsync(RealtimeEvents.Tasks.MilestoneCompleted, new
                        {
                            milestone.MilestoneId,
                            milestone.ProjectId,
                            milestone.Title,
                            ProjectName = project.ProjectName,
                            TasksCount = i_ProjectMilestoneViewModel.TasksCount,
                            DoneTasksCount = i_ProjectMilestoneViewModel.DoneTasksCount,
                            CompletedAtUtc = DateTime.UtcNow
                        });
                }

                // Send email to all project members
                foreach (var member in allMembers)
                {
                    await r_Email.SendAsync(
                        member.User.EmailAddress,
                        $"Milestone completed: {milestone.Title}",
                        $"Hi {member.User.FullName},\n\nCongratulations! The milestone '{milestone.Title}' in project '{project.ProjectName}' has been completed!\n\nTasks completed: {i_ProjectMilestoneViewModel.DoneTasksCount}/{i_ProjectMilestoneViewModel.TasksCount}\n\nGreat work team!",
                        $"Hi {member.User.FullName},<br/><br/>Congratulations! The milestone <b>'{milestone.Title}'</b> in project <b>'{project.ProjectName}'</b> has been completed!<br/><br/><b>Tasks completed:</b> {i_ProjectMilestoneViewModel.DoneTasksCount}/{i_ProjectMilestoneViewModel.TasksCount}<br/><br/>Great work team!",
                        "GainIt Notifications"
                    );
                }

                r_Log.LogInformation("Milestone completion notifications sent: MilestoneId={MilestoneId}, ProjectId={ProjectId}, NotifiedUsers={Count}", 
                    milestone.MilestoneId, i_ProjectId, allMembers.Count);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error sending milestone completion notifications: MilestoneId={MilestoneId}, ProjectId={ProjectId}", 
                    i_ProjectMilestoneViewModel.MilestoneId, i_ProjectId);
            }
        }
    }
}
