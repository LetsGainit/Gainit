using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.ViewModels.Projects;

namespace GainIt.API.Services.Projects.Interfaces
{
    public interface IProjectService
    {
        // Retrieve a project by its ID
        Task<ProjectViewModel?> GetProjectByProjectId(Guid i_ProjectId);

        // Retrieve all projects that are templates
        Task<IEnumerable<ProjectViewModel>> GetAllTemplatesProjects();

        // Retrieve all projects that are nonprofit projects
        Task<IEnumerable<ProjectViewModel>> GetAllNonprofitProjects();

        // Retrieve projects by user ID
        Task<IEnumerable<ProjectViewModel>> GetProjectsByUserId(Guid i_UserId);

        // Retrieve projects by mentor ID
        Task<IEnumerable<ProjectViewModel>> GetProjectsByMentorId(Guid i_MentorId);

        // Retrieve projects by nonprofit ID
        Task<IEnumerable<ProjectViewModel>> GetProjectsByNonprofitId(Guid i_NonprofitId);

        // Update project status
        Task UpdateProjectStatus(Guid i_ProjectId, eProjectStatus i_Status);

        // Assigning a mentor to a project
        Task AssignMentor(Guid i_ProjectId, Guid i_MentorId);

        // Update project repository link
        Task UpdateRepositoryLink(Guid i_ProjectId, string i_RepositoryLink);

        // Add team member to project
        Task AddTeamMember(Guid i_ProjectId, Guid i_UserId);

        // Remove team member from project
        Task RemoveTeamMember(Guid i_ProjectId, Guid i_UserId);

        // Remove mentor from project
        Task RemoveMentor(Guid i_ProjectId);

        // Search projects by name or description
        Task<IEnumerable<ProjectViewModel>> SearchProjectsByNameOrDescription(string i_SearchQuery);

        // Filter projects by status and difficulty level
        Task<IEnumerable<ProjectViewModel>> FilterProjectsByStatusAndDifficulty(eProjectStatus i_Status, eDifficultyLevel i_Difficulty);

        // Create a new project from a template and assign the user as one of the team members
        Task<ProjectViewModel> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId);

        // Create a new project for a nonprofit organization and assign the organization as the owner
        Task<ProjectViewModel> CreateProjectForNonprofitAsync(ProjectViewModel i_Project, Guid i_NonprofitOrgId);


        // Adding a new project to the system used for testing purposes for now
        Task AddProject(Project i_Project);


    }
}
