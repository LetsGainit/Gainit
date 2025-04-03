using GainIt.API.Data;
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

        public Project? GetProjectById(Guid i_ProjectId)
        {
            return null;
        }

        public IEnumerable<Project> GetTemplatesProjects()
        {
            return null;
        }

        public IEnumerable<Project> GetNonprofitProjects()
        {
            return null;
        }


        public IEnumerable<Project> GetProjectsByUserId(Guid i_UserId)
        {
            return null;
        }

        public IEnumerable<Project> GetProjectsByMentorId(Guid i_MentorId)
        {
            return null;
        }

        public IEnumerable<Project> GetProjectsByNonprofitId(Guid i_NonprofitId)
        {
            return null;
        }


        public void AddProject(Project i_Project)
        {
            
        }


        public void UpdateProjectStatus(Guid i_ProjectId, eProjectStatus i_Status)
        {
            
        }

        public void AssignMentor(Guid i_ProjectId, Guid i_MentorId)
        {
            
        }
        public void UpdateRepositoryLink(Guid i_ProjectId, string i_RepositoryLink)
        {
          
        }



        public void AddTeamMember(Guid i_ProjectId, Guid i_UserId)
        {
        }

        public void RemoveTeamMember(Guid i_ProjectId, Guid i_UserId)
        {
        }

    }
}
