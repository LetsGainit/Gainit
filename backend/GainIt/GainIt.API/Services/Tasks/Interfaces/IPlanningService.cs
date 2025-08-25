using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Models.Enums.Tasks;

namespace GainIt.API.Services.Tasks.Interfaces
{
    public interface IPlanningService
    {
        // Build roadmap for a project (called from ProjectService after project creation)
        Task<PlanApplyResultViewModel> GenerateForProjectAsync(
            Guid i_ProjectId,
            eRoadmapPlanMode i_Mode,
            PlanRequestDto i_PlanRequest,
            Guid i_ActorUserId);

        // Elaborate a single task when moving to InProgress
        Task<TaskElaborationResultViewModel> ElaborateTaskAsync(
            Guid i_ProjectId,
            Guid i_TaskId,
            TaskElaborationRequestDto i_ElaborationRequest,
            Guid i_ActorUserId);
    }
}
