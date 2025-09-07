using GainIt.API.DTOs.Search;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Services.Projects.Implementations;
using GainIt.API.Services.GitHub.Interfaces;
using GainIt.API.Services.Projects.Interfaces;
using GainIt.API.Services.FileUpload.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using GainIt.API.DTOs.Requests.Projects;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GainIt.API.Data;

namespace GainIt.API.Controllers.Projects
{
    [ApiController]
    [Route("api/projects")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService r_ProjectService;
        private readonly IProjectMatchingService r_ProjectMatchingService;
        private readonly IGitHubService r_GitHubService;
        private readonly IFileUploadService r_FileUploadService;
        private readonly ILogger<ProjectsController> r_logger;
        private readonly GainItDbContext r_DbContext;

        public ProjectsController(
            IProjectService i_ProjectService, 
            IProjectMatchingService r_ProjectMatchingService,
            IGitHubService gitHubService,
            IFileUploadService fileUploadService,
            ILogger<ProjectsController> logger,
            GainItDbContext dbContext)
        {
            r_ProjectService = i_ProjectService;
            this.r_ProjectMatchingService = r_ProjectMatchingService;
            r_GitHubService = gitHubService;
            r_FileUploadService = fileUploadService;
            r_logger = logger;
            r_DbContext = dbContext;
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
            r_logger.LogInformation("Getting active project by ID: {ProjectId}", projectId);
            
            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: {ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                UserProject? project = await r_ProjectService.GetActiveProjectByProjectIdAsync(projectId);

                if (project == null)
                {
                    r_logger.LogWarning("Project not found: {ProjectId}", projectId);
                    return NotFound(new { Message = "Project not found." });
                }

                UserProjectViewModel userProjectViewModel = new UserProjectViewModel(project);
                r_logger.LogInformation("Successfully retrieved project: {ProjectId}", projectId);
                return Ok(userProjectViewModel);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving project: {ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a template project by its ID.
        /// </summary>
        /// <param name="projectId">The ID of the template project to retrieve.</param>
        /// <returns>The template project details if found, or a 404 Not Found response.</returns>
        [HttpGet("template/{projectId}")]
        public async Task<ActionResult<TemplateProjectViewModel>> GetTemplateProjectById(Guid projectId)
        {
            r_logger.LogInformation("Getting template project by ID: {ProjectId}", projectId);
            
            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid template project ID provided: {ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                TemplateProject? project = await r_ProjectService.GetTemplateProjectByProjectIdAsync(projectId);

                if (project == null)
                {
                    r_logger.LogWarning("Template project not found: {ProjectId}", projectId);
                    return NotFound(new { Message = "Project not found." });
                }

                TemplateProjectViewModel templateProjectViewModel = new TemplateProjectViewModel(project);
                r_logger.LogInformation("Successfully retrieved template project: {ProjectId}", projectId);
                return Ok(templateProjectViewModel);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving template project: {ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all projects that are templates.
        /// </summary>
        /// <returns>A list of template projects.</returns>
        [HttpGet("templates")]
        public async Task<ActionResult<IEnumerable<TemplateProjectViewModel>>> GetAllTemplatesProjects()
        {
            r_logger.LogInformation("Getting all template projects");
            
            try
            {
                var projects = await r_ProjectService.GetAllTemplatesProjectsAsync();
                var templateProjectsViewModel = projects.Select(p => new TemplateProjectViewModel(p)).ToList();
                
                r_logger.LogInformation("Successfully retrieved {Count} template projects", templateProjectsViewModel.Count);
                return Ok(templateProjectsViewModel);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving all template projects");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all projects that are nonprofit projects.
        /// </summary>
        /// /// <returns>A list of nonprofit projects.</returns>
        [HttpGet("nonprofits")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> GetAllNonprofitProjects()
        {
            r_logger.LogInformation("Getting all nonprofit projects");
            
            try
            {
                var projects = await r_ProjectService.GetAllNonprofitProjectsAsync();
                var nonprofitProjectsViewModel = projects.Select(p => new UserProjectViewModel(p)).ToList();
                
                r_logger.LogInformation("Successfully retrieved {Count} nonprofit projects", nonprofitProjectsViewModel.Count);
                return Ok(nonprofitProjectsViewModel);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving all nonprofit projects");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all active projects (in-progress non-template projects).
        /// </summary>
        /// <returns>A list of all active projects.</returns>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<UserProjectViewModel>>> GetAllActiveProjects()
        {
            r_logger.LogInformation("Getting all active projects");
            
            try
            {
                var projects = await r_ProjectService.GetAllActiveProjectsAsync();
                var activeProjectsViewModel = projects.Select(p => new UserProjectViewModel(p)).ToList();
                
                r_logger.LogInformation("Successfully retrieved {Count} active projects", activeProjectsViewModel.Count);
                return Ok(activeProjectsViewModel);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving all active projects");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all projects associated with the current authenticated user.
        /// </summary>
        /// <returns>A list of projects associated with the user.</returns>
        [HttpGet("user/me")]
        [Authorize(Policy = "RequireAccessAsUser")]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetMyProjects()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                r_logger.LogInformation("Getting projects for user: {UserId}", userId);
                
                var projects = await r_ProjectService.GetProjectsByUserIdAsync(userId);
                var conciseProjects = projects.Select(p => new ConciseUserProjectViewModel(p, userId)).ToList();
                
                r_logger.LogInformation("Successfully retrieved {Count} projects for user: {UserId}", conciseProjects.Count, userId);
                return Ok(conciseProjects);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning("Unauthorized access: Error={Error}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving projects for current user");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all projects associated with a specific mentor.
        /// </summary>
        /// <param name="mentorId">The ID of the mentor.</param>
        /// <returns>A list of projects associated with the mentor.</returns>
        [HttpGet("mentor/{mentorId}")]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetProjectsByMentorId(Guid mentorId)
        {
            r_logger.LogInformation("Getting projects for mentor: {MentorId}", mentorId);
            
            if (mentorId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid mentor ID provided: {MentorId}", mentorId);
                return BadRequest(new { Message = "Mentor ID cannot be empty." });
            }

            try
            {
                var projects = await r_ProjectService.GetProjectsByMentorIdAsync(mentorId);
                var conciseProjects = projects.Select(p => new ConciseUserProjectViewModel(p, mentorId)).ToList();
                
                r_logger.LogInformation("Successfully retrieved {Count} projects for mentor: {MentorId}", conciseProjects.Count, mentorId);
                return Ok(conciseProjects);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving projects for mentor: {MentorId}", mentorId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all projects associated with a specific nonprofit organization.
        /// </summary>
        /// <param name="nonprofitId">The ID of the nonprofit organization.</param>
        /// <returns>A list of projects associated with the nonprofit organization.</returns>
        [HttpGet("nonprofit/{nonprofitId}")]
        public async Task<ActionResult<IEnumerable<ConciseUserProjectViewModel>>> GetProjectsByNonprofitId(Guid nonprofitId)
        {
            r_logger.LogInformation("Getting projects for nonprofit: {NonprofitId}", nonprofitId);
            
            if (nonprofitId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid nonprofit ID provided: {NonprofitId}", nonprofitId);
                return BadRequest(new { Message = "Nonprofit ID cannot be empty." });
            }

            try
            {
                var projects = await r_ProjectService.GetProjectsByNonprofitIdAsync(nonprofitId);
                var conciseProjects = projects.Select(p => new ConciseUserProjectViewModel(p, null)).ToList();
                
                r_logger.LogInformation("Successfully retrieved {Count} projects for nonprofit: {NonprofitId}", conciseProjects.Count, nonprofitId);
                return Ok(conciseProjects);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving projects for nonprofit: {NonprofitId}", nonprofitId);
                throw;
            }
        }

        #endregion

        #region Template Actions
        /// <summary>
        /// Starts a new project from a template and assigns the current authenticated user as a team member.
        /// </summary>
        /// <param name="templateId">The ID of the template to use.</param>
        /// <returns>The newly created project details.</returns>
        [HttpPost("start-from-template")]
        [Authorize(Policy = "RequireAccessAsUser")]
        public async Task<ActionResult<UserProjectViewModel>> CreateProjectFromTemplate([FromQuery] Guid templateId)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                r_logger.LogInformation("Creating project from template: TemplateId={TemplateId}, UserId={UserId}", templateId, userId);
                
                if (templateId == Guid.Empty)
                {
                    r_logger.LogWarning("Invalid template ID provided: TemplateId={TemplateId}", templateId);
                    return BadRequest(new { Message = "Template ID cannot be empty." });
                }

                UserProject o_Project = await r_ProjectService.StartProjectFromTemplateAsync(templateId, userId);

                UserProjectViewModel projectViewModel = new UserProjectViewModel(o_Project);
                
                r_logger.LogInformation("Successfully created project from template: ProjectId={ProjectId}, TemplateId={TemplateId}, UserId={UserId}", 
                    o_Project.ProjectId, templateId, userId);

                return CreatedAtAction(
                    nameof(GetActiveProjectById),
                    new { projectId = o_Project.ProjectId },
                    projectViewModel
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning("Unauthorized access: TemplateId={TemplateId}, Error={Error}", templateId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException e)
            {
                r_logger.LogWarning("Template or user not found: TemplateId={TemplateId}, Error={Error}", templateId, e.Message);
                return NotFound(new { Message = e.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error creating project from template: TemplateId={TemplateId}", templateId);
                throw;
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
            r_logger.LogInformation("Assigning mentor to project: ProjectId={ProjectId}, MentorId={MentorId}", projectId, mentorId);
            
            if (projectId == Guid.Empty || mentorId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid parameters provided: ProjectId={ProjectId}, MentorId={MentorId}", projectId, mentorId);
                return BadRequest(new { Message = "Project ID and Mentor ID cannot be empty." });
            }

            try
            {
                UserProject updatedProject = await r_ProjectService.AssignMentorAsync(projectId, mentorId);

                UserProjectViewModel updatedProjectViewModel = new UserProjectViewModel(updatedProject);
                
                r_logger.LogInformation("Successfully assigned mentor to project: ProjectId={ProjectId}, MentorId={MentorId}", projectId, mentorId);

                return Ok(updatedProjectViewModel);
            }
            catch (KeyNotFoundException e)
            {
                r_logger.LogWarning("Project or mentor not found: ProjectId={ProjectId}, MentorId={MentorId}, Error={Error}", projectId, mentorId, e.Message);
                return NotFound(new { Message = e.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error assigning mentor to project: ProjectId={ProjectId}, MentorId={MentorId}", projectId, mentorId);
                throw;
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
            r_logger.LogInformation("Removing mentor from project: {ProjectId}", projectId);
            
            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: {ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                UserProject updatedProject = await r_ProjectService.RemoveMentorAsync(projectId);

                UserProjectViewModel updatedProjectViewModel = new UserProjectViewModel(updatedProject);
                
                r_logger.LogInformation("Successfully removed mentor from project: {ProjectId}", projectId);

                return Ok(updatedProjectViewModel);
            }
            catch (KeyNotFoundException e)
            {
                r_logger.LogWarning("Project not found: ProjectId={ProjectId}, Error={Error}", projectId, e.Message);
                return NotFound(new { Message = e.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error removing mentor from project: {ProjectId}", projectId);
                throw;
            }
        }

        #endregion

        #region Team Members Management

        /// <summary>
        /// Removes the current authenticated user from a project team.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>The updated project details.</returns>
        [HttpDelete("{projectId}/team-members")]
        [Authorize(Policy = "RequireAccessAsUser")]
        public async Task<ActionResult<UserProjectViewModel>> RemoveTeamMember(Guid projectId)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                r_logger.LogInformation("Removing team member from project: ProjectId={ProjectId}, UserId={UserId}", projectId, userId);
                
                if (projectId == Guid.Empty)
                {
                    r_logger.LogWarning("Invalid project ID provided: ProjectId={ProjectId}", projectId);
                    return BadRequest(new { Message = "Project ID cannot be empty." });
                }

                UserProject updatedProject = await r_ProjectService.RemoveTeamMemberAsync(projectId, userId);

                UserProjectViewModel userProjectViewModel = new UserProjectViewModel(updatedProject);
                
                r_logger.LogInformation("Successfully removed team member from project: ProjectId={ProjectId}, UserId={UserId}", projectId, userId);

                return Ok(userProjectViewModel);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException e)
            {
                r_logger.LogWarning("Project or user not found: ProjectId={ProjectId}, Error={Error}", projectId, e.Message);
                return NotFound(new { Message = e.Message });
            }
            catch (InvalidOperationException e)
            {
                r_logger.LogWarning("Invalid operation: ProjectId={ProjectId}, Error={Error}", projectId, e.Message);
                return BadRequest(new { Message = e.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error removing team member from project: ProjectId={ProjectId}", projectId);
                throw;
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
            r_logger.LogInformation("Updating project status: ProjectId={ProjectId}, Status={Status}", projectId, status.ProjectStatus);
            
            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: {ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }
            
            try
            {
                UserProject updatedProject = await r_ProjectService.UpdateProjectStatusAsync(projectId, status.ProjectStatus);

                UserProjectViewModel updatedProjectViewModel = new UserProjectViewModel(updatedProject);
                
                r_logger.LogInformation("Successfully updated project status: ProjectId={ProjectId}, Status={Status}", projectId, status.ProjectStatus);

                return Ok(updatedProjectViewModel);
            }
            catch (KeyNotFoundException e)
            {
                r_logger.LogWarning("Project not found: ProjectId={ProjectId}, Error={Error}", projectId, e.Message);
                return NotFound(new { Message = e.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating project status: ProjectId={ProjectId}, Status={Status}", projectId, status.ProjectStatus);
                throw;
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
            r_logger.LogInformation("Updating repository link: ProjectId={ProjectId}, RepositoryLink={RepositoryLink}", projectId, repositoryLink);
            
            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: {ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(repositoryLink))
            {
                r_logger.LogWarning("Invalid repository link provided: ProjectId={ProjectId}, RepositoryLink={RepositoryLink}", projectId, repositoryLink);
                return BadRequest(new { Message = "Repository link cannot be empty." });
            }

            try
            {
                // Validate GitHub URL before persisting
                var isValid = await r_GitHubService.ValidateRepositoryUrlAsync(repositoryLink);
                if (!isValid)
                {
                    r_logger.LogWarning("Invalid GitHub repository URL provided for project {ProjectId}: {RepositoryLink}", projectId, repositoryLink);
                    return BadRequest(new { Message = "Invalid or inaccessible GitHub repository URL. Only *public* repos are supported." });
                }

                UserProject updatedProject = await r_ProjectService.UpdateRepositoryLinkAsync(projectId, repositoryLink);

                UserProjectViewModel userProjectViewModel = new UserProjectViewModel(updatedProject);
                
                r_logger.LogInformation("Successfully updated repository link: ProjectId={ProjectId}, RepositoryLink={RepositoryLink}", projectId, repositoryLink);

                return Ok(userProjectViewModel);
            }
            catch (KeyNotFoundException e)
            {
                r_logger.LogWarning("Project not found: ProjectId={ProjectId}, Error={Error}", projectId, e.Message);
                return NotFound(new { Message = e.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating repository link: ProjectId={ProjectId}, RepositoryLink={RepositoryLink}", projectId, repositoryLink);
                throw;
            }
        }

        /// <summary>
        /// Updates project details including name, description, repository link, picture upload, and other properties.
        /// </summary>
        /// <param name="projectId">The ID of the project to update.</param>
        /// <param name="updateDto">The project update data (supports multipart form data for file uploads).</param>
        /// <returns>The updated project details.</returns>
        [HttpPut("{projectId}/update")]
        public async Task<ActionResult<UserProjectViewModel>> UpdateProject(
            [FromRoute] Guid projectId, 
            [FromForm] ProjectUpdateDto updateDto)
        {
            r_logger.LogInformation("Updating project: ProjectId={ProjectId}", projectId);
            
            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: {ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (updateDto == null)
            {
                r_logger.LogWarning("Update DTO is null for project: {ProjectId}", projectId);
                return BadRequest(new { Message = "Update data cannot be null." });
            }

            try
            {
                var userId = await GetCurrentUserIdAsync();
                await ensureActorCanManageProjectAsync(projectId, userId);

                // Parse form data for complex types if they come as strings
                if (updateDto.Goals == null && !string.IsNullOrEmpty(Request.Form["Goals"]))
                {
                    var goalsString = Request.Form["Goals"].ToString();
                    if (!string.IsNullOrEmpty(goalsString))
                    {
                        updateDto.Goals = goalsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(g => g.Trim()).ToList();
                    }
                }

                if (updateDto.Technologies == null && !string.IsNullOrEmpty(Request.Form["Technologies"]))
                {
                    var technologiesString = Request.Form["Technologies"].ToString();
                    if (!string.IsNullOrEmpty(technologiesString))
                    {
                        updateDto.Technologies = technologiesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim()).ToList();
                    }
                }

                if (updateDto.RequiredRoles == null && !string.IsNullOrEmpty(Request.Form["RequiredRoles"]))
                {
                    var rolesString = Request.Form["RequiredRoles"].ToString();
                    if (!string.IsNullOrEmpty(rolesString))
                    {
                        updateDto.RequiredRoles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(r => r.Trim()).ToList();
                    }
                }

                if (updateDto.ProgrammingLanguages == null && !string.IsNullOrEmpty(Request.Form["ProgrammingLanguages"]))
                {
                    var languagesString = Request.Form["ProgrammingLanguages"].ToString();
                    if (!string.IsNullOrEmpty(languagesString))
                    {
                        updateDto.ProgrammingLanguages = languagesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim()).ToList();
                    }
                }
                
                var updatedProject = await r_ProjectService.UpdateProjectAsync(projectId, updateDto);
                var projectViewModel = new UserProjectViewModel(updatedProject);
                
                r_logger.LogInformation("Successfully updated project: ProjectId={ProjectId}, UserId={UserId}", projectId, userId);
                return Ok(projectViewModel);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning("Unauthorized access: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Project not found: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                r_logger.LogWarning("Invalid argument provided: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                r_logger.LogWarning("Invalid operation: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating project: ProjectId={ProjectId}", projectId);
                throw;
            }
        }


        /// <summary>
        /// Get a project's picture by project ID.
        /// This endpoint acts as a proxy to serve images from private blob storage.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>The project picture image.</returns>
        [HttpGet("{projectId}/picture")]
        public async Task<IActionResult> GetProjectPicture([FromRoute] Guid projectId)
        {
            try
            {
                // Get project to find picture URL
                var project = await r_DbContext.Projects.FindAsync(projectId);
                if (project == null)
                {
                    r_logger.LogWarning("Project not found when requesting picture: ProjectId={ProjectId}", projectId);
                    return NotFound("Project not found");
                }

                if (string.IsNullOrEmpty(project.ProjectPictureUrl))
                {
                    r_logger.LogDebug("Project has no picture: ProjectId={ProjectId}", projectId);
                    return NotFound("No project picture found");
                }

                r_logger.LogDebug("Retrieving project picture: ProjectId={ProjectId}, BlobUrl={BlobUrl}",
                    projectId, project.ProjectPictureUrl);

                // Fetch image from private blob storage
                var blobResult = await r_FileUploadService.GetFileAsync(project.ProjectPictureUrl, "project-pictures");
                if (blobResult == null)
                {
                    r_logger.LogWarning("Project picture blob not found: ProjectId={ProjectId}, BlobUrl={BlobUrl}",
                        projectId, project.ProjectPictureUrl);
                    return NotFound("Project picture not found");
                }

                // Set cache headers for better performance
                Response.Headers.Add("Cache-Control", "public, max-age=3600"); // Cache for 1 hour
                Response.Headers.Add("ETag", $"\"{projectId}\"");

                return File(blobResult.Content, blobResult.ContentType);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving project picture: ProjectId={ProjectId}", projectId);
                return StatusCode(500, "An error occurred while retrieving the project picture");
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
            r_logger.LogInformation("Searching active projects: Query={Query}", searchQuery);
            
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                r_logger.LogWarning("Empty search query provided");
                return BadRequest(new { Message = "Search query cannot be empty." });
            }

            try
            {
                var projects = await r_ProjectService.SearchActiveProjectsByNameOrDescriptionAsync(searchQuery);
                var projectViewModels = projects.Select(p => new ConciseUserProjectViewModel(p, null)).ToList();
                
                r_logger.LogInformation("Search completed: Query={Query}, Results={Count}", searchQuery, projectViewModels.Count);
                return Ok(projectViewModels);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error searching active projects: Query={Query}", searchQuery);
                throw;
            }
        }
        
        /// <summary>
        /// Searches for template projects by name or description.
        /// </summary>
        /// <param name="searchQuery">The search query to match against project names or descriptions.</param>
        /// <returns>A list of matching projects.</returns>
        [HttpGet("search/template")] 
        public async Task<ActionResult<IEnumerable<TemplateProjectViewModel>>> SearchTemplateProjectsByNameOrDescription([FromQuery] string searchQuery)
        {
            r_logger.LogInformation("Searching template projects: Query={Query}", searchQuery);
            
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                r_logger.LogWarning("Empty search query provided");
                return BadRequest(new { Message = "Search query cannot be empty." });
            }

            try
            {
                var projects = await r_ProjectService.SearchTemplateProjectsByNameOrDescriptionAsync(searchQuery);
                var projectViewModels = projects.Select(p => new TemplateProjectViewModel(p)).ToList();
                
                r_logger.LogInformation("Template search completed: Query={Query}, Results={Count}", searchQuery, projectViewModels.Count);
                return Ok(projectViewModels);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error searching template projects: Query={Query}", searchQuery);
                throw;
            }
        }

        /// <summary>
        /// Performs vector-based semantic search for projects using the input query.
        /// </summary>
        /// <param name="query">The user's search text.</param>
        /// <param name="count">Maximum number of results to return (default 3).</param>
        /// <returns>A list of relevant projects based on vector similarity.</returns>
        [HttpGet("search/vector")]
        public async Task<ActionResult<ProjectMatchResultViewModel>> SearchProjectsByVector([FromQuery] string query, [FromQuery] int count = 3)
        {
            r_logger.LogInformation("Performing vector search: Query={Query}, Count={Count}", query, count);
            
            if (string.IsNullOrWhiteSpace(query))
            {
                r_logger.LogWarning("Empty vector search query provided");
                return BadRequest(new { Message = "Search query cannot be empty." });
            }

            try
            {
                ProjectMatchResultDto resultDto = await r_ProjectMatchingService.MatchProjectsByTextAsync(query, count);

                ProjectMatchResultViewModel resultViewModel = new ProjectMatchResultViewModel(resultDto.Projects, resultDto.Explanation);

                r_logger.LogInformation("Vector search completed: Query={Query}, Results={Count}", query, resultDto.Projects.Count());
                return Ok(resultViewModel);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error performing vector search: Query={Query}, Count={Count}", query, count);
                throw;
            }
        }

        /// <summary>
        /// Matches projects to the current authenticated user's profile (Gainer or Mentor).
        /// </summary>
        /// <param name="count">Max number of results (default 3).</param>
        [HttpGet("match/profile")]
        [Authorize(Policy = "RequireAccessAsUser")]
        public async Task<ActionResult<IEnumerable<AzureVectorSearchProjectViewModel>>> MatchByProfile(
             [FromQuery] int count = 3)
        {
            try 
            { 
                var userId = await GetCurrentUserIdAsync();
                r_logger.LogInformation("Matching projects by profile: UserId={UserId}, Count={Count}", userId, count);
                
                var matchedProjects = await r_ProjectMatchingService.MatchProjectsByProfileAsync(userId, count);

                if (matchedProjects == null || !matchedProjects.Any())
                {
                    r_logger.LogWarning("No projects matched for user profile: {UserId}", userId);
                    return NotFound(new { Message = "No projects matched for the given user profile." });
                }

                r_logger.LogInformation("Successfully matched {Count} projects for user profile: {UserId}", matchedProjects.Count(), userId);
                return Ok(matchedProjects);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning("Unauthorized access: Error={Error}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("User profile not found: Error={Error}", ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error matching projects by profile: Count={Count}", count);
                throw;
            }
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
            r_logger.LogInformation("Filtering projects: Status={Status}, Difficulty={Difficulty}", status.ProjectStatus, difficulty.DifficultyLevel);
            
            try
            {
                var projects = await r_ProjectService.FilterActiveProjectsByStatusAndDifficultyAsync(status.ProjectStatus, difficulty.DifficultyLevel);
                var projectViewModels = projects.Select(p => new ConciseUserProjectViewModel(p, null)).ToList();
                
                r_logger.LogInformation("Filter completed: Status={Status}, Difficulty={Difficulty}, Results={Count}", status.ProjectStatus, difficulty.DifficultyLevel, projectViewModels.Count);
                return Ok(projectViewModels);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error filtering projects: Status={Status}, Difficulty={Difficulty}", status.ProjectStatus, difficulty.DifficultyLevel);
                throw;
            }
        }

        /// <summary>
        /// Filters template projects by difficulty level.
        /// </summary>
        /// <param name="difficulty">The difficulty level to filter by.</param>
        /// <returns>A list of filtered template projects.</returns>
        [HttpGet("templates/filter")]
        public async Task<ActionResult<IEnumerable<TemplateProjectViewModel>>> FilterTemplateProjectsByDifficulty([FromQuery] ProjectDifficultyLevelOptionDTO difficulty)
        {
            r_logger.LogInformation("Filtering template projects: Difficulty={Difficulty}", difficulty.DifficultyLevel);
            
            try
            {
                var templates = await r_ProjectService.FilterTemplateProjectsByDifficultyAsync(difficulty.DifficultyLevel);
                var templateViewModels = templates.Select(t => new TemplateProjectViewModel(t)).ToList();
                
                r_logger.LogInformation("Template filter completed: Difficulty={Difficulty}, Results={Count}", difficulty.DifficultyLevel, templateViewModels.Count);
                return Ok(templateViewModels);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error filtering template projects: Difficulty={Difficulty}", difficulty.DifficultyLevel);
                throw;
            }
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
            r_logger.LogInformation("Creating nonprofit project: NonprofitOrgId={NonprofitOrgId}, ProjectName={ProjectName}", 
                nonprofitOrgId, CreateProjectForNonprofitViewModel.ProjectName);
            
            if (nonprofitOrgId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid nonprofit organization ID provided: {NonprofitOrgId}", nonprofitOrgId);
                return BadRequest(new { Message = "Nonprofit organization ID cannot be empty." });
            }

            if (CreateProjectForNonprofitViewModel == null)
            {
                r_logger.LogWarning("Project details are null");
                return BadRequest(new { Message = "Project details cannot be null." });
            }

            try
            {
                UserProject o_Project = await r_ProjectService.CreateProjectForNonprofitAsync(CreateProjectForNonprofitViewModel, nonprofitOrgId);

                UserProjectViewModel userProjectViewModel = new UserProjectViewModel(o_Project);
                
                r_logger.LogInformation("Successfully created nonprofit project: ProjectId={ProjectId}, NonprofitOrgId={NonprofitOrgId}, ProjectName={ProjectName}", 
                    o_Project.ProjectId, nonprofitOrgId, CreateProjectForNonprofitViewModel.ProjectName);

                return CreatedAtAction(nameof(GetActiveProjectById), new { projectId = o_Project.ProjectId }, userProjectViewModel);
            }
            catch (KeyNotFoundException e)
            {
                r_logger.LogWarning("Nonprofit organization not found: NonprofitOrgId={NonprofitOrgId}, Error={Error}", nonprofitOrgId, e.Message);
                return NotFound(new { Message = e.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error creating nonprofit project: NonprofitOrgId={NonprofitOrgId}, ProjectName={ProjectName}", 
                    nonprofitOrgId, CreateProjectForNonprofitViewModel.ProjectName);
                throw;
            }
        }
        #endregion

        /// <summary>
        /// Export all projects for Azure Cognitive Search vector indexing and upload to blob storage
        /// This endpoint updates the projects file in the blob container
        /// </summary>
        /// <returns>Blob URL where the file was uploaded</returns>
        [HttpPost("export-for-azure-vector-search")]
        public async Task<IActionResult> ExportProjectsForAzureVectorSearch()
        {
            try
            {
                r_logger.LogInformation("Starting project export for Azure vector search and blob upload");
                
                var blobUrl = await r_ProjectService.ExportAndUploadProjectsAsync();
                
                r_logger.LogInformation("Successfully exported and uploaded projects to blob storage: BlobUrl={BlobUrl}", blobUrl);
                
                return Ok(new { 
                    message = "Projects exported and uploaded successfully",
                    blobUrl = blobUrl,
                    uploadedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error exporting projects for Azure vector search and blob upload");
                return StatusCode(500, new { error = "Failed to export and upload projects", details = ex.Message });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Helper method to extract claim values from the user principal.
        /// </summary>
        /// <param name="user">The user principal.</param>
        /// <param name="types">The claim types to try.</param>
        /// <returns>The first non-empty claim value found, or null if none found.</returns>
        private static string? tryGetClaim(ClaimsPrincipal user, params string[] types)
        {
            foreach (var t in types)
            {
                var v = user.FindFirstValue(t);
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            return null;
        }

        /// <summary>
        /// Gets the current user ID from the JWT token claims.
        /// </summary>
        /// <returns>The current user ID from the database.</returns>
        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var externalId =
                tryGetClaim(User, "oid", ClaimTypes.NameIdentifier)
            ?? tryGetClaim(User, "sub")
            ?? tryGetClaim(User, ClaimTypes.NameIdentifier)
            ?? tryGetClaim(User, "http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? tryGetClaim(User, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            if (string.IsNullOrEmpty(externalId))
                throw new UnauthorizedAccessException("User ID not found in token.");

            // Find the user in the database by external ID
            var user = await r_DbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ExternalId == externalId);

            if (user == null)
                throw new UnauthorizedAccessException("User not found in database.");

            return user.UserId;
        }

        /// <summary>
        /// Ensures the actor can manage the project (is a project admin or mentor).
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="actorUserId">The actor user ID.</param>
        private async Task ensureActorCanManageProjectAsync(Guid projectId, Guid actorUserId)
        {
            // Check if user is a mentor
            var isMentor = await r_DbContext.Mentors.AnyAsync(m => m.UserId == actorUserId);
            if (isMentor) return;

            // Check if user is a project admin
            var isProjectAdmin = await r_DbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId
                             && pm.UserId == actorUserId
                             && pm.IsAdmin
                             && pm.LeftAtUtc == null);

            if (!isProjectAdmin)
                throw new UnauthorizedAccessException("Only project admins or mentors can update project details.");
        }

        #endregion

    }
}