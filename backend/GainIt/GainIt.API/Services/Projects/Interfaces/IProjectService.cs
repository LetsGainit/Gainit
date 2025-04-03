using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectService
    {
        // Basic getters
        Project? GetProjectById(Guid i_ProjectId);
        IEnumerable<Project> GetTemplatesProjects();
        IEnumerable<Project> GetNonprofitProjects();

        // Get projects by different user roles
        IEnumerable<Project> GetProjectsByUserId(Guid i_UserId);
        IEnumerable<Project> GetProjectsByMentorId(Guid i_MentorId);
        IEnumerable<Project> GetProjectsByNonprofitId(Guid i_NonprofitId);

        // Creating a new project
        void AddProject(Project i_Project);

        // Updating a project
        void UpdateProjectStatus(Guid i_ProjectId, eProjectStatus i_Status);
        void AssignMentor(Guid i_ProjectId, Guid i_MentorId);
        void UpdateRepositoryLink(Guid i_ProjectId, string i_RepositoryLink);

        // Team member management
        void AddTeamMember(Guid i_ProjectId, Guid i_UserId);
        void RemoveTeamMember(Guid i_ProjectId, Guid i_UserId);
    }
}
