using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectService
    {
        // Basic getters
        Project? GetProjectById(int i_ProjectId);
        IEnumerable<Project> getTemplatesProjects();
        IEnumerable<Project> GetNonprofitProjects();

        // Get projects by different user roles
        IEnumerable<Project> GetProjectsByUserId(int i_UserId);
        IEnumerable<Project> GetProjectsByMentorId(int i_MentorId);
        IEnumerable<Project> GetProjectsByNonprofitId(int i_NonprofitId);

        // Creating a new project
        void AddProject(Project i_Project);

        // Updating a project
        void UpdateProjectStatus(int i_ProjectId, eProjectStatus i_Status);
        void AssignMentor(int i_ProjectId, int i_MentorId);
        void UpdateRepositoryLink(int i_ProjectId, string i_RepositoryLink);

        // Team member management
        void AddTeamMember(int i_ProjectId, int i_UserId);
        void RemoveTeamMember(int i_ProjectId, int i_UserId);

    }
}
