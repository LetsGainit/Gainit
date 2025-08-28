using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;

namespace GainIt.API.Services.Tasks.Interfaces
{
    public interface IPlanningService
    {
        Task<PlanApplyResultViewModel> GenerateForProjectAsync(
            Guid i_ProjectId,
            PlanRequestDto i_PlanRequest,
            Guid i_ActorUserId);

        Task<TaskElaborationResultViewModel> ElaborateTaskAsync(
            Guid i_ProjectId,
            Guid i_TaskId,
            TaskElaborationRequestDto i_ElaborationRequest,
            Guid i_ActorUserId);
    }
}
