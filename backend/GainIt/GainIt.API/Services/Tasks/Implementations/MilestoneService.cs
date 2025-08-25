using GainIt.API.Data;
using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Services.Tasks.Interfaces;

namespace GainIt.API.Services.Tasks.Implementations
{
    public class MilestoneService : IMilestoneService
    {

        private readonly GainItDbContext r_Db;

        public MilestoneService(GainItDbContext i_Db)
        {
            r_Db = i_Db;
        }

        public async Task<ProjectMilestoneViewModel> CreateAsync(Guid i_ProjectId, ProjectMilestoneCreateDto i_MilestoneCreateRequest, Guid i_ActorUserId)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(Guid i_ProjectId, Guid i_MilestoneId, Guid i_ActorUserId)
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<ProjectMilestoneViewModel>> GetMilestionesListAsync(Guid i_ProjectId)
        {
            throw new NotImplementedException();
        }

        public async Task<ProjectMilestoneViewModel?> GetMilestoneAsync(Guid i_ProjectId, Guid i_MilestoneId)
        {
            throw new NotImplementedException();
        }

        public Task<ProjectMilestoneViewModel> UpdateAsync(Guid i_ProjectId, Guid i_MilestoneId, ProjectMilestoneUpdateDto i_MilestoneUpdateRequest, Guid i_ActorUserId)
        {
            throw new NotImplementedException();
        }
    }
}
