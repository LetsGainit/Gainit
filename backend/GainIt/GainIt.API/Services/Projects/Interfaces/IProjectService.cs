using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.ViewModels.Projects;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectService
    {
        // Retrieve a project by its ID
        Task<ProjectViewModel?> GetProjectByProjectIdAsync(Guid i_ProjectId);

        // Retrieve all projects that are templates
        Task<IEnumerable<ProjectViewModel>> GetAllTemplatesProjectsAsync();

        // Retrieve all projects that are nonprofit projects
        Task<IEnumerable<ProjectViewModel>> GetAllNonprofitProjectsAsync();

        // Retrieve projects by user ID
        Task<IEnumerable<ProjectViewModel>> GetProjectsByUserIdAsync(Guid i_UserId);

        // Retrieve projects by mentor ID
        Task<IEnumerable<ProjectViewModel>> GetProjectsByMentorIdAsync(Guid i_MentorId);

        // Retrieve projects by nonprofit ID
        Task<IEnumerable<ProjectViewModel>> GetProjectsByNonprofitIdAsync(Guid i_NonprofitId);

        // Update project status
        Task<ProjectViewModel> UpdateProjectStatusAsync(Guid i_ProjectId, eProjectStatus i_Status);

        // Assigning a mentor to a project
        Task<ProjectViewModel> AssignMentorAsync(Guid i_ProjectId, Guid i_MentorId);

        // Update project repository link
        Task<ProjectViewModel> UpdateRepositoryLinkAsync(Guid i_ProjectId, string i_RepositoryLink);

        // Add team member to project
        Task<ProjectViewModel> AddTeamMemberAsync(Guid i_ProjectId, Guid i_UserId);

        // Remove team member from project
        Task<ProjectViewModel> RemoveTeamMemberAsync(Guid i_ProjectId, Guid i_UserId);

        // Remove mentor from project
        Task<ProjectViewModel> RemoveMentorAsync(Guid i_ProjectId);

        // Search projects by name or description
        Task<IEnumerable<ProjectViewModel>> SearchProjectsByNameOrDescriptionAsync(string i_SearchQuery);

        // Filter projects by status and difficulty level
        Task<IEnumerable<ProjectViewModel>> FilterProjectsByStatusAndDifficultyAsync(eProjectStatus i_Status, eDifficultyLevel i_Difficulty);

        // Create a new project from a template and assign the user as one of the team members
        Task<ProjectViewModel> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId);

        // Create a new project for a nonprofit organization and assign the organization as the owner
        Task<ProjectViewModel> CreateProjectForNonprofitAsync(ProjectViewModel i_Project, Guid i_NonprofitOrgId);
    }
}
