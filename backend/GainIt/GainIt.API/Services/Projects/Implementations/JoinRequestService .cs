using GainIt.API.Data;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Realtime;
using GainIt.API.Services.Email.Interfaces;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GainIt.API.Services.Projects.Implementations
{
    public class JoinRequestService : IJoinRequestService
    {
        private readonly GainItDbContext r_Db;
        private readonly IEmailSender r_Email;
        private readonly IHubContext<NotificationsHub> r_Hub;

        public JoinRequestService(GainItDbContext i_Db, IEmailSender i_Email, IHubContext<NotificationsHub> i_Hub)
        {
            r_Db = i_Db;
            r_Email = i_Email;
            r_Hub = i_Hub;
        }

        public async Task<JoinRequest> CreateJoinRequestAsync(Guid i_ProjectId, Guid i_RequesterUserId, string? i_Message)
        {

        }

        public async Task<IReadOnlyList<JoinRequest>> GetJoinRequestsForProjectAsync(Guid i_ProjectId, Guid i_DeciderUserId, eJoinRequestStatus? i_RequestStatus = null)
        {
            var isAdmin = await r_Db.ProjectMembers
               .AnyAsync(m => m.ProjectId == i_ProjectId && m.UserId == i_DeciderUserId && m.IsAdmin && m.LeftAtUtc == null);

            if (!isAdmin)
                throw new UnauthorizedAccessException("Only project admins can view join requests.");

            var q = r_Db.JoinRequests
                .Include(j => j.RequesterUser)
                .Where(j => j.ProjectId == i_ProjectId);

            if (i_RequestStatus.HasValue)
                q = q.Where(j => j.Status == i_RequestStatus.Value);

            return await q.OrderByDescending(j => j.CreatedAtUtc).ToListAsync();
        }

        public async Task<JoinRequest> JoinRequestDecisionAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_DeciderUserId, bool i_IsApproved, string? i_Reason)
        {

        }

        public Task<JoinRequest> CancelJoinRequestAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_RequesterUserId, string? i_Reason = null)
        {

        }
    }
}
