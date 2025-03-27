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

        public void AddTeamMember(int i_ProjectId, int i_UserId)
        {
            throw new NotImplementedException();
        }

        public void AssignMentor(int i_ProjectId, int i_MentorId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetNonprofitProjects()
        {
            throw new NotImplementedException();
        }

        public Project? GetProjectById(int i_ProjectId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetProjectsByMentorId(int i_MentorId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetProjectsByNonprofitId(int i_NonprofitId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> GetProjectsByUserId(int i_UserId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Project> getTemplatesProjects()
        {
            throw new NotImplementedException();
        }

        public void RemoveTeamMember(int i_ProjectId, int i_UserId)
        {
            throw new NotImplementedException();
        }

        public void UpdateProjectStatus(int i_ProjectId, eProjectStatus i_Status)
        {
            throw new NotImplementedException();
        }

        public void UpdateRepositoryLink(int i_ProjectId, string i_RepositoryLink)
        {
            throw new NotImplementedException();
        }
    }
}
