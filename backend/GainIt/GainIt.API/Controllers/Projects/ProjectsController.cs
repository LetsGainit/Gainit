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

            UserProject? project = await r_ProjectService.GetActiveProjectByProjectIdAsync(projectId);

            if (project == null)
            {
                return NotFound(new { Message = "Project not found." });
            }

            UserProjectViewModel userProjectViewModel = new UserProjectViewModel(project);

            return Ok(userProjectViewModel);
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

            TemplateProject? project = await r_ProjectService.GetTemplateProjectByProjectIdAsync(projectId);

            if (project == null)
            {
                return NotFound(new { Message = "Project not found." });
            }

            TemplateProjectViewModel templateProjectViewModel = new TemplateProjectViewModel(project);

            return Ok(templateProjectViewModel);
        }

        /// <summary>
        /// Retrieves all projects that are templates.
        /// </summary>
        /// <returns>A list of template projects.</returns>
        [HttpGet("templates")]
        public async Task<ActionResult<IEnumerable<TemplateProjectViewModel>>> GetAllTemplatesProjects()
        {
            var projects = await r_ProjectService.GetAllTemplatesProjectsAsync();

            var templateProjectsViewModel = projects.Select(p => new TemplateProjectViewModel(p)).ToList();

            return Ok(templateProjectsViewModel);
        }

        /// <summary>
        /// Retrieves all projects that are nonprofit projects.
        /// </summary>
        /// /// <returns>A list of nonprofit projects.</returns>
        [HttpGet("nonprofits")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> GetAllNonprofitProjects()
        {
            var projects = await r_ProjectService.GetAllNonprofitProjectsAsync();

            var nonprofitProjectsViewModel = projects.Select(p => new UserProjectViewModel(p)).ToList();

            return Ok(nonprofitProjectsViewModel);
        }

        /// <summary>
        /// Retrieves all projects associated with a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of projects associated with the user.</returns>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetProjectsByUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { Message = "User ID cannot be empty." });
            }

            var projects = await r_ProjectService.GetProjectsByUserIdAsync(userId);

            var conciseProjects = projects.Select(p => new ConciseUserProjectViewModel(p, userId)).ToList();

            return Ok(conciseProjects);
        }

        /// <summary>
        /// Retrieves all projects associated with a specific mentor.
        /// </summary>
        /// <param name="mentorId">The ID of the mentor.</param>
        /// <returns>A list of projects associated with the mentor.</returns>
        [HttpGet("mentor/{mentorId}")]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetProjectsByMentorId(Guid mentorId)
        {
            if (mentorId == Guid.Empty)
            {
                return BadRequest(new { Message = "Mentor ID cannot be empty." });
            }

            var projects = await r_ProjectService.GetProjectsByMentorIdAsync(mentorId);

            var conciseProjects = projects.Select(p => new ConciseUserProjectViewModel(p, mentorId)).ToList();

            return Ok(conciseProjects);
        }

        /// <summary>
        /// Retrieves all projects associated with a specific nonprofit organization.
        /// </summary>
        /// <param name="nonprofitId">The ID of the nonprofit organization.</param>
        /// <returns>A list of projects associated with the nonprofit organization.</returns>
        [HttpGet("nonprofit/{nonprofitId}")]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetProjectsByNonprofitId(Guid nonprofitId)
        {
            if (nonprofitId == Guid.Empty)
            {
                return BadRequest(new { Message = "Nonprofit ID cannot be empty." });
            }
            var projects = await r_ProjectService.GetProjectsByNonprofitIdAsync(nonprofitId);

            var conciseProjects = projects.Select(p => new ConciseUserProjectViewModel(p, null)).ToList();

            return Ok(conciseProjects);
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
                UserProject o_Project = await r_ProjectService.StartProjectFromTemplateAsync(templateId, userId);

                UserProjectViewModel projectViewModel = new UserProjectViewModel(o_Project);

                return CreatedAtAction(
                    nameof(GetActiveProjectById),
                    new { projectId = o_Project.ProjectId },
                    projectViewModel
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
        public async Task<ActionResult<UserProjectViewModel>> AssignMentor(Guid projectId, [FromQuery] Guid mentorId)
        {
            if (projectId == Guid.Empty || mentorId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID and Mentor ID cannot be empty." });
            }

            try
            {
                UserProject updatedProject = await r_ProjectService.AssignMentorAsync(projectId, mentorId);

                UserProjectViewModel updatedProjectViewModel = new UserProjectViewModel(updatedProject);

                return Ok(updatedProjectViewModel);
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
        public async Task<ActionResult<UserProjectViewModel>> RemoveMentor(Guid projectId)
        {
            if (projectId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                UserProject updatedProject = await r_ProjectService.RemoveMentorAsync(projectId);

                UserProjectViewModel updatedProjectViewModel = new UserProjectViewModel(updatedProject);

                return Ok(updatedProjectViewModel);
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
        /// <param name="userRole"> The Role of the user</param>
        /// 
        /// <returns>The updated project details.</returns>
        [HttpPost("{projectId}/team-members")]
        public async Task<ActionResult<UserProjectViewModel>> AddTeamMember(Guid projectId, [FromQuery] Guid userId, [FromQuery] string userRole)
        {
            if (projectId == Guid.Empty || userId == Guid.Empty || userRole == string.Empty)
            {
                return BadRequest(new { Message = "Project ID ot User ID or User role cannot be empty." });
            }

            try
            {
                UserProject updatedProject = await r_ProjectService.AddTeamMemberAsync(projectId, userId, userRole);

                UserProjectViewModel updatedProjectViewModel = new UserProjectViewModel(updatedProject);

                return Ok(updatedProjectViewModel);
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
        public async Task<ActionResult<UserProjectViewModel>> RemoveTeamMember(Guid projectId, [FromQuery] Guid userId)
        {
            if (projectId == Guid.Empty || userId == Guid.Empty)
            {
                return BadRequest(new { Message = "Project ID and User ID cannot be empty." });
            }

            try
            {
                UserProject updatedProject = await r_ProjectService.RemoveTeamMemberAsync(projectId, userId);

                UserProjectViewModel userProjectViewModel = new UserProjectViewModel(updatedProject);

                return Ok(userProjectViewModel);
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
                UserProject updatedProject = await r_ProjectService.UpdateProjectStatusAsync(projectId, status.ProjectStatus);

                UserProjectViewModel updatedProjectViewModel = new UserProjectViewModel(updatedProject);

                return Ok(updatedProjectViewModel);
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
                UserProject updatedProject = await r_ProjectService.UpdateRepositoryLinkAsync(projectId, repositoryLink);

                UserProjectViewModel userProjectViewModel = new UserProjectViewModel(updatedProject);

                return Ok(userProjectViewModel);
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
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> SearchActiveProjectsByNameOrDescription([FromQuery] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return BadRequest(new { Message = "Search query cannot be empty." });
            }

            var projects = await r_ProjectService.SearchActiveProjectsByNameOrDescriptionAsync(searchQuery);

            var projectViewModels = projects.Select(p => new ConciseUserProjectViewModel(p, null)).ToList();

            return Ok(projectViewModels);
        }
        /// <summary>
        /// Searches for template projects by name or description.
        /// </summary>
        /// <param name="searchQuery">The search query to match against project names or descriptions.</param>
        /// <returns>A list of matching projects.</returns>
        [HttpGet("search/template")] 
        public async Task<ActionResult<IEnumerable<TemplateProjectViewModel>>> SearchTemplateProjectsByNameOrDescription([FromQuery] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return BadRequest(new { Message = "Search query cannot be empty." });
            }

            var projects = await r_ProjectService.SearchTemplateProjectsByNameOrDescriptionAsync(searchQuery);

            var projectViewModels = projects.Select(p => new TemplateProjectViewModel(p)).ToList();

            return Ok(projectViewModels);
        }

        /// <summary>
        /// Filters active projects by status and difficulty level.
        /// </summary>
        /// <param name="status">The status of the projects to filter by.</param>
        /// <param name="difficulty">The difficulty level of the projects to filter by.</param>
        /// <returns>A list of filtered projects.</returns>
        [HttpGet("projects/filter")]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> FilterProjectsByStatusAndDifficulty([FromQuery] ProjectStatusOptionDTO status, [FromQuery] ProjectDifficultyLevelOptionDTO difficulty)
        {
            var projects = await r_ProjectService.FilterActiveProjectsByStatusAndDifficultyAsync(status.ProjectStatus, difficulty.DifficultyLevel);

            var projectViewModels = projects.Select(p => new ConciseUserProjectViewModel(p, null)).ToList();

            return Ok(projectViewModels);
        }

        /// <summary>
        /// Filters template projects by difficulty level.
        /// </summary>
        /// <param name="difficulty"></param>
        /// <param name="filter">Filter criteria including difficulty level.</param>
        /// <returns>A list of filtered template projects.</returns>
        [HttpGet("templates/filter")]
        public async Task<ActionResult<IEnumerable<TemplateProjectViewModel>>> FilterTemplateProjectsByDifficulty([FromQuery] ProjectDifficultyLevelOptionDTO difficulty)
        {
            var templates = await r_ProjectService.FilterTemplateProjectsByDifficultyAsync(difficulty.DifficultyLevel);

            var templateViewModels = templates.Select(t => new TemplateProjectViewModel(t)).ToList();

            return Ok(templateViewModels);
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
                UserProject o_Project = await r_ProjectService.CreateProjectForNonprofitAsync(CreateProjectForNonprofitViewModel, nonprofitOrgId);

                UserProjectViewModel userProjectViewModel = new UserProjectViewModel(o_Project);

                return CreatedAtAction(nameof(GetActiveProjectById), new { projectId = o_Project.ProjectId }, userProjectViewModel);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { Message = e.Message });
            }
        }
        #endregion
    }
}