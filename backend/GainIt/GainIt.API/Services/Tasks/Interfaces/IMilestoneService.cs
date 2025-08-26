using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.DTOs.Requests.Tasks;

namespace GainIt.API.Services.Tasks.Interfaces
{
    public interface IMilestoneService
    {
        Task<IReadOnlyList<ProjectMilestoneViewModel>> GetMilestionesListAsync(Guid i_ProjectId, Guid i_ActorUserId);
        Task<ProjectMilestoneViewModel?> GetMilestoneAsync(Guid i_ProjectId, Guid i_MilestoneId, Guid i_ActorUserId);

        Task<ProjectMilestoneViewModel> CreateAsync(Guid i_ProjectId, ProjectMilestoneCreateDto i_MilestoneCreateRequest, Guid i_ActorUserId);
        Task<ProjectMilestoneViewModel> UpdateAsync(Guid i_ProjectId, Guid i_MilestoneId, ProjectMilestoneUpdateDto i_MilestoneUpdateRequest, Guid i_ActorUserId);
        Task DeleteAsync(Guid i_ProjectId, Guid i_MilestoneId, Guid i_ActorUserId);
    }
}
