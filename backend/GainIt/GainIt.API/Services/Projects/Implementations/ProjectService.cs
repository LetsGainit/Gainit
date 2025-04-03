using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Services.Projects.Interfaces;

namespace GainIt.API.Services.Projects.Implementations
{
    public class ProjectService : IProjectService
    {
        public void AddProject(Project i_Project)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetNonprofitProjects()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetProjectsByMentorId(Guid i_MentorId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetProjectsByNonprofitId(Guid i_NonprofitId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetProjectsByUserId(Guid i_UserId)
        {
            throw new NotImplementedException();
        }

        public Project? GetProjectById(Guid i_ProjectId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetTemplatesProjects()
        {
            throw new NotImplementedException();
        }

        public void AssignMentor(Guid i_ProjectId, Guid i_MentorId)
        {
            throw new NotImplementedException();
        }

        public void RemoveTeamMember(Guid i_ProjectId, Guid i_UserId)
        {
            throw new NotImplementedException();
        }

        public void UpdateProjectStatus(Guid i_ProjectId, eProjectStatus i_Status)
        {
            throw new NotImplementedException();
        }

        public void UpdateRepositoryLink(Guid i_ProjectId, string i_RepositoryLink)
        {
            throw new NotImplementedException();
        }

        public void AddTeamMember(Guid i_ProjectId, Guid i_UserId)
        {
            throw new NotImplementedException();
        }
    }
}
