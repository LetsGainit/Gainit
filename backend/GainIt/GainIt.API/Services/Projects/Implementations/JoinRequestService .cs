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
            r_logger.LogInformation("Creating join request: ProjectId={ProjectId}, RequesterUserId={RequesterUserId}, RequestedRole={RequestedRole}",
                i_ProjectId, i_RequesterUserId, i_RequestedRole);

            try
            {
                var project = await r_Db.Projects
                   .Include(p => p.ProjectMembers
                       .Where(pm => pm.IsAdmin && pm.LeftAtUtc == null))
                   .ThenInclude(pm => pm.User)
                   .AsNoTracking()
                   .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

                if (project == null)
                {
                    r_logger.LogWarning("Project not found for join request: ProjectId={ProjectId}", i_ProjectId);
                    throw new KeyNotFoundException("Project not found.");
                }

            var pendingCount = await r_Db.JoinRequests
           .CountAsync(j => j.RequesterUserId == i_RequesterUserId &&
                            j.Status == eJoinRequestStatus.Pending);

            r_logger.LogInformation("User pending join requests count: RequesterUserId={RequesterUserId}, PendingCount={PendingCount}, MaxAllowed={MaxAllowed}",
                i_RequesterUserId, pendingCount, r_joinReqOptions.MaxPendingPerUser);

            if (pendingCount >= r_joinReqOptions.MaxPendingPerUser)
            {
                r_logger.LogWarning("User exceeded max pending join requests: RequesterUserId={RequesterUserId}, PendingCount={PendingCount}, MaxAllowed={MaxAllowed}",
                    i_RequesterUserId, pendingCount, r_joinReqOptions.MaxPendingPerUser);
                throw new InvalidOperationException(
                    $"You have reached the maximum number of pending join requests ({r_joinReqOptions.MaxPendingPerUser}).");
            }

            var adminMember = project.ProjectMembers.FirstOrDefault(); // filtered include -> either 0 or 1
            if (adminMember?.User is null)
            {
                r_logger.LogWarning("Project has no active admin: ProjectId={ProjectId}", i_ProjectId);
                throw new InvalidOperationException("Project has no active admin assigned.");
            }

            var adminUser = adminMember.User;

            // 2) Validations on requester
            var alreadyMember = await r_Db.ProjectMembers
                .AnyAsync(m => m.ProjectId == i_ProjectId && m.UserId == i_RequesterUserId && m.LeftAtUtc == null);
            if (alreadyMember)
            {
                r_logger.LogWarning("User already a project member: ProjectId={ProjectId}, RequesterUserId={RequesterUserId}", 
                    i_ProjectId, i_RequesterUserId);
                throw new InvalidOperationException("User is already a member of this project.");
            }

            var hasPending = await r_Db.JoinRequests
                .AnyAsync(j => j.ProjectId == i_ProjectId &&
                               j.RequesterUserId == i_RequesterUserId &&
                               j.Status == eJoinRequestStatus.Pending);
            if (hasPending)
            {
                r_logger.LogWarning("User already has pending join request: ProjectId={ProjectId}, RequesterUserId={RequesterUserId}", 
                    i_ProjectId, i_RequesterUserId);
                throw new InvalidOperationException("There is already a pending join request for this project.");
            }

            var requester = await r_Db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == i_RequesterUserId);

            if (requester == null)
            {
                r_logger.LogWarning("Requester user not found: RequesterUserId={RequesterUserId}", i_RequesterUserId);
                throw new KeyNotFoundException("Requester user not found.");
            }

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

            r_logger.LogInformation("Join request created successfully: JoinRequestId={JoinRequestId}, ProjectId={ProjectId}, RequesterUserId={RequesterUserId}, RequestedRole={RequestedRole}",
                joinRequest.JoinRequestId, i_ProjectId, i_RequesterUserId, i_RequestedRole);

            // 4) Realtime notification -> to the single admin (use external ID for SignalR)
            r_logger.LogInformation("Admin found for join request notification: AdminUserId={AdminUserId}, ExternalId={ExternalId}, Email={Email}, FullName={FullName}", 
                adminUser.UserId, adminUser.ExternalId, adminUser.EmailAddress, adminUser.FullName);
            
            if (!string.IsNullOrEmpty(adminUser.ExternalId))
            {
                try
                {
                    await r_Hub.Clients.User(adminUser.ExternalId)
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

                    r_logger.LogInformation("Realtime notification sent to admin: AdminUserId={AdminUserId}, ExternalId={ExternalId}, JoinRequestId={JoinRequestId}", 
                        adminUser.UserId, adminUser.ExternalId, joinRequest.JoinRequestId);
                }
                catch (Exception signalrEx)
                {
                    r_logger.LogWarning(signalrEx, "Failed to send SignalR join request notification: AdminUserId={AdminUserId}, ExternalId={ExternalId}, JoinRequestId={JoinRequestId}", 
                        adminUser.UserId, adminUser.ExternalId, joinRequest.JoinRequestId);
                    // Don't rethrow - continue with the operation even if SignalR fails
                }
            }
            else
            {
                r_logger.LogWarning("Admin has no ExternalId for SignalR notification: AdminUserId={AdminUserId}, Email={Email}, FullName={FullName}, JoinRequestId={JoinRequestId}", 
                    adminUser.UserId, adminUser.EmailAddress, adminUser.FullName, joinRequest.JoinRequestId);
            }

            // 5) Email the admin (already loaded from the first query)
            try
            {
                await r_Email.SendAsync(
                    adminUser.EmailAddress,
                    "GainIt Notifications: New join request",
                    $"Hi {adminUser.FullName},\n\n{requester.FullName} requested to join the project '{project.ProjectName}'.",
                    null
                );
            }
            catch (Exception emailEx)
            {
                r_logger.LogWarning(emailEx, "Failed to send email notification to admin: AdminEmail={AdminEmail}, JoinRequestId={JoinRequestId}", 
                    adminUser.EmailAddress, joinRequest.JoinRequestId);
                // Don't rethrow - continue with the operation even if email fails
            }

            r_logger.LogInformation("Email notification sent to admin: AdminEmail={AdminEmail}, JoinRequestId={JoinRequestId}", 
                adminUser.EmailAddress, joinRequest.JoinRequestId);

            return joinRequest;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error creating join request: ProjectId={ProjectId}, RequesterUserId={RequesterUserId}, RequestedRole={RequestedRole}",
                    i_ProjectId, i_RequesterUserId, i_RequestedRole);
                throw;
            }
        }

        public async Task<IReadOnlyList<JoinRequest>> GetJoinRequestsForProjectAsync(Guid i_ProjectId, Guid i_DeciderUserId, eJoinRequestStatus? i_RequestStatus = null)
        {
            r_logger.LogInformation("Getting join requests for project: ProjectId={ProjectId}, DeciderUserId={DeciderUserId}, Status={Status}",
                i_ProjectId, i_DeciderUserId, i_RequestStatus?.ToString() ?? "All");

            try
            {
                var isAdmin = await r_Db.ProjectMembers
                   .AnyAsync(m => m.ProjectId == i_ProjectId && m.UserId == i_DeciderUserId && m.IsAdmin && m.LeftAtUtc == null);

                if (!isAdmin)
                {
                    r_logger.LogWarning("Unauthorized access attempt to view join requests: ProjectId={ProjectId}, DeciderUserId={DeciderUserId}", 
                        i_ProjectId, i_DeciderUserId);
                    throw new UnauthorizedAccessException("Only project admins can view join requests.");
                }

                var q = r_Db.JoinRequests
                    .Include(j => j.RequesterUser)
                    .Where(j => j.ProjectId == i_ProjectId);

                if (i_RequestStatus.HasValue)
                    q = q.Where(j => j.Status == i_RequestStatus.Value);

                var result = await q.OrderByDescending(j => j.CreatedAtUtc).ToListAsync();

                r_logger.LogInformation("Retrieved join requests: ProjectId={ProjectId}, Count={Count}", i_ProjectId, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error getting join requests for project: ProjectId={ProjectId}, DeciderUserId={DeciderUserId}",
                    i_ProjectId, i_DeciderUserId);
                throw;
            }
        }

        public async Task<JoinRequest> JoinRequestDecisionAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_DeciderUserId, bool i_IsApproved, string? i_Reason)
        {
            r_logger.LogInformation("Processing join request decision: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, DeciderUserId={DeciderUserId}, IsApproved={IsApproved}",
                i_ProjectId, i_JoinRequestId, i_DeciderUserId, i_IsApproved);

            try
            {
                var isAdmin = await r_Db.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_DeciderUserId && pm.IsAdmin && pm.LeftAtUtc == null);
                if (!isAdmin)
                {
                    r_logger.LogWarning("Unauthorized access attempt to decide join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, DeciderUserId={DeciderUserId}",
                        i_ProjectId, i_JoinRequestId, i_DeciderUserId);
                    throw new UnauthorizedAccessException("Only the project admin can decide on join requests.");
                }

                var joinRequest = await r_Db.JoinRequests
                    .FirstOrDefaultAsync(j => j.JoinRequestId == i_JoinRequestId && j.ProjectId == i_ProjectId);

                if (joinRequest == null)
                {
                    r_logger.LogWarning("Join request not found: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", i_ProjectId, i_JoinRequestId);
                    throw new KeyNotFoundException("Join request not found.");
                }

                if (joinRequest.Status != eJoinRequestStatus.Pending)
                {
                    r_logger.LogWarning("Attempt to decide non-pending join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, CurrentStatus={CurrentStatus}",
                        i_ProjectId, i_JoinRequestId, joinRequest.Status);
                    throw new InvalidOperationException("Only pending join requests can be decided.");
                }

            // 3) decision
            if (i_IsApproved)
            {
                r_logger.LogInformation("Approving join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, RequesterUserId={RequesterUserId}, RequestedRole={RequestedRole}",
                    i_ProjectId, i_JoinRequestId, joinRequest.RequesterUserId, joinRequest.RequestedRole);

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

                r_logger.LogInformation("Join request approved successfully: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, RequesterUserId={RequesterUserId}",
                    i_ProjectId, i_JoinRequestId, joinRequest.RequesterUserId);
            }
            else
            {
                r_logger.LogInformation("Rejecting join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, RequesterUserId={RequesterUserId}, Reason={Reason}",
                    i_ProjectId, i_JoinRequestId, joinRequest.RequesterUserId, i_Reason ?? "No reason provided");

                joinRequest.Status = eJoinRequestStatus.Rejected;
                joinRequest.DeciderUserId = i_DeciderUserId;
                joinRequest.DecisionAtUtc = DateTime.UtcNow;
                joinRequest.DecisionReason = i_Reason;
                await r_Db.SaveChangesAsync();

                r_logger.LogInformation("Join request rejected successfully: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, RequesterUserId={RequesterUserId}",
                    i_ProjectId, i_JoinRequestId, joinRequest.RequesterUserId);
            }

            // 4) realtime + email
            var requester = await r_Db.Users.AsNoTracking().FirstAsync(u => u.UserId == joinRequest.RequesterUserId);
            var project = await r_Db.Projects.AsNoTracking().FirstAsync(p => p.ProjectId == i_ProjectId);

            if (i_IsApproved)
            {
                // Send SignalR notification using external ID
                if (!string.IsNullOrEmpty(requester.ExternalId))
                {
                    try
                    {
                        await r_Hub.Clients.User(requester.ExternalId)
                            .SendAsync(RealtimeEvents.Projects.JoinApproved, new { joinRequest.JoinRequestId, joinRequest.ProjectId, Status = joinRequest.Status.ToString() });

                        r_logger.LogInformation("Realtime approval notification sent: RequesterUserId={RequesterUserId}, ExternalId={ExternalId}, JoinRequestId={JoinRequestId}", 
                            joinRequest.RequesterUserId, requester.ExternalId, joinRequest.JoinRequestId);
                    }
                    catch (Exception signalrEx)
                    {
                        r_logger.LogWarning(signalrEx, "Failed to send SignalR approval notification: RequesterUserId={RequesterUserId}, ExternalId={ExternalId}, JoinRequestId={JoinRequestId}", 
                            joinRequest.RequesterUserId, requester.ExternalId, joinRequest.JoinRequestId);
                        // Don't rethrow - continue with the operation even if SignalR fails
                    }
                }
                else
                {
                    r_logger.LogWarning("Requester has no ExternalId for SignalR notification: RequesterUserId={RequesterUserId}, Email={Email}, JoinRequestId={JoinRequestId}", 
                        joinRequest.RequesterUserId, requester.EmailAddress, joinRequest.JoinRequestId);
                }

                try
                {
                    await r_Email.SendAsync(
                        requester.EmailAddress,
                        "GainIt Notifications: Your join request was approved",
                        $"You have been accepted to the project '{project.ProjectName}'.",
                        null);
                }
                catch (Exception emailEx)
                {
                    r_logger.LogWarning(emailEx, "Failed to send approval email: RequesterEmail={RequesterEmail}, JoinRequestId={JoinRequestId}", 
                        requester.EmailAddress, joinRequest.JoinRequestId);
                    // Don't rethrow - continue with the operation even if email fails
                }

                r_logger.LogInformation("Approval email sent: RequesterEmail={RequesterEmail}, JoinRequestId={JoinRequestId}", 
                    requester.EmailAddress, joinRequest.JoinRequestId);
            }
            else
            {
                // Send SignalR notification using external ID
                if (!string.IsNullOrEmpty(requester.ExternalId))
                {
                    try
                    {
                        await r_Hub.Clients.User(requester.ExternalId)
                            .SendAsync(RealtimeEvents.Projects.JoinRejected, new { joinRequest.JoinRequestId, joinRequest.ProjectId, Status = joinRequest.Status.ToString(), joinRequest.DecisionReason });

                        r_logger.LogInformation("Realtime rejection notification sent: RequesterUserId={RequesterUserId}, ExternalId={ExternalId}, JoinRequestId={JoinRequestId}", 
                            joinRequest.RequesterUserId, requester.ExternalId, joinRequest.JoinRequestId);
                    }
                    catch (Exception signalrEx)
                    {
                        r_logger.LogWarning(signalrEx, "Failed to send SignalR rejection notification: RequesterUserId={RequesterUserId}, ExternalId={ExternalId}, JoinRequestId={JoinRequestId}", 
                            joinRequest.RequesterUserId, requester.ExternalId, joinRequest.JoinRequestId);
                        // Don't rethrow - continue with the operation even if SignalR fails
                    }
                }
                else
                {
                    r_logger.LogWarning("Requester has no ExternalId for SignalR notification: RequesterUserId={RequesterUserId}, Email={Email}, JoinRequestId={JoinRequestId}", 
                        joinRequest.RequesterUserId, requester.EmailAddress, joinRequest.JoinRequestId);
                }

                var reasonText = string.IsNullOrWhiteSpace(joinRequest.DecisionReason) ? "" : $"\nReason: {joinRequest.DecisionReason}";
                var reasonHtml = string.IsNullOrWhiteSpace(joinRequest.DecisionReason) ? "" : $"<br/>Reason: {joinRequest.DecisionReason}";

                try
                {
                    await r_Email.SendAsync(
                        requester.EmailAddress,
                        "GainIt Notifications: Your join request was rejected",
                        $"Your request to join '{project.ProjectName}' was rejected.{reasonText}",
                        null);
                }
                catch (Exception emailEx)
                {
                    r_logger.LogWarning(emailEx, "Failed to send rejection email: RequesterEmail={RequesterEmail}, JoinRequestId={JoinRequestId}", 
                        requester.EmailAddress, joinRequest.JoinRequestId);
                    // Don't rethrow - continue with the operation even if email fails
                }

                r_logger.LogInformation("Rejection email sent: RequesterEmail={RequesterEmail}, JoinRequestId={JoinRequestId}", 
                    requester.EmailAddress, joinRequest.JoinRequestId);
            }

            return joinRequest;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error processing join request decision: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, DeciderUserId={DeciderUserId}, IsApproved={IsApproved}",
                    i_ProjectId, i_JoinRequestId, i_DeciderUserId, i_IsApproved);
                throw;
            }
        }

        public async Task<JoinRequest> CancelJoinRequestAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_RequesterUserId, string? i_Reason = null)
        {
            r_logger.LogInformation("Cancelling join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, RequesterUserId={RequesterUserId}, Reason={Reason}",
                i_ProjectId, i_JoinRequestId, i_RequesterUserId, i_Reason ?? "No reason provided");

            try
            {
                var joinRequest = await r_Db.JoinRequests
                   .FirstOrDefaultAsync(j => j.JoinRequestId == i_JoinRequestId && j.ProjectId == i_ProjectId);

                if (joinRequest == null)
                {
                    r_logger.LogWarning("Join request not found for cancellation: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", i_ProjectId, i_JoinRequestId);
                    throw new KeyNotFoundException("Join request not found.");
                }

                if (joinRequest.RequesterUserId != i_RequesterUserId)
                {
                    r_logger.LogWarning("Unauthorized cancellation attempt: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, RequesterUserId={RequesterUserId}, ActualRequesterUserId={ActualRequesterUserId}",
                        i_ProjectId, i_JoinRequestId, i_RequesterUserId, joinRequest.RequesterUserId);
                    throw new UnauthorizedAccessException("Only the requester can cancel this join request.");
                }

                if (joinRequest.Status != eJoinRequestStatus.Pending)
                {
                    r_logger.LogWarning("Attempt to cancel non-pending join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, CurrentStatus={CurrentStatus}",
                        i_ProjectId, i_JoinRequestId, joinRequest.Status);
                    throw new InvalidOperationException("Only pending requests can be cancelled.");
                }

                joinRequest.Status = eJoinRequestStatus.Cancelled;
                joinRequest.DecisionAtUtc = DateTime.UtcNow;
                joinRequest.DecisionReason = i_Reason;
                joinRequest.DeciderUserId = i_RequesterUserId; // self-decision

                await r_Db.SaveChangesAsync();

                r_logger.LogInformation("Join request cancelled successfully: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, RequesterUserId={RequesterUserId}",
                    i_ProjectId, i_JoinRequestId, i_RequesterUserId);

                var adminMember = await r_Db.ProjectMembers
                    .Include(pm => pm.User)
                    .Where(pm => pm.ProjectId == i_ProjectId && pm.IsAdmin && pm.LeftAtUtc == null)
                    .SingleOrDefaultAsync();
                
                var admin = adminMember?.User;
                
                // Debug: Check if admin has ExternalId
                if (admin != null)
                {
                    r_logger.LogDebug("Admin user details: UserId={UserId}, ExternalId={ExternalId}, Email={Email}, FullName={FullName}, CreatedAt={CreatedAt}", 
                        admin.UserId, admin.ExternalId, admin.EmailAddress, admin.FullName, admin.CreatedAt);
                }

                if (admin != null)
                {
                    r_logger.LogInformation("Admin found for cancellation notification: AdminUserId={AdminUserId}, ExternalId={ExternalId}, Email={Email}, FullName={FullName}", 
                        admin.UserId, admin.ExternalId, admin.EmailAddress, admin.FullName);
                    
                    if (!string.IsNullOrEmpty(admin.ExternalId))
                    {
                        try
                        {
                            await r_Hub.Clients.User(admin.ExternalId)
                                .SendAsync(RealtimeEvents.Projects.JoinCancelled, new
                                {
                                    joinRequest.JoinRequestId,
                                    joinRequest.ProjectId,
                                    Status = joinRequest.Status.ToString(),
                                    joinRequest.DecisionReason
                                });

                            r_logger.LogInformation("Cancellation notification sent to admin: AdminUserId={AdminUserId}, ExternalId={ExternalId}, JoinRequestId={JoinRequestId}", 
                                admin.UserId, admin.ExternalId, joinRequest.JoinRequestId);
                        }
                        catch (Exception signalrEx)
                        {
                            r_logger.LogWarning(signalrEx, "Failed to send SignalR cancellation notification: AdminUserId={AdminUserId}, ExternalId={ExternalId}, JoinRequestId={JoinRequestId}", 
                                admin.UserId, admin.ExternalId, joinRequest.JoinRequestId);
                            // Don't rethrow - continue with the operation even if SignalR fails
                        }
                    }
                    else
                    {
                        r_logger.LogWarning("Admin has no ExternalId for SignalR notification: AdminUserId={AdminUserId}, Email={Email}, FullName={FullName}, JoinRequestId={JoinRequestId}", 
                            admin.UserId, admin.EmailAddress, admin.FullName, joinRequest.JoinRequestId);
                    }
                }
                else
                {
                    r_logger.LogWarning("No admin found to notify about cancellation: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", 
                        i_ProjectId, joinRequest.JoinRequestId);
                }

                return joinRequest;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error cancelling join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, RequesterUserId={RequesterUserId}",
                    i_ProjectId, i_JoinRequestId, i_RequesterUserId);
                throw;
            }
        }

        public async Task<JoinRequest> GetJoinRequestByIdAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_UserId)
        {
            r_logger.LogInformation("Getting join request by ID: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, UserId={UserId}",
                i_ProjectId, i_JoinRequestId, i_UserId);

            try
            {
                var joinRequest = await r_Db.JoinRequests
                   .Include(j => j.RequesterUser)
                   .FirstOrDefaultAsync(j => j.JoinRequestId == i_JoinRequestId && j.ProjectId == i_ProjectId);

                if (joinRequest == null)
                {
                    r_logger.LogWarning("Join request not found: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", i_ProjectId, i_JoinRequestId);
                    throw new KeyNotFoundException("Join request not found.");
                }

                // authorize: either project admin OR the requester
                var isAdmin = await r_Db.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_UserId && pm.IsAdmin && pm.LeftAtUtc == null);

                if (!isAdmin && joinRequest.RequesterUserId != i_UserId)
                {
                    r_logger.LogWarning("Unauthorized access attempt to view join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, UserId={UserId}, RequesterUserId={RequesterUserId}",
                        i_ProjectId, i_JoinRequestId, i_UserId, joinRequest.RequesterUserId);
                    throw new UnauthorizedAccessException("Not allowed to view this join request.");
                }

                r_logger.LogInformation("Join request retrieved successfully: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, UserId={UserId}",
                    i_ProjectId, i_JoinRequestId, i_UserId);
                return joinRequest;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error getting join request by ID: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, UserId={UserId}",
                    i_ProjectId, i_JoinRequestId, i_UserId);
                throw;
            }
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

                // Remove the role from the open roles list since it's now filled
                project.RequiredRoles.Remove(i_Role);

                r_logger.LogInformation("Successfully added team member and removed role from open roles: ProjectId={ProjectId}, UserId={UserId}, Role={Role}",
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