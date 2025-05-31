using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectService
    {
        // Retrieve a project by its ID
        Task<UserProjectViewModel?> GetActiveProjectByProjectIdAsync(Guid i_ProjectId);

        // Retrieve a Template project by its ID
        Task<TemplateProjectViewModel?> GetTemplateProjectByProjectIdAsync(Guid i_ProjectId);

        // Retrieve all projects that are templates
        Task<IEnumerable<TemplateProjectViewModel>> GetAllTemplatesProjectsAsync();

        // Retrieve all projects that are pending templates
        Task<IEnumerable<UserProjectViewModel>> GetAllPendingTemplatesProjectsAsync();

        // Retrieve all projects that are nonprofit projects
        Task<IEnumerable<UserProjectViewModel>> GetAllNonprofitProjectsAsync();

        // Retrieve projects by user ID
        Task<IEnumerable<UserProjectViewModel>> GetProjectsByUserIdAsync(Guid i_UserId);

        // Retrieve projects by mentor ID
        Task<IEnumerable<UserProjectViewModel>> GetProjectsByMentorIdAsync(Guid i_MentorId);

        // Retrieve projects by nonprofit ID
        Task<IEnumerable<UserProjectViewModel>> GetProjectsByNonprofitIdAsync(Guid i_NonprofitId);

        // Update project status
        Task<UserProjectViewModel> UpdateProjectStatusAsync(Guid i_ProjectId, eProjectStatus i_Status);

        // Assigning a mentor to a project
        Task<UserProjectViewModel> AssignMentorAsync(Guid i_ProjectId, Guid i_MentorId);

        // Update project repository link
        Task<UserProjectViewModel> UpdateRepositoryLinkAsync(Guid i_ProjectId, string i_RepositoryLink);

        // Add team member to project
        Task<UserProjectViewModel> AddTeamMemberAsync(Guid i_ProjectId, Guid i_UserId);

        // Remove team member from project
        Task<UserProjectViewModel> RemoveTeamMemberAsync(Guid i_ProjectId, Guid i_UserId);

        // Remove mentor from project
        Task<UserProjectViewModel> RemoveMentorAsync(Guid i_ProjectId);

        // Search active projects by name or description
        Task<IEnumerable<UserProjectViewModel>> SearchActiveProjectsByNameOrDescriptionAsync(string i_SearchQuery);

        // Search template projects by name or description
        Task<IEnumerable<TemplateProjectViewModel>> SearchTemplateProjectsByNameOrDescriptionAsync(string i_SearchQuery);

        // Filter active projects by status and difficulty level
        Task<IEnumerable<UserProjectViewModel>> FilterActiveProjectsByStatusAndDifficultyAsync(eProjectStatus i_Status, eDifficultyLevel i_Difficulty);

        // Filter template projects by difficulty level
        Task<IEnumerable<TemplateProjectViewModel>> FilterTemplateProjectsByDifficultyAsync(eDifficultyLevel i_Difficulty);

        // Create a new project from a template and assign the user as one of the team members
        Task<UserProjectViewModel> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId);

        // Create a new project for a nonprofit organization and assign the organization as the owner
        Task<UserProjectViewModel> CreateProjectForNonprofitAsync(UserProjectViewModel i_Project, Guid i_NonprofitOrgId);
    }
}
