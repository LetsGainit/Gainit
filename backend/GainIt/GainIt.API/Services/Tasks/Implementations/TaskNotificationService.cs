using GainIt.API.Data;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Models.Tasks;
using GainIt.API.Realtime;
using GainIt.API.Services.Email.Interfaces;
using GainIt.API.Services.Tasks.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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
        public Task MilestoneCompletedAsync(Guid i_ProjectId, ProjectMilestoneViewModel i_ProjectMilestoneViewModel)
        {
            throw new NotImplementedException();
        }

        public Task TaskCompletedAsync(Guid i_ProjectId, Guid i_TaskId, string i_OldStatus, string i_NewStatus)
        {
            throw new NotImplementedException();
        }

        public Task TaskCreatedAsync(Guid i_ProjectId, ProjectTaskViewModel i_ProjectTaskViewModel)
        {
            throw new NotImplementedException();
        }

        public Task TaskUnblockedAsync(Guid i_ProjectId, Guid i_TaskId)
        {
            throw new NotImplementedException();
        }
    }
}
