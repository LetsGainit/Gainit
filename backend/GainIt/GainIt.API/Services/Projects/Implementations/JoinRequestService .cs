using GainIt.API.Data;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Options;
using GainIt.API.Realtime;
using GainIt.API.Services.Email.Interfaces;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GainIt.API.Services.Projects.Implementations
{
    public class JoinRequestService : IJoinRequestService
    {
        private readonly GainItDbContext r_Db;
        private readonly IEmailSender r_Email;
        private readonly IHubContext<NotificationsHub> r_Hub;
        private readonly ILogger<JoinRequestService> r_logger;
        private readonly JoinRequestOptions r_joinReqOptions;

        public JoinRequestService(GainItDbContext i_Db, IEmailSender i_Email, IHubContext<NotificationsHub> i_Hub, ILogger<JoinRequestService> r_logger, IOptions<JoinRequestOptions> i_joinReqOptions)
        {
            r_Db = i_Db;
            r_Email = i_Email;
            r_Hub = i_Hub;
            this.r_logger = r_logger;
            r_joinReqOptions = i_joinReqOptions.Value;
        }

        public async Task<JoinRequest> CreateJoinRequestAsync(Guid i_ProjectId, Guid i_RequesterUserId, string i_RequestedRole, string? i_Message)
        {
            var project = await r_Db.Projects
               .Include(p => p.ProjectMembers
                   .Where(pm => pm.IsAdmin && pm.LeftAtUtc == null))
               .ThenInclude(pm => pm.User)
               .AsNoTracking()
               .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId)
               ?? throw new KeyNotFoundException("Project not found.");

            var pendingCount = await r_Db.JoinRequests
           .CountAsync(j => j.RequesterUserId == i_RequesterUserId &&
                            j.Status == eJoinRequestStatus.Pending);

            if (pendingCount >= r_joinReqOptions.MaxPendingPerUser)
                throw new InvalidOperationException(
                    $"You have reached the maximum number of pending join requests ({r_joinReqOptions.MaxPendingPerUser}).");


            var adminMember = project.ProjectMembers.FirstOrDefault(); // filtered include -> either 0 or 1
            if (adminMember?.User is null)
                throw new InvalidOperationException("Project has no active admin assigned.");

            var adminUser = adminMember.User;

            // 2) Validations on requester
            var alreadyMember = await r_Db.ProjectMembers
                .AnyAsync(m => m.ProjectId == i_ProjectId && m.UserId == i_RequesterUserId && m.LeftAtUtc == null);
            if (alreadyMember)
                throw new InvalidOperationException("User is already a member of this project.");

            var hasPending = await r_Db.JoinRequests
                .AnyAsync(j => j.ProjectId == i_ProjectId &&
                               j.RequesterUserId == i_RequesterUserId &&
                               j.Status == eJoinRequestStatus.Pending);
            if (hasPending)
                throw new InvalidOperationException("There is already a pending join request for this project.");

            var requester = await r_Db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == i_RequesterUserId)
                ?? throw new KeyNotFoundException("Requester user not found.");

            // 3) Create join request
            var joinRequest = new JoinRequest
            {
                ProjectId = i_ProjectId,
                RequesterUserId = i_RequesterUserId,
                Message = i_Message,
                RequestedRole = i_RequestedRole,
            };

            r_Db.JoinRequests.Add(joinRequest);
            await r_Db.SaveChangesAsync();

            // 4) Realtime notification -> to the single admin (use IUserIdProvider mapping)
            await r_Hub.Clients.User(adminUser.UserId.ToString())
                .SendAsync(RealtimeEvents.Projects.JoinRequested, new
                {
                    joinRequest.JoinRequestId,
                    joinRequest.ProjectId,
                    joinRequest.RequesterUserId,
                    joinRequest.RequestedRole,
                    Status = joinRequest.Status.ToString(),
                    joinRequest.CreatedAtUtc,
                    joinRequest.Message
                });

            // 5) Email the admin (already loaded from the first query)
            await r_Email.SendAsync(
                adminUser.EmailAddress,
                "New join request",
                $"Hi {adminUser.FullName},\n\n{requester.FullName} requested to join the project '{project.ProjectName}'.",
                "GainIt Notifications"
            );

            return joinRequest;

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
            var isAdmin = await r_Db.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_DeciderUserId && pm.IsAdmin && pm.LeftAtUtc == null);
            if (!isAdmin)
                throw new UnauthorizedAccessException("Only the project admin can decide on join requests.");

            var joinRequest = await r_Db.JoinRequests
                .FirstOrDefaultAsync(j => j.JoinRequestId == i_JoinRequestId && j.ProjectId == i_ProjectId)
                ?? throw new KeyNotFoundException("Join request not found.");

            if (joinRequest.Status != eJoinRequestStatus.Pending)
                throw new InvalidOperationException("Only pending join requests can be decided.");

            // 3) decision
            if (i_IsApproved)
            {
                using var transaction = await r_Db.Database.BeginTransactionAsync();

                joinRequest.Status = eJoinRequestStatus.Approved;
                joinRequest.DeciderUserId = i_DeciderUserId;
                joinRequest.DecisionAtUtc = DateTime.UtcNow;
                joinRequest.DecisionReason = null;

                await addTeamMemberAsync(
                   i_ProjectId,
                   joinRequest.RequesterUserId,
                   joinRequest.RequestedRole);

                await r_Db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            else
            {
                joinRequest.Status = eJoinRequestStatus.Rejected;
                joinRequest.DeciderUserId = i_DeciderUserId;
                joinRequest.DecisionAtUtc = DateTime.UtcNow;
                joinRequest.DecisionReason = i_Reason;
                await r_Db.SaveChangesAsync();
            }

            // 4) realtime + email
            var requester = await r_Db.Users.AsNoTracking().FirstAsync(u => u.UserId == joinRequest.RequesterUserId);
            var project = await r_Db.Projects.AsNoTracking().FirstAsync(p => p.ProjectId == i_ProjectId);

            if (i_IsApproved)
            {
                await r_Hub.Clients.User(joinRequest.RequesterUserId.ToString())
                    .SendAsync(RealtimeEvents.Projects.JoinApproved, new { joinRequest.JoinRequestId, joinRequest.ProjectId, Status = joinRequest.Status.ToString() });

                await r_Email.SendAsync(
                    requester.EmailAddress,
                    "Your join request was approved",
                    $"You have been accepted to the project '{project.ProjectName}'.",
                    $"You have been accepted to the project <b>{project.ProjectName}</b>.",
                    "GainIt Notifications");
            }
            else
            {
                await r_Hub.Clients.User(joinRequest.RequesterUserId.ToString())
                    .SendAsync(RealtimeEvents.Projects.JoinRejected, new { joinRequest.JoinRequestId, joinRequest.ProjectId, Status = joinRequest.Status.ToString(), joinRequest.DecisionReason });

                var reasonText = string.IsNullOrWhiteSpace(joinRequest.DecisionReason) ? "" : $"\nReason: {joinRequest.DecisionReason}";
                var reasonHtml = string.IsNullOrWhiteSpace(joinRequest.DecisionReason) ? "" : $"<br/>Reason: {joinRequest.DecisionReason}";

                await r_Email.SendAsync(
                    requester.EmailAddress,
                    "Your join request was rejected",
                    $"Your request to join '{project.ProjectName}' was rejected.{reasonText}",
                    $"Your request to join <b>{project.ProjectName}</b> was rejected.{reasonHtml}",
                    "GainIt Notifications");
            }

            return joinRequest;
        }

        public async Task<JoinRequest> CancelJoinRequestAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_RequesterUserId, string? i_Reason = null)
        {
            var joinRequest = await r_Db.JoinRequests
               .FirstOrDefaultAsync(j => j.JoinRequestId == i_JoinRequestId && j.ProjectId == i_ProjectId)
               ?? throw new KeyNotFoundException("Join request not found.");

            if (joinRequest.RequesterUserId != i_RequesterUserId)
                throw new UnauthorizedAccessException("Only the requester can cancel this join request.");

            if (joinRequest.Status != eJoinRequestStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be cancelled.");

            joinRequest.Status = eJoinRequestStatus.Cancelled;
            joinRequest.DecisionAtUtc = DateTime.UtcNow;
            joinRequest.DecisionReason = i_Reason;
            joinRequest.DeciderUserId = i_RequesterUserId; // self-decision

            await r_Db.SaveChangesAsync();

            var adminId = await r_Db.ProjectMembers
                .Where(pm => pm.ProjectId == i_ProjectId && pm.IsAdmin && pm.LeftAtUtc == null)
                .Select(pm => pm.UserId)
                .SingleOrDefaultAsync();

            if (adminId != Guid.Empty)
            {
                await r_Hub.Clients.User(adminId.ToString())
                    .SendAsync(RealtimeEvents.Projects.JoinCancelled, new
                    {
                        joinRequest.JoinRequestId,
                        joinRequest.ProjectId,
                        Status = joinRequest.Status.ToString(),
                        joinRequest.DecisionReason
                    });
            }

            return joinRequest;
        }

        public async Task<JoinRequest> GetJoinRequestByIdAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_UserId)
        {
            var joinRequest = await r_Db.JoinRequests
               .Include(j => j.RequesterUser)
               .FirstOrDefaultAsync(j => j.JoinRequestId == i_JoinRequestId && j.ProjectId == i_ProjectId)
               ?? throw new KeyNotFoundException("Join request not found.");

            // authorize: either project admin OR the requester
            var isAdmin = await r_Db.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_UserId && pm.IsAdmin && pm.LeftAtUtc == null);

            if (!isAdmin && joinRequest.RequesterUserId != i_UserId)
                throw new UnauthorizedAccessException("Not allowed to view this join request.");

            return joinRequest;
        }

        private async Task<UserProject> addTeamMemberAsync(Guid i_ProjectId, Guid i_UserId, string i_Role)
        {
            r_logger.LogInformation("Adding team member: ProjectId={ProjectId}, UserId={UserId}, Role={Role}",
                i_ProjectId, i_UserId, i_Role);

            try
            {
                var project = await r_Db.Projects
                    .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                    .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

                if (project == null)
                {
                    r_logger.LogWarning("Project not found: ProjectId={ProjectId}", i_ProjectId);
                    throw new KeyNotFoundException("Project not found.");
                }

                var gainer = await r_Db.Gainers.FindAsync(i_UserId);
                if (gainer == null)
                {
                    r_logger.LogWarning("User not found or is not a Gainer: UserId={UserId}", i_UserId);
                    throw new KeyNotFoundException("User not found or is not a Gainer.");
                }

                // Check if the role is open in the project
                if (!project.RequiredRoles.Contains(i_Role))
                {
                    r_logger.LogWarning("Role is not open in project: ProjectId={ProjectId}, Role={Role}", i_ProjectId, i_Role);
                    throw new InvalidOperationException($"Role '{i_Role}' is not an open role in this project.");
                }

                // Check if the role is already filled
                if (project.ProjectMembers.Any(pm =>
                    pm.UserRole == i_Role &&
                    pm.LeftAtUtc == null))
                {
                    r_logger.LogWarning("Role already filled: ProjectId={ProjectId}, Role={Role}", i_ProjectId, i_Role);
                    throw new InvalidOperationException($"Role '{i_Role}' is already filled in this project.");
                }

                // Check if user is already a member
                if (project.ProjectMembers.Any(pm =>
                    pm.UserId == i_UserId &&
                    pm.LeftAtUtc == null))
                {
                    r_logger.LogWarning("User already a team member: ProjectId={ProjectId}, UserId={UserId}", i_ProjectId, i_UserId);
                    throw new InvalidOperationException("User is already a team member in this project.");
                }

                project.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = i_ProjectId,
                    UserId = i_UserId,
                    UserRole = i_Role,
                    IsAdmin = false,
                    Project = project,
                    User = gainer,
                    JoinedAtUtc = DateTime.UtcNow
                });

                r_logger.LogInformation("Successfully added team member: ProjectId={ProjectId}, UserId={UserId}, Role={Role}",
                    i_ProjectId, i_UserId, i_Role);
                return project;
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Key not found while adding team member: ProjectId={ProjectId}, UserId={UserId}, Error={Error}",
                    i_ProjectId, i_UserId, ex.Message);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                r_logger.LogWarning("Invalid operation while adding team member: ProjectId={ProjectId}, UserId={UserId}, Error={Error}",
                    i_ProjectId, i_UserId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding team member: ProjectId={ProjectId}, UserId={UserId}, Role={Role}",
                    i_ProjectId, i_UserId, i_Role);
                throw;
            }
        }

    }
}