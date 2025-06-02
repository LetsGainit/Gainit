using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectService
    {
        // Retrieve a project by its ID
        Task<UserProject?> GetActiveProjectByProjectIdAsync(Guid i_ProjectId);

        // Retrieve a Template project by its ID
        Task<TemplateProject?> GetTemplateProjectByProjectIdAsync(Guid i_ProjectId);

        // Retrieve all projects that are templates
        Task<IEnumerable<TemplateProject>> GetAllTemplatesProjectsAsync();

        // Retrieve all projects that are pending templates
        Task<IEnumerable<UserProject>> GetAllPendingUserTemplatesProjectsAsync();

        // Retrieve all projects that are nonprofit projects
        Task<IEnumerable<UserProject>> GetAllNonprofitProjectsAsync();

        // Retrieve projects by user ID
        Task<IEnumerable<UserProject>> GetProjectsByUserIdAsync(Guid i_UserId);

        // Retrieve projects by mentor ID
        Task<IEnumerable<UserProject>> GetProjectsByMentorIdAsync(Guid i_MentorId);

        // Retrieve projects by nonprofit ID
        Task<IEnumerable<UserProject>> GetProjectsByNonprofitIdAsync(Guid i_NonprofitId);

        // Update project status
        Task<UserProject> UpdateProjectStatusAsync(Guid i_ProjectId, eProjectStatus i_Status);

        // Assigning a mentor to a project
        Task<UserProject> AssignMentorAsync(Guid i_ProjectId, Guid i_MentorId);

        // Update project repository link
        Task<UserProject> UpdateRepositoryLinkAsync(Guid i_ProjectId, string i_RepositoryLink);

        // Add team member to project
        Task<UserProject> AddTeamMemberAsync(Guid i_ProjectId, Guid i_UserId);

        // Remove team member from project
        Task<UserProject> RemoveTeamMemberAsync(Guid i_ProjectId, Guid i_UserId);

        // Remove mentor from project
        Task<UserProject> RemoveMentorAsync(Guid i_ProjectId);

        // Search active projects by name or description
        Task<IEnumerable<UserProject>> SearchActiveProjectsByNameOrDescriptionAsync(string i_SearchQuery);

        // Search template projects by name or description
        Task<IEnumerable<TemplateProject>> SearchTemplateProjectsByNameOrDescriptionAsync(string i_SearchQuery);

        // Filter active projects by status and difficulty level
        Task<IEnumerable<UserProject>> FilterActiveProjectsByStatusAndDifficultyAsync(eProjectStatus i_Status, eDifficultyLevel i_Difficulty);

        // Filter template projects by difficulty level
        Task<IEnumerable<TemplateProject>> FilterTemplateProjectsByDifficultyAsync(eDifficultyLevel i_Difficulty);

        // Create a new project from a template and assign the user as one of the team members
        Task<UserProject> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId);

        // Create a new project for a nonprofit organization and assign the organization as the owner
        Task<UserProject> CreateProjectForNonprofitAsync(UserProjectViewModel i_Project, Guid i_NonprofitOrgId);
    }
}
