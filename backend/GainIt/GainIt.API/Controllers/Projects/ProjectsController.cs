using GainIt.API.DTOs.Requests;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GainIt.API.Controllers.Projects
{
    

    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService r_ProjectService;

        public ProjectsController(IProjectService i_ProjectService)
        {
            r_ProjectService = i_ProjectService;
        }

        #region Project Retrieval

        /// <summary>
        /// Retrieves an active project by its ID.
        /// </summary>
        /// <param name="projectId">The ID of the project to retrieve.</param>
        /// <returns>The project details if found, or a 404 Not Found response.</returns>
        [HttpGet("{projectId}")]
        public async Task<ActionResult<UserProjectViewModel>> GetActiveProjectById(Guid projectId)
        {
            if (projectId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID cannot be empty." }); 
            }

            UserProjectViewModel? project = await r_ProjectService.GetActiveProjectByProjectIdAsync(projectId);
            if (project == null)
            {
                return NotFound(new { Message = "Project not found." });
            }
            return Ok(project);
        }
        /// <summary>
        /// Retrieves a template project by its ID.
        /// </summary>
        /// <param name="projectId">The ID of the template project to retrieve.</param>
        /// <returns>The template project details if found, or a 404 Not Found response.</returns>
        [HttpGet("template/{projectId}")]
        public async Task<ActionResult<TemplateProjectViewModel>> GetTemplateProjectById(Guid projectId)
        {
            if (projectId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }
            TemplateProjectViewModel? project = await r_ProjectService.GetTemplateProjectByProjectIdAsync(projectId);
            if (project == null)
            {
                return NotFound(new { Message = "Project not found." });
            }
            return Ok(project);
        }
        /// <summary>
        /// Retrieves all projects that are templates.
        /// </summary>
        /// <returns>A list of template projects.</returns>
        [HttpGet("templates")]
        public async Task<ActionResult<IEnumerable<TemplateProjectViewModel>>> GetAllTemplatesProjects()
        {
            var projects = await r_ProjectService.GetAllTemplatesProjectsAsync();
            return Ok(projects);
        }

        /// <summary>
        /// Retrieves all projects that are nonprofit projects.
        /// </summary>
        /// /// <returns>A list of nonprofit projects.</returns>
        [HttpGet("nonprofits")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> GetAllNonprofitProjects()
        {
            var projects = await r_ProjectService.GetAllNonprofitProjectsAsync();
            return Ok(projects);
        }

        /// <summary>
        /// Retrieves all projects associated with a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of projects associated with the user.</returns>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> GetProjectsByUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            var projects = await r_ProjectService.GetProjectsByUserIdAsync(userId);
            return Ok(projects);
        }

        /// <summary>
        /// Retrieves all projects associated with a specific mentor.
        /// </summary>
        /// <param name="mentorId">The ID of the mentor.</param>
        /// <returns>A list of projects associated with the mentor.</returns>
        [HttpGet("mentor/{mentorId}")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> GetProjectsByMentorId(Guid mentorId)
        {
            if (mentorId == Guid.Empty)
            {
                return BadRequest(new { Message = "Mentor ID cannot be empty." });
            }

            var projects = await r_ProjectService.GetProjectsByMentorIdAsync(mentorId);
            return Ok(projects);
        }

        /// <summary>
        /// Retrieves all projects associated with a specific nonprofit organization.
        /// </summary>
        /// <param name="nonprofitId">The ID of the nonprofit organization.</param>
        /// <returns>A list of projects associated with the nonprofit organization.</returns>
        [HttpGet("nonprofit/{nonprofitId}")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> GetProjectsByNonprofitId(Guid nonprofitId)
        {
            if (nonprofitId == Guid.Empty)
            {
                return BadRequest(new { Message = "Nonprofit ID cannot be empty." });
            }
            var projects = await r_ProjectService.GetProjectsByNonprofitIdAsync(nonprofitId);
            return Ok(projects);
        }

        #endregion

        #region Template Actions
        /// <summary>
        /// Starts a new project from a template and assigns the user as a team member.
        /// </summary>
        /// <param name="templateId">The ID of the template to use.</param>
        /// <param name="userId">The ID of the user to assign to the project.</param>
        /// <returns>The newly created project details.</returns>
        [HttpPost("start-from-template")]
        public async Task<ActionResult<UserProjectViewModel>> CreateProjectFromTemplate([FromQuery] Guid templateId, [FromQuery] Guid userId)
        {
            if (templateId == Guid.Empty || userId == Guid.Empty)
            {
                return BadRequest(new { Message = "Template ID and User ID cannot be empty." });
            }

            try
            {
                UserProjectViewModel? o_Project = await r_ProjectService.StartProjectFromTemplateAsync(templateId, userId);
                return CreatedAtAction(
                    nameof(GetActiveProjectById),
                    new {projectId = o_Project.projectId },
                    o_Project
                );
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
        }

        #endregion

        #region Mentorship Management
        /// <summary>
        /// Assigns a mentor to a project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="mentorId">The ID of the mentor to assign.</param>
        /// <returns>The updated project details.</returns>
        [HttpPut("{projectId}/mentor")]
        public async Task<IActionResult> AssignMentor(Guid projectId, [FromQuery] Guid mentorId)
        {
            if (projectId == Guid.Empty || mentorId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID and Mentor ID cannot be empty." });
            }

            try
            {
                UserProjectViewModel updatedProject = await r_ProjectService.AssignMentorAsync(projectId, mentorId);
                return Ok(updatedProject);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
        }

        /// <summary>
        /// Removes the mentor from a project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>The updated project details.</returns>
        [HttpDelete("{projectId}/mentor")]
        public async Task<IActionResult> RemoveMentor(Guid projectId)
        {
            if (projectId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                UserProjectViewModel updatedProject = await r_ProjectService.RemoveMentorAsync(projectId);
                return Ok(updatedProject);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
        }

        #endregion

        #region Team Members Management

        /// <summary>
        /// Adds a team member to a project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="userId">The ID of the user to add as a team member.</param>
        /// <returns>The updated project details.</returns>
        [HttpPost("{projectId}/team-members")]
        public async Task<IActionResult> AddTeamMember(Guid projectId, [FromQuery] Guid userId)
        {
            if (projectId == Guid.Empty || userId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID and User ID cannot be empty." });
            }

            try
            {
                UserProjectViewModel updatedProject = await r_ProjectService.AddTeamMemberAsync(projectId, userId);
                return Ok(updatedProject);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new { Message = e.Message });
            }
        }

        /// <summary>
        /// Removes a team member from a project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="userId">The ID of the user to remove from the team.</param>
        /// <returns>The updated project details.</returns>
        [HttpDelete("{projectId}/team-members")]
        public async Task<IActionResult> RemoveTeamMember(Guid projectId, [FromQuery] Guid userId)
        {
            if (projectId == Guid.Empty || userId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID and User ID cannot be empty." });
            }

            try
            {
                UserProjectViewModel updatedProject = await r_ProjectService.RemoveTeamMemberAsync(projectId, userId);
                return Ok(updatedProject);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new { Message = e.Message });
            }
        }

        #endregion

        #region Project Management

        /// <summary>
        /// Updates the status of a project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="status">The new status of the project.</param>
        /// <returns>The updated project details.</returns>
        [HttpPut("{projectId}/status")]
        public async Task<ActionResult<UserProjectViewModel>> UpdateProjectStatus(Guid projectId, [FromBody] ProjectStatusOptionDTO status)
        {
            if (projectId == Guid.Empty)
            { 
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }
            try 
            {
                UserProjectViewModel updatedProject = await r_ProjectService.UpdateProjectStatusAsync(projectId, status.ProjectStatus);
                return Ok(updatedProject);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
        }

        /// <summary>
        /// Updates the repository link of a project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="repositoryLink">The new repository link.</param>
        /// <returns>The updated project details.</returns>
        [HttpPut("{projectId}/repository")]
        public async Task<ActionResult<UserProjectViewModel>> UpdateRepositoryLink(Guid projectId, [FromBody] string repositoryLink)
        {
            if (projectId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(repositoryLink))
            {
                return BadRequest(new { Message = "Repository link cannot be empty." });
            }

            try
            {
                UserProjectViewModel updatedProject = await r_ProjectService.UpdateRepositoryLinkAsync(projectId, repositoryLink);
                return Ok(updatedProject);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
        }
        #endregion 

        #region Project Search and Filter
        /// <summary>
        /// Searches for active projects by name or description.
        /// </summary>
        /// <param name="searchQuery">The search query to match against project names or descriptions.</param>
        /// <returns>A list of matching projects.</returns>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> SearchActiveProjectsByNameOrDescription([FromQuery] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return BadRequest(new { Message = "Search query cannot be empty." });
            }

            var projects = await r_ProjectService.SearchActiveProjectsByNameOrDescriptionAsync(searchQuery);
            return Ok(projects);
        }
        /// <summary>
        /// Searches for template projects by name or description.
        /// </summary>
        /// <param name="searchQuery">The search query to match against project names or descriptions.</param>
        /// <returns>A list of matching projects.</returns>
        [HttpGet("search/template")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> SearchTepmplateProjectsByNameOrDescription([FromQuery] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return BadRequest(new { Message = "Search query cannot be empty." });
            }

            var projects = await r_ProjectService.SearchTemplateProjectsByNameOrDescriptionAsync(searchQuery);
            return Ok(projects);
        }

        /// <summary>
        /// Filters active projects by status and difficulty level.
        /// </summary>
        /// <param name="status">The status of the projects to filter by.</param>
        /// <param name="difficulty">The difficulty level of the projects to filter by.</param>
        /// <returns>A list of filtered projects.</returns>
        [HttpGet("projects/filter")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> FilterProjectsByStatusAndDifficulty([FromQuery] ProjectStatusOptionDTO status, [FromQuery] ProjectDifficultyLevelOptionDTO difficulty)
        {
            var projects = await r_ProjectService.FilterActiveProjectsByStatusAndDifficultyAsync(status.ProjectStatus, difficulty.DifficultyLevel);
            return Ok(projects);
        }

        /// <summary>
        /// Filters template projects by difficulty level.
        /// </summary>
        /// <param name="filter">Filter criteria including difficulty level.</param>
        /// <returns>A list of filtered template projects.</returns>
        [HttpGet("templates/filter")]
        public async Task<ActionResult<IEnumerable<TemplateProjectViewModel>>> FilterTemplateProjectsByDifficulty([FromQuery] ProjectDifficultyLevelOptionDTO difficulty)
        {
            var templates = await r_ProjectService.FilterTemplateProjectsByDifficultyAsync(difficulty.DifficultyLevel);
            return Ok(templates);
        }
        #endregion

        #region NonProfit Project Creation

        /// <summary>
        /// Creates a new project for a nonprofit organization.
        /// </summary>
        /// <param name="CreateProjectForNonprofitViewModel">The project details to create.</param>
        /// <param name="nonprofitOrgId">The ID of the nonprofit organization.</param>
        /// <returns>The newly created project details.</returns>
        [HttpPost("nonprofit")]
        public async Task<ActionResult<UserProjectViewModel>> CreateProjectForNonprofit(
            [FromBody] UserProjectViewModel CreateProjectForNonprofitViewModel,
            [FromQuery] Guid nonprofitOrgId)
        {
            if (nonprofitOrgId == Guid.Empty)
            {
                return BadRequest(new { Message = "Nonprofit organization ID cannot be empty." });
            }

            if (CreateProjectForNonprofitViewModel == null)
            {
                return BadRequest(new { Message = "Project details cannot be null." });
            }

            try
            {
                var o_Project = await r_ProjectService.CreateProjectForNonprofitAsync(CreateProjectForNonprofitViewModel, nonprofitOrgId);
                return CreatedAtAction(nameof(GetActiveProjectById), new { projectId = o_Project.projectId }, o_Project);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
        }
        #endregion
    }
}
