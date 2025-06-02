using GainIt.API.Data;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Services.Projects.Interfaces;

namespace GainIt.API.Services.Projects.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly GainItDbContext r_DbContext;
        public ProjectService(GainItDbContext i_DbContext)
        {
            r_DbContext = i_DbContext;
        }

        public Task<UserProject> AddTeamMemberAsync(Guid i_ProjectId, Guid i_UserId)
        {
            throw new NotImplementedException();
        }

        public Task<UserProject> AssignMentorAsync(Guid i_ProjectId, Guid i_MentorId)
        {
            throw new NotImplementedException();
        }

        public Task<UserProject> CreateProjectForNonprofitAsync(UserProjectViewModel i_Project, Guid i_NonprofitOrgId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProject>> FilterActiveProjectsByStatusAndDifficultyAsync(eProjectStatus i_Status, eDifficultyLevel i_Difficulty)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TemplateProject>> FilterTemplateProjectsByDifficultyAsync(eDifficultyLevel i_Difficulty)
        {
            throw new NotImplementedException();
        }

        public Task<UserProject?> GetActiveProjectByProjectIdAsync(Guid i_ProjectId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProject>> GetAllNonprofitProjectsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProject>> GetAllPendingUserTemplatesProjectsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TemplateProject>> GetAllTemplatesProjectsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProject>> GetProjectsByMentorIdAsync(Guid i_MentorId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProject>> GetProjectsByNonprofitIdAsync(Guid i_NonprofitId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProject>> GetProjectsByUserIdAsync(Guid i_UserId)
        {
            throw new NotImplementedException();
        }

        public Task<TemplateProject?> GetTemplateProjectByProjectIdAsync(Guid i_ProjectId)
        {
            throw new NotImplementedException();
        }

        public Task<UserProject> RemoveMentorAsync(Guid i_ProjectId)
        {
            throw new NotImplementedException();
        }

        public Task<UserProject> RemoveTeamMemberAsync(Guid i_ProjectId, Guid i_UserId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProject>> SearchActiveProjectsByNameOrDescriptionAsync(string i_SearchQuery)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TemplateProject>> SearchTemplateProjectsByNameOrDescriptionAsync(string i_SearchQuery)
        {
            throw new NotImplementedException();
        }

        public Task<UserProject> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId)
        {
            throw new NotImplementedException();
        }

        public Task<UserProject> UpdateProjectStatusAsync(Guid i_ProjectId, eProjectStatus i_Status)
        {
            throw new NotImplementedException();
        }

        public Task<UserProject> UpdateRepositoryLinkAsync(Guid i_ProjectId, string i_RepositoryLink)
        {
            throw new NotImplementedException();
        }
    }
}
