using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.ViewModels.Projects;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectService
    {
        // Basic getters

        // Retrieve a project by its ID
        ProjectViewModel? GetProjectByProjectId(Guid i_ProjectId);

        // Retrieve all projects that are templates
        IEnumerable<ProjectViewModel> GetAllTemplatesProjects();

        // Retrieve all projects that are nonprofit projects
        IEnumerable<ProjectViewModel> GetAllNonprofitProjects();

        // Get projects by different user roles

        // Retrieve projects by user ID
        IEnumerable<ProjectViewModel> GetProjectsByUserId(Guid i_UserId);

        // Retrieve projects by mentor ID
        IEnumerable<ProjectViewModel> GetProjectsByMentorId(Guid i_MentorId);

        // Retrieve projects by nonprofit ID
        IEnumerable<ProjectViewModel> GetProjectsByNonprofitId(Guid i_NonprofitId);

        // Adding a new project to the system
        void AddProject(Project i_Project);

        // Updating a project

        // Update project status
        void UpdateProjectStatus(Guid i_ProjectId, eProjectStatus i_Status);

        // Assigning a mentor to a project
        void AssignMentor(Guid i_ProjectId, Guid i_MentorId);

        // Update project repository link
        void UpdateRepositoryLink(Guid i_ProjectId, string i_RepositoryLink);

        // Team member management
        
        // Add team member to project
        void AddTeamMember(Guid i_ProjectId, Guid i_UserId);

        // Remove team member from project
        void RemoveTeamMember(Guid i_ProjectId, Guid i_UserId);

        // Remove mentor from project
        void RemoveMentor(Guid i_ProjectId);

        // Search and filter

        // Search projects by name or description
        IEnumerable<ProjectViewModel> SearchProjectsByNameOrDescription(string i_SearchQuery);

        // Filter projects by status and difficulty level
        IEnumerable<ProjectViewModel> FilterProjectsByStatusAndDifficulty(eProjectStatus i_Status, eDifficultyLevel i_Difficulty);

    }
}
