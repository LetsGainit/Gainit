using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IJoinRequestService
    {
        Task<JoinRequest> CreateJoinRequestAsync(Guid i_ProjectId, Guid i_RequesterUserId, string? i_Message);
        Task<IReadOnlyList<JoinRequest>> GetJoinRequestsForProjectAsync(Guid i_ProjectId, Guid i_DeciderUserId, eJoinRequestStatus? i_RequestStatus = null);
        Task<JoinRequest> JoinRequestDecisionAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_DeciderUserId, bool i_IsApproved, string? i_Reason);
        Task<JoinRequest> CancelJoinRequestAsync(Guid i_ProjectId, Guid i_JoinRequestId, Guid i_RequesterUserId, string? i_Reason = null);
    }

}
