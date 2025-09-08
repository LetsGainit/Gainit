using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GainIt.API.Data;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.DTOs.Requests.Projects;
using GainIt.API.DTOs.Requests.Forum;
using GainIt.API.DTOs.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Models.ProjectForum;
using GainIt.API.Options;
using GainIt.API.Realtime;
using GainIt.API.Services.Email.Interfaces;
using GainIt.API.Services.Forum.Interfaces;
using GainIt.API.Services.GitHub.Interfaces;
using GainIt.API.Services.Projects.Interfaces;
using GainIt.API.Services.FileUpload.Interfaces;
using GainIt.API.Services.Tasks.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace GainIt.API.Services.Projects.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly GainItDbContext r_DbContext;
        private readonly ILogger<ProjectService> r_logger;
        private readonly IGitHubService r_gitHubService;
        private readonly IFileUploadService r_FileUploadService;
        private readonly AzureOpenAIClient r_azureOpenAIClient;
        private readonly ChatClient r_chatClient;
        private readonly BlobServiceClient r_BlobServiceClient;
        private readonly AzureStorageOptions r_AzureStorageOptions;
        private readonly SearchClient r_SearchClient;
        private readonly IPlanningService r_PlanningService;
        private readonly IEmailSender r_EmailSender;
        private readonly IHubContext<NotificationsHub> r_Hub;
        private readonly IForumService r_ForumService;

        public ProjectService(GainItDbContext i_DbContext, ILogger<ProjectService> i_logger, IGitHubService i_gitHubService, IFileUploadService i_FileUploadService, AzureOpenAIClient i_azureOpenAIClient, IOptions<OpenAIOptions> i_openAIOptions, BlobServiceClient i_BlobServiceClient, IOptions<AzureStorageOptions> i_azureStorageOptions, SearchClient i_SearchClient, IPlanningService i_PlanningService, IEmailSender i_EmailSender, IHubContext<NotificationsHub> i_Hub, IForumService i_ForumService)
        {
            r_DbContext = i_DbContext;
            r_logger = i_logger;
            r_gitHubService = i_gitHubService;
            r_FileUploadService = i_FileUploadService;
            r_azureOpenAIClient = i_azureOpenAIClient;
            r_chatClient = i_azureOpenAIClient.GetChatClient(i_openAIOptions.Value.ChatDeploymentName);
            r_BlobServiceClient = i_BlobServiceClient;
            r_AzureStorageOptions = i_azureStorageOptions.Value;
            r_SearchClient = i_SearchClient;
            r_PlanningService = i_PlanningService;
            r_EmailSender = i_EmailSender;
            r_Hub = i_Hub;
            r_ForumService = i_ForumService;
        }
        
        public async Task<UserProject> AssignMentorAsync(Guid i_ProjectId, Guid i_MentorId)
        {
            r_logger.LogInformation("Assigning mentor to project: ProjectId={ProjectId}, MentorId={MentorId}", 
                i_ProjectId, i_MentorId);

            try
            {
                var project = await r_DbContext.Projects
                    .Include(project => project.ProjectMembers)
                    .ThenInclude(projectMember => projectMember.User)
                    .FirstOrDefaultAsync(project => project.ProjectId == i_ProjectId);

                if (project == null)
                {
                    r_logger.LogWarning("Project not found: ProjectId={ProjectId}", i_ProjectId);
                    throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
                }

                var mentor = await r_DbContext.Mentors.FindAsync(i_MentorId);
                if (mentor == null)
                {
                    r_logger.LogWarning("Mentor not found: MentorId={MentorId}", i_MentorId);
                    throw new KeyNotFoundException($"Mentor with ID {i_MentorId} not found");
                }

                // Check if mentor is already a project member
                if (project.ProjectMembers.Any(member =>
                    member.UserId == i_MentorId &&
                    member.LeftAtUtc == null))
                {
                    r_logger.LogWarning("Mentor already a project member: ProjectId={ProjectId}, MentorId={MentorId}", 
                        i_ProjectId, i_MentorId);
                    throw new InvalidOperationException($"Mentor {i_MentorId} is already a member of project {i_ProjectId}");
                }

                // Add mentor as a project member
                project.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = i_ProjectId,
                    UserId = i_MentorId,
                    UserRole = "Mentor",
                    IsAdmin = true,
                    Project = project,
                    User = mentor,
                    JoinedAtUtc = DateTime.UtcNow
                });

                await r_DbContext.SaveChangesAsync();
                
                // Export projects to Azure blob for indexer
                try
                {
                    await ExportAndUploadProjectsAsync();
                    r_logger.LogInformation("Successfully exported projects after mentor assignment: ProjectId={ProjectId}", i_ProjectId);
                }
                catch (Exception exportEx)
                {
                    r_logger.LogWarning(exportEx, "Failed to export projects after mentor assignment: ProjectId={ProjectId}", i_ProjectId);
                    // Don't throw - the main operation succeeded
                }
                
                r_logger.LogInformation("Successfully assigned mentor to project: ProjectId={ProjectId}, MentorId={MentorId}", 
                    i_ProjectId, i_MentorId);
                return project;
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Key not found while assigning mentor: ProjectId={ProjectId}, MentorId={MentorId}, Error={Error}", 
                    i_ProjectId, i_MentorId, ex.Message);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                r_logger.LogWarning("Invalid operation while assigning mentor: ProjectId={ProjectId}, MentorId={MentorId}, Error={Error}", 
                    i_ProjectId, i_MentorId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error assigning mentor to project: ProjectId={ProjectId}, MentorId={MentorId}", 
                    i_ProjectId, i_MentorId);
                throw;
            }
        }

        public async Task<UserProject> CreateProjectForNonprofitAsync(UserProjectViewModel i_Project, Guid i_NonprofitOrgId)
        {
            r_logger.LogInformation("Creating project for nonprofit: NonprofitOrgId={NonprofitOrgId}", i_NonprofitOrgId);

            try
            {
                var nonprofit = await r_DbContext.Nonprofits.FindAsync(i_NonprofitOrgId);

                if (nonprofit == null)
                {
                    r_logger.LogWarning("Nonprofit organization not found: NonprofitOrgId={NonprofitOrgId}", i_NonprofitOrgId);
                    throw new KeyNotFoundException($"Nonprofit organization with ID {i_NonprofitOrgId} not found");
                }

                var newProject = new UserProject
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = i_Project.ProjectName,
                    ProjectDescription = i_Project.ProjectDescription,
                    ProjectStatus = eProjectStatus.Pending,
                    ProjectSource = eProjectSource.NonprofitOrganization,
                    CreatedAtUtc = DateTime.UtcNow,
                    DifficultyLevel = (eDifficultyLevel)Enum.Parse(typeof(eDifficultyLevel), i_Project.DifficultyLevel),
                    ProjectPictureUrl = i_Project.ProjectPictureUrl ?? "",  // what do you think ? is this mandatory ?
                    Duration = TimeSpan.FromDays(i_Project.Duration ?? 0),
                    Goals = i_Project.Goals,
                    Technologies = i_Project.Technologies,
                    RequiredRoles = i_Project.OpenRoles,
                    ProgrammingLanguages = i_Project.ProgrammingLanguages,
                    OwningOrganizationUserId = i_NonprofitOrgId,
                    OwningOrganization = nonprofit,
                    ProjectMembers = new List<ProjectMember>()
                };

                // Generate RAG context for the project
                newProject.RagContext = await GenerateRagContextAsync(newProject);

                // Add the nonprofit organization as the project admin (they own the project)
                newProject.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = newProject.ProjectId,
                    UserId = i_NonprofitOrgId, // Nonprofit organization becomes admin
                    UserRole = "Project Owner", // Nonprofit organization role
                    IsAdmin = true, // Nonprofit organization becomes admin
                    Project = newProject,
                    User = nonprofit, // Nonprofit organization user
                    JoinedAtUtc = DateTime.UtcNow
                });

                // Note: Creator is NOT automatically added as a team member
                // They can join later through join requests if they want to participate
                r_logger.LogInformation("Nonprofit project created with only nonprofit organization as admin. Creator can join later via join request if desired.");

                r_DbContext.Projects.Add(newProject);
                await r_DbContext.SaveChangesAsync();

                // Export projects to Azure blob for indexer
                try
                {
                    await ExportAndUploadProjectsAsync();
                    r_logger.LogInformation("Successfully exported projects after nonprofit project creation: ProjectId={ProjectId}", newProject.ProjectId);
                }
                catch (Exception exportEx)
                {
                    r_logger.LogWarning(exportEx, "Failed to export projects after nonprofit project creation: ProjectId={ProjectId}", newProject.ProjectId);
                    // Don't throw - the main operation succeeded
                }

                r_logger.LogInformation("Successfully created project for nonprofit: ProjectId={ProjectId}, NonprofitOrgId={NonprofitOrgId}", newProject.ProjectId, i_NonprofitOrgId);
                return newProject;
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning("Key not found while creating project for nonprofit: NonprofitOrgId={NonprofitOrgId}, Error={Error}", i_NonprofitOrgId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error creating project for nonprofit: NonprofitOrgId={NonprofitOrgId}", i_NonprofitOrgId);
                throw;
            }
        }

        public async Task<IEnumerable<UserProject>> FilterActiveProjectsByStatusAndDifficultyAsync(eProjectStatus i_Status, eDifficultyLevel i_Difficulty)
        {
            r_logger.LogInformation("Filtering active projects by status and difficulty: Status={Status}, Difficulty={Difficulty}", i_Status, i_Difficulty);

            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Where(project => project.ProjectStatus == i_Status &&
                                project.DifficultyLevel == i_Difficulty &&
                                project.ProjectMembers.Count(projectMember => projectMember.LeftAtUtc == null) > 0)  // Only include projects with active members
                .ToListAsync();

            r_logger.LogInformation("Filtered active projects: Count={Count}", projects.Count);
            return projects;
        }

        public async Task<IEnumerable<TemplateProject>> FilterTemplateProjectsByDifficultyAsync(eDifficultyLevel i_Difficulty)
        {
            r_logger.LogInformation("Filtering template projects by difficulty: Difficulty={Difficulty}", i_Difficulty);

            var projects = await r_DbContext.TemplateProjects
                .Where(templateProject => templateProject.DifficultyLevel == i_Difficulty)
                .ToListAsync();

            r_logger.LogInformation("Filtered template projects: Count={Count}", projects.Count);
            return projects;
        }

        public async Task<UserProject?> GetActiveProjectByProjectIdAsync(Guid i_ProjectId)
        {
            r_logger.LogInformation("Getting active project by ID: ProjectId={ProjectId}", i_ProjectId);

            var project = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .FirstOrDefaultAsync(project =>
                    project.ProjectId == i_ProjectId &&
                    project.ProjectMembers.Count(projectMember => projectMember.LeftAtUtc == null) > 0);

            r_logger.LogInformation("Active project found: ProjectId={ProjectId}", i_ProjectId);
            return project;
        }

        public async Task<IEnumerable<UserProject>> GetAllNonprofitProjectsAsync()
        {
            r_logger.LogInformation("Getting all nonprofit projects");

            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Include(project => project.OwningOrganization)
                .Where(project => project.ProjectSource == eProjectSource.NonprofitOrganization)
                .ToListAsync();

            r_logger.LogInformation("Retrieved all nonprofit projects: Count={Count}", projects.Count);
            return projects;
        }

        public async Task<IEnumerable<UserProject>> GetAllActiveProjectsAsync()
        {
            r_logger.LogInformation("Getting all active projects");
            
            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Include(project => project.OwningOrganization!)
                .ThenInclude(org => org.NonprofitExpertise)
                .Where(project => project.ProjectStatus == eProjectStatus.Pending)
                .ToListAsync();

            r_logger.LogInformation("Retrieved all active projects: Count={Count}", projects.Count);
            return projects;
        }
        public async Task<IEnumerable<UserProject>> GetAllPendingUserTemplatesProjectsAsync()
        {
            r_logger.LogInformation("Getting all pending user template projects");

            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Where(project =>
                    project.ProjectSource == eProjectSource.Template &&
                    project.ProjectStatus == eProjectStatus.Pending)
                .ToListAsync();

            r_logger.LogInformation("Retrieved all pending user template projects: Count={Count}", projects.Count);
            return projects;
        }

        public async Task<IEnumerable<TemplateProject>> GetAllTemplatesProjectsAsync()
        {
            r_logger.LogInformation("Getting all template projects");

            var projects = await r_DbContext.TemplateProjects
                .ToListAsync();

            r_logger.LogInformation("Retrieved all template projects: Count={Count}", projects.Count);
            return projects;
        }

        public async Task<IEnumerable<UserProject>> GetProjectsByMentorIdAsync(Guid i_MentorId)
        {
            r_logger.LogInformation("Getting projects by mentor ID: MentorId={MentorId}", i_MentorId);

            // Verify mentor exists
            var mentor = await r_DbContext.Mentors.FindAsync(i_MentorId);
            if (mentor == null)
            {
                r_logger.LogWarning("Mentor not found: MentorId={MentorId}", i_MentorId);
                throw new KeyNotFoundException($"Mentor with ID {i_MentorId} not found");
            }

            // Get all projects where the mentor is a member
            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(member => member.User)
                .Where(project => project.ProjectMembers
                    .Any(member =>
                        member.UserId == i_MentorId &&
                        member.UserRole == "Mentor"))
                .ToListAsync();

            r_logger.LogInformation("Retrieved projects by mentor ID: MentorId={MentorId}, Count={Count}", i_MentorId, projects.Count);
            return projects;
        }

        public async Task<IEnumerable<UserProject>> GetProjectsByNonprofitIdAsync(Guid i_NonprofitId)
        {
            r_logger.LogInformation("Getting projects by nonprofit ID: NonprofitId={NonprofitId}", i_NonprofitId);

            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Include(project => project.OwningOrganization)
                .Where(project => project.OwningOrganizationUserId == i_NonprofitId)
                .ToListAsync();

            r_logger.LogInformation("Retrieved projects by nonprofit ID: NonprofitId={NonprofitId}, Count={Count}", i_NonprofitId, projects.Count);
            return projects;
        }

        public async Task<IEnumerable<UserProject>> GetProjectsByUserIdAsync(Guid i_UserId)
        {
            r_logger.LogInformation("Getting projects by user ID: UserId={UserId}", i_UserId);

            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Where(project => project.ProjectMembers
                    .Any(projectMember => projectMember.UserId == i_UserId))
                .ToListAsync();

            r_logger.LogInformation("Retrieved projects by user ID: UserId={UserId}, Count={Count}", i_UserId, projects.Count);
            return projects;

        }

        public async Task<TemplateProject?> GetTemplateProjectByProjectIdAsync(Guid i_ProjectId)
        {
            r_logger.LogInformation("Getting template project by ID: ProjectId={ProjectId}", i_ProjectId);

            var template = await r_DbContext.TemplateProjects
                .FirstOrDefaultAsync(templateProject => templateProject.ProjectId == i_ProjectId);

            r_logger.LogInformation("Template project found: ProjectId={ProjectId}", i_ProjectId);
            return template;
        }

        public async Task<UserProject> RemoveMentorAsync(Guid i_ProjectId)
        {
            r_logger.LogInformation("Removing mentor from project: ProjectId={ProjectId}", i_ProjectId);

            var project = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(member => member.User)
                .FirstOrDefaultAsync(project => project.ProjectId == i_ProjectId);

            if (project == null)
            {
                r_logger.LogWarning("Project not found: ProjectId={ProjectId}", i_ProjectId);
                throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
            }

            var currentMentor = project.ProjectMembers
                .FirstOrDefault(member =>
                    member.UserRole == "Mentor" &&
                    member.LeftAtUtc == null);

            if (currentMentor != null)
            {
                currentMentor.LeftAtUtc = DateTime.UtcNow;
                await r_DbContext.SaveChangesAsync();
                
                // Check if project has any active members left (including team members)
                var activeMemberCount = project.ProjectMembers.Count(member => member.LeftAtUtc == null);
                if (activeMemberCount == 0)
                {
                    r_logger.LogWarning("Project has no active members left after mentor removal, deleting orphaned project: ProjectId={ProjectId}", i_ProjectId);
                    r_DbContext.Projects.Remove(project);
                    await r_DbContext.SaveChangesAsync();
                    r_logger.LogInformation("Successfully deleted orphaned project: ProjectId={ProjectId}", i_ProjectId);
                }
                else
                {
                    r_logger.LogInformation("Successfully removed mentor from project: ProjectId={ProjectId}, RemainingActiveMembers={RemainingMembers}", 
                        i_ProjectId, activeMemberCount);
                }

                // Export projects to Azure blob for indexer
                try
                {
                    await ExportAndUploadProjectsAsync();
                    r_logger.LogInformation("Successfully exported projects after mentor removal: ProjectId={ProjectId}", i_ProjectId);
                }
                catch (Exception exportEx)
                {
                    r_logger.LogWarning(exportEx, "Failed to export projects after mentor removal: ProjectId={ProjectId}", i_ProjectId);
                    // Don't throw - the main operation succeeded
                }
            }
            else
            {
                r_logger.LogWarning("No active mentor found to remove: ProjectId={ProjectId}", i_ProjectId);
            }

            return project;
        }

        public async Task<UserProject> RemoveTeamMemberAsync(Guid i_ProjectId, Guid i_UserId)
        {
            r_logger.LogInformation("Removing team member from project: ProjectId={ProjectId}, UserId={UserId}", i_ProjectId, i_UserId);

            var project = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(member => member.User)
                .FirstOrDefaultAsync(project => project.ProjectId == i_ProjectId);

            if (project == null)
            {
                r_logger.LogWarning("Project not found: ProjectId={ProjectId}", i_ProjectId);
                throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
            }

            var teamMember = project.ProjectMembers
                .FirstOrDefault(member =>
                    member.UserId == i_UserId &&
                    member.UserRole == "Team Member" &&
                    member.LeftAtUtc == null);

            if (teamMember == null)
            {
                r_logger.LogWarning("Active team member not found: ProjectId={ProjectId}, UserId={UserId}", i_ProjectId, i_UserId);
                throw new KeyNotFoundException($"Active team member with ID {i_UserId} not found in project {i_ProjectId}");
            }

            teamMember.LeftAtUtc = DateTime.UtcNow;
            await r_DbContext.SaveChangesAsync();
            
            // Check if project has any active members left
            var activeMemberCount = project.ProjectMembers.Count(member => member.LeftAtUtc == null);
            if (activeMemberCount == 0)
            {
                r_logger.LogWarning("Project has no active members left, deleting orphaned project: ProjectId={ProjectId}", i_ProjectId);
                r_DbContext.Projects.Remove(project);
                await r_DbContext.SaveChangesAsync();
                r_logger.LogInformation("Successfully deleted orphaned project: ProjectId={ProjectId}", i_ProjectId);
            }
            else
            {
                r_logger.LogInformation("Successfully removed team member from project: ProjectId={ProjectId}, UserId={UserId}, RemainingActiveMembers={RemainingMembers}", 
                    i_ProjectId, i_UserId, activeMemberCount);
            }

            // Export projects to Azure blob for indexer
            try
            {
                await ExportAndUploadProjectsAsync();
                r_logger.LogInformation("Successfully exported projects after team member removal: ProjectId={ProjectId}", i_ProjectId);
            }
            catch (Exception exportEx)
            {
                r_logger.LogWarning(exportEx, "Failed to export projects after team member removal: ProjectId={ProjectId}", i_ProjectId);
                // Don't throw - the main operation succeeded
            }

            return project;
        }

        public async Task<IEnumerable<UserProject>> SearchActiveProjectsByNameOrDescriptionAsync(string i_SearchQuery)
        {
            r_logger.LogInformation("Searching active projects by name or description: SearchQuery={SearchQuery}", i_SearchQuery);

            if (string.IsNullOrWhiteSpace(i_SearchQuery))
            {
                r_logger.LogWarning("Search query is empty: SearchQuery={SearchQuery}", i_SearchQuery);
                throw new ArgumentException("Search query cannot be empty", nameof(i_SearchQuery));
            }

            var searchTerm = i_SearchQuery.ToLower();
            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(member => member.User)
                .Where(project =>
                    project.ProjectStatus == eProjectStatus.InProgress  &&
                    (project.ProjectName.ToLower().Contains(searchTerm) ||
                     project.ProjectDescription.ToLower().Contains(searchTerm)))
                .ToListAsync();

            r_logger.LogInformation("Searched active projects: Count={Count}", projects.Count);
            return projects;
        }

        public async Task<IEnumerable<TemplateProject>> SearchTemplateProjectsByNameOrDescriptionAsync(string i_SearchQuery)
        {
            r_logger.LogInformation("Searching template projects by name or description: SearchQuery={SearchQuery}", i_SearchQuery);

            if (string.IsNullOrWhiteSpace(i_SearchQuery))
            {
                r_logger.LogWarning("Search query is empty: SearchQuery={SearchQuery}", i_SearchQuery);
                throw new ArgumentException("Search query cannot be empty", nameof(i_SearchQuery));
            }

            var searchTerm = i_SearchQuery.ToLower();
            var projects = await r_DbContext.TemplateProjects
                .Where(project =>
                    project.ProjectName.ToLower().Contains(searchTerm) ||
                    project.ProjectDescription.ToLower().Contains(searchTerm))
                .ToListAsync();

            r_logger.LogInformation("Searched template projects: Count={Count}", projects.Count);
            return projects;
        }

        public async Task<UserProject> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId, string i_SelectedRole)
        {
            r_logger.LogInformation("Starting project from template: TemplateId={TemplateId}, UserId={UserId}, SelectedRole={SelectedRole}", i_TemplateId, i_UserId, i_SelectedRole);

            // Get the template project
            var template = await r_DbContext.TemplateProjects
                .FirstOrDefaultAsync(t => t.ProjectId == i_TemplateId);

            if (template == null)
            {
                r_logger.LogWarning("Template project not found: TemplateId={TemplateId}", i_TemplateId);
                throw new KeyNotFoundException($"Template project with ID {i_TemplateId} not found");
            }

            // Get the user
            var user = await r_DbContext.Users.FindAsync(i_UserId);
            if (user == null)
            {
                r_logger.LogWarning("User not found: UserId={UserId}", i_UserId);
                throw new KeyNotFoundException($"User with ID {i_UserId} not found");
            }

            // Validate that the selected role is one of the template's required roles
            if (string.IsNullOrWhiteSpace(i_SelectedRole))
            {
                r_logger.LogWarning("Selected role is empty: TemplateId={TemplateId}, UserId={UserId}", i_TemplateId, i_UserId);
                throw new ArgumentException("Selected role cannot be empty");
            }

            if (template.RequiredRoles == null || !template.RequiredRoles.Contains(i_SelectedRole))
            {
                r_logger.LogWarning("Selected role is not valid for template: TemplateId={TemplateId}, UserId={UserId}, SelectedRole={SelectedRole}, AvailableRoles={AvailableRoles}", 
                    i_TemplateId, i_UserId, i_SelectedRole, string.Join(", ", template.RequiredRoles ?? new List<string>()));
                throw new ArgumentException($"Selected role '{i_SelectedRole}' is not one of the available roles for this template. Available roles: {string.Join(", ", template.RequiredRoles ?? new List<string>())}");
            }

            // Create new project from template
            var newProject = new UserProject
            {
                ProjectId = Guid.NewGuid(),
                ProjectName = template.ProjectName,
                ProjectDescription = template.ProjectDescription,
                ProjectStatus = eProjectStatus.Pending,
                ProjectSource = eProjectSource.Template,
                CreatedAtUtc = DateTime.UtcNow,
                DifficultyLevel = template.DifficultyLevel,
                ProjectPictureUrl = template.ProjectPictureUrl,
                Duration = template.Duration,
                Goals = template.Goals,
                Technologies = template.Technologies,
                RequiredRoles = template.RequiredRoles

            };

            // Add the user as a project member with their selected role
            newProject.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = newProject.ProjectId,
                UserId = i_UserId,
                UserRole = i_SelectedRole,  // Use the selected role instead of hardcoded "Team Member"
                IsAdmin = true,  // the first user is the administrator 
                Project = newProject,
                User = user,
                JoinedAtUtc = DateTime.UtcNow
            });

            await r_DbContext.Projects.AddAsync(newProject);
            await r_DbContext.SaveChangesAsync();

            // Export projects to Azure blob for indexer
            try
            {
                await ExportAndUploadProjectsAsync();
                r_logger.LogInformation("Successfully exported projects after template project creation: ProjectId={ProjectId}", newProject.ProjectId);
            }
            catch (Exception exportEx)
            {
                r_logger.LogWarning(exportEx, "Failed to export projects after template project creation: ProjectId={ProjectId}", newProject.ProjectId);
                // Don't throw - the main operation succeeded
            }

            r_logger.LogInformation("Successfully started project from template: ProjectId={ProjectId}, TemplateId={TemplateId}, UserId={UserId}", newProject.ProjectId, i_TemplateId, i_UserId);
            return newProject;
        }

        public async Task<UserProject> UpdateProjectStatusAsync(Guid i_ProjectId, eProjectStatus i_Status)
        {
            r_logger.LogInformation("Updating project status: ProjectId={ProjectId}, Status={Status}", i_ProjectId, i_Status);

            var project = await r_DbContext.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

            if (project == null)
            {
                r_logger.LogWarning("Project not found: ProjectId={ProjectId}", i_ProjectId);
                throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
            }

            project.ProjectStatus = i_Status;
            await r_DbContext.SaveChangesAsync();

            // Export projects to Azure blob for indexer
            try
            {
                await ExportAndUploadProjectsAsync();
                r_logger.LogInformation("Successfully exported projects after status update: ProjectId={ProjectId}", i_ProjectId);
            }
            catch (Exception exportEx)
            {
                r_logger.LogWarning(exportEx, "Failed to export projects after status update: ProjectId={ProjectId}", i_ProjectId);
                // Don't throw - the main operation succeeded
            }

            r_logger.LogInformation("Successfully updated project status: ProjectId={ProjectId}, Status={Status}", i_ProjectId, i_Status);
            return project;
        }

        public async Task<UserProject> UpdateRepositoryLinkAsync(Guid i_ProjectId, string i_RepositoryLink)
        {
            r_logger.LogInformation("Updating project repository link: ProjectId={ProjectId}, RepositoryLink={RepositoryLink}", i_ProjectId, i_RepositoryLink);

            if (string.IsNullOrWhiteSpace(i_RepositoryLink))
            {
                r_logger.LogWarning("Repository link is empty: ProjectId={ProjectId}", i_ProjectId);
                throw new ArgumentException("Repository link cannot be empty", nameof(i_RepositoryLink));
            }

            var project = await r_DbContext.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

            if (project == null)
            {
                r_logger.LogWarning("Project not found: ProjectId={ProjectId}", i_ProjectId);
                throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
            }

            // If a GitHub repo is already linked, unlink and delete its data first
            var existingRepo = await r_DbContext.Set<GitHubRepository>()
                .FirstOrDefaultAsync(r => r.ProjectId == i_ProjectId);

            if (existingRepo != null)
            {
                r_logger.LogInformation("Existing GitHub repository found for ProjectId={ProjectId}. Unlinking and deleting related data.", i_ProjectId);
                await r_gitHubService.UnlinkRepositoryAsync(i_ProjectId);
            }

            // Update the project link
            project.RepositoryLink = i_RepositoryLink;
            await r_DbContext.SaveChangesAsync();

            // Attempt to link the new repository via GitHubService (validates URL, stores metadata, branches, etc.)
            var linkResult = await r_gitHubService.LinkRepositoryAsync(i_ProjectId, i_RepositoryLink);
            if (!linkResult.Success)
            {
                r_logger.LogWarning("Failed to link new GitHub repo for ProjectId={ProjectId}: {Message}", i_ProjectId, linkResult.Message);
                throw new InvalidOperationException(linkResult.Message ?? "Failed to link GitHub repository");
            }

            r_logger.LogInformation("Successfully updated and re-linked GitHub repository: ProjectId={ProjectId}, RepositoryLink={RepositoryLink}", i_ProjectId, i_RepositoryLink);
            
            // Export projects to Azure blob for indexer
            try
            {
                await ExportAndUploadProjectsAsync();
                r_logger.LogInformation("Successfully exported projects after repository link update: ProjectId={ProjectId}", i_ProjectId);
            }
            catch (Exception exportEx)
            {
                r_logger.LogWarning(exportEx, "Failed to export projects after repository link update: ProjectId={ProjectId}", i_ProjectId);
                // Don't throw - the main operation succeeded
            }
            
            return project;
        }

        public async Task<UserProject> UpdateProjectAsync(Guid i_ProjectId, ProjectUpdateDto i_UpdateDto)
        {
            r_logger.LogInformation("Updating project details: ProjectId={ProjectId}", i_ProjectId);

            try
            {
                var project = await r_DbContext.Projects
                    .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

                if (project == null)
                {
                    r_logger.LogWarning("Project not found: ProjectId={ProjectId}", i_ProjectId);
                    throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
                }

                // Update fields only if they are provided (not null)
                if (!string.IsNullOrWhiteSpace(i_UpdateDto.ProjectName))
                    project.ProjectName = i_UpdateDto.ProjectName;

                if (!string.IsNullOrWhiteSpace(i_UpdateDto.ProjectDescription))
                    project.ProjectDescription = i_UpdateDto.ProjectDescription;

                if (i_UpdateDto.DifficultyLevel.HasValue)
                    project.DifficultyLevel = i_UpdateDto.DifficultyLevel.Value;

                // Handle project picture - file upload takes precedence over URL
                if (i_UpdateDto.ProjectPicture != null)
                {
                    r_logger.LogInformation("Uploading project picture: ProjectId={ProjectId}, FileName={FileName}, Size={Size}KB",
                        i_ProjectId, i_UpdateDto.ProjectPicture.FileName, i_UpdateDto.ProjectPicture.Length / 1024);

                    // Validate file using image-specific validation
                    if (!r_FileUploadService.IsValidImageFile(i_UpdateDto.ProjectPicture))
                    {
                        throw new ArgumentException("Invalid image file. Please ensure the file is a valid image format and under 10MB.");
                    }

                    // Upload to blob storage
                    var blobUrl = await r_FileUploadService.UploadFileAsync(
                        i_UpdateDto.ProjectPicture,
                        "project-pictures",
                        i_ProjectId.ToString());

                    project.ProjectPictureUrl = blobUrl;
                    r_logger.LogInformation("Project picture uploaded successfully: ProjectId={ProjectId}, BlobUrl={BlobUrl}", i_ProjectId, blobUrl);
                }
                else if (!string.IsNullOrWhiteSpace(i_UpdateDto.ProjectPictureUrl))
                {
                    project.ProjectPictureUrl = i_UpdateDto.ProjectPictureUrl;
                }

                if (i_UpdateDto.Goals != null && i_UpdateDto.Goals.Any())
                    project.Goals = i_UpdateDto.Goals;

                if (i_UpdateDto.Technologies != null && i_UpdateDto.Technologies.Any())
                    project.Technologies = i_UpdateDto.Technologies;

                if (i_UpdateDto.RequiredRoles != null && i_UpdateDto.RequiredRoles.Any())
                    project.RequiredRoles = i_UpdateDto.RequiredRoles;

                if (i_UpdateDto.ProgrammingLanguages != null && i_UpdateDto.ProgrammingLanguages.Any())
                    project.ProgrammingLanguages = i_UpdateDto.ProgrammingLanguages;

                if (i_UpdateDto.ProjectStatus.HasValue)
                    project.ProjectStatus = i_UpdateDto.ProjectStatus.Value;

                // Handle repository link update separately (includes GitHub validation and linking)
                if (!string.IsNullOrWhiteSpace(i_UpdateDto.RepositoryLink))
                {
                    await UpdateRepositoryLinkAsync(i_ProjectId, i_UpdateDto.RepositoryLink);
                }

                await r_DbContext.SaveChangesAsync();

                // Export projects to Azure blob for indexer
                try
                {
                    await ExportAndUploadProjectsAsync();
                    r_logger.LogInformation("Successfully exported projects after project details update: ProjectId={ProjectId}", i_ProjectId);
                }
                catch (Exception exportEx)
                {
                    r_logger.LogWarning(exportEx, "Failed to export projects after project details update: ProjectId={ProjectId}", i_ProjectId);
                    // Don't throw - the main operation succeeded
                }

                r_logger.LogInformation("Successfully updated project details: ProjectId={ProjectId}", i_ProjectId);
                return project;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating project details: ProjectId={ProjectId}", i_ProjectId);
                throw;
            }
        }

        /// <summary>
        /// Generates RAG context for a project to improve search and matching capabilities
        /// </summary>
        /// <param name="project">The project to generate RAG context for</param>
        /// <returns>Generated RAG context</returns>
        private async Task<RagContext> GenerateRagContextAsync(UserProject project)
        {
            r_logger.LogInformation("Generating RAG context for project: ProjectId={ProjectId}, ProjectName={ProjectName}", 
                project.ProjectId, project.ProjectName);

            try
            {
                var ragContext = await GenerateRagContextWithAIAsync(project);
                
                r_logger.LogInformation("Successfully generated RAG context for project: ProjectId={ProjectId}, TagsCount={TagsCount}, SkillLevelsCount={SkillLevelsCount}", 
                    project.ProjectId, ragContext.Tags.Count, ragContext.SkillLevels.Count);

                return ragContext;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error generating RAG context for project: ProjectId={ProjectId}", project.ProjectId);
                throw;
            }
        }

        /// <summary>
        /// Generates RAG context using AI for better accuracy
        /// </summary>
        private async Task<RagContext> GenerateRagContextWithAIAsync(UserProject project)
        {
            try
            {
                var projectInfo = 
                    $"Project Name: {project.ProjectName}\n" +
                    $"Description: {project.ProjectDescription}\n" +
                    $"Technologies: [{string.Join(", ", project.Technologies)}]\n" +
                    $"Required Roles: [{string.Join(", ", project.RequiredRoles)}]\n" +
                    $"Difficulty: {project.DifficultyLevel}\n" +
                    $"Duration: {project.Duration.TotalDays} days\n" +
                    $"Goals: [{string.Join(", ", project.Goals)}]";

                var messages = new ChatMessage[]
                {
                    new SystemChatMessage(
                        "You are an expert at analyzing software projects and generating structured metadata for search and matching. " +
                        "Analyze the project and return ONLY a JSON object with these exact fields:\n" +
                        "{\n" +
                        "  \"searchableText\": \"concise searchable description\",\n" +
                        "  \"tags\": [\"tag1\", \"tag2\", \"tag3\"],\n" +
                        "  \"skillLevels\": [\"beginner\", \"intermediate\", \"advanced\"],\n" +
                        "  \"projectType\": \"web-app|mobile-app|api|data-project|ai-project|general-project\",\n" +
                        "  \"domain\": \"education|healthcare|environment|social-impact|business|technology|general\",\n" +
                        "  \"learningOutcomes\": [\"outcome1\", \"outcome2\", \"outcome3\"],\n" +
                        "  \"complexityFactors\": [\"factor1\", \"factor2\", \"factor3\"]\n" +
                        "}\n\n" +
                        "Rules:\n" +
                        "- SearchableText: Concise 1-2 sentence description combining name and key features\n" +
                        "- Tags: 5-10 relevant, lowercase, hyphenated tags (technologies, concepts, domains)\n" +
                        "- SkillLevels: Based on technologies and difficulty (1-2 levels max)\n" +
                        "- ProjectType: Choose the most appropriate type\n" +
                        "- Domain: Choose the primary domain\n" +
                        "- LearningOutcomes: 3-5 specific skills users will develop\n" +
                        "- ComplexityFactors: 2-4 factors that make this project complex\n" +
                        "- Return ONLY valid JSON, no other text"
                    ),
                    new UserChatMessage($"Analyze this project:\n\n{projectInfo}")
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.1f // Low temperature for consistent results
                };

                ChatCompletion completion = await r_chatClient.CompleteChatAsync(messages, options);
                var response = completion.Content[0].Text.Trim();

                // Parse JSON response
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                
                // Validate that we have all required fields
                if (!result.ContainsKey("searchableText") || !result.ContainsKey("tags") || !result.ContainsKey("skillLevels") ||
                    !result.ContainsKey("projectType") || !result.ContainsKey("domain") || !result.ContainsKey("learningOutcomes") ||
                    !result.ContainsKey("complexityFactors"))
                {
                    throw new InvalidOperationException("AI response missing required fields");
                }

                return new RagContext
                {
                    SearchableText = result["searchableText"]?.ToString() ?? $"{project.ProjectName} - {project.ProjectDescription}",
                    Tags = JsonSerializer.Deserialize<List<string>>(result["tags"]?.ToString() ?? "[]") ?? new List<string>(),
                    SkillLevels = JsonSerializer.Deserialize<List<string>>(result["skillLevels"]?.ToString() ?? "[]") ?? new List<string>(),
                    ProjectType = result["projectType"]?.ToString() ?? "general-project",
                    Domain = result["domain"]?.ToString() ?? "general",
                    LearningOutcomes = JsonSerializer.Deserialize<List<string>>(result["learningOutcomes"]?.ToString() ?? "[]") ?? new List<string>(),
                    ComplexityFactors = JsonSerializer.Deserialize<List<string>>(result["complexityFactors"]?.ToString() ?? "[]") ?? new List<string>()
                };
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error generating RAG context with AI for project: ProjectId={ProjectId}", project.ProjectId);
                throw;
            }
        }

        /// <summary>
        /// Starts a project by changing its status to InProgress and generating a roadmap
        /// </summary>
        public async Task<UserProject> StartProjectAsync(Guid projectId, Guid actorUserId)
        {
            r_logger.LogInformation("Starting project: ProjectId={ProjectId}, ActorUserId={ActorUserId}", projectId, actorUserId);

            try
            {
                // Get the project with team members
                var project = await r_DbContext.Projects
                    .Include(p => p.ProjectMembers.Where(pm => pm.LeftAtUtc == null))
                    .ThenInclude(pm => pm.User)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null)
                {
                    r_logger.LogWarning("Project not found: ProjectId={ProjectId}", projectId);
                    throw new KeyNotFoundException($"Project with ID {projectId} not found");
                }

                // Validate project can be started
                await validateProjectCanBeStartedAsync(project, actorUserId);

                // Change project status to InProgress using existing service method
                var updatedProject = await UpdateProjectStatusAsync(projectId, eProjectStatus.InProgress);
                r_logger.LogInformation("Project status changed to InProgress: ProjectId={ProjectId}", projectId);

                // Generate roadmap automatically
                try
                {
                    var roadmap = await generateProjectRoadmapAsync(updatedProject);
                    r_logger.LogInformation("Roadmap generated: ProjectId={ProjectId}, Milestones={Milestones}, Tasks={Tasks}", 
                        projectId, roadmap.CreatedMilestones.Count, roadmap.CreatedTasks.Count());
                }
                catch (Exception ex)
                {
                    r_logger.LogWarning(ex, "Failed to generate roadmap for project: ProjectId={ProjectId}", projectId);
                    // Don't throw - project was started successfully, roadmap is optional
                }

                // Send notifications to team members
                try
                {
                    await sendProjectStartedNotificationsAsync(updatedProject);
                    r_logger.LogInformation("Project start notifications sent: ProjectId={ProjectId}", projectId);
                }
                catch (Exception ex)
                {
                    r_logger.LogWarning(ex, "Failed to send project start notifications: ProjectId={ProjectId}", projectId);
                    // Don't throw - project was started successfully, notifications are optional
                }

                // Create announcement post in project forum
                try
                {
                    await createProjectStartAnnouncementAsync(updatedProject);
                    r_logger.LogInformation("Project start announcement created: ProjectId={ProjectId}", projectId);
                }
                catch (Exception ex)
                {
                    r_logger.LogWarning(ex, "Failed to create project start announcement: ProjectId={ProjectId}", projectId);
                    // Don't throw - project was started successfully, announcement is optional
                }

                // Export projects to Azure blob for indexer
                try
                {
                    await ExportAndUploadProjectsAsync();
                    r_logger.LogInformation("Successfully exported projects after project start: ProjectId={ProjectId}", projectId);
                }
                catch (Exception exportEx)
                {
                    r_logger.LogWarning(exportEx, "Failed to export projects after project start: ProjectId={ProjectId}", projectId);
                    // Don't throw - project was started successfully, export is optional
                }

                r_logger.LogInformation("Project started successfully: ProjectId={ProjectId}", projectId);
                return updatedProject;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error starting project: ProjectId={ProjectId}, ActorUserId={ActorUserId}", projectId, actorUserId);
                throw;
            }
        }

        /// <summary>
        /// Validates that a project can be started
        /// </summary>
        private async Task validateProjectCanBeStartedAsync(UserProject project, Guid actorUserId)
        {
            // Check if project is in Pending status
            if (project.ProjectStatus != eProjectStatus.Pending)
            {
                throw new InvalidOperationException($"Project must be in Pending status to be started. Current status: {project.ProjectStatus}");
            }

            // Check if user has permission to start the project
            var isProjectAdmin = project.ProjectMembers.Any(pm => pm.UserId == actorUserId && pm.IsAdmin && pm.LeftAtUtc == null);
            var isMentor = await r_DbContext.Mentors.AnyAsync(m => m.UserId == actorUserId);
            
            if (!isProjectAdmin && !isMentor)
            {
                throw new UnauthorizedAccessException("Only project admins or mentors can start a project");
            }

            // Check if project has minimum team members
            var activeMembers = project.ProjectMembers.Count(pm => pm.LeftAtUtc == null);
            if (activeMembers < 1)
            {
                throw new InvalidOperationException("Project must have at least one team member to be started");
            }

            // Check if project has required information
            if (string.IsNullOrWhiteSpace(project.ProjectName))
            {
                throw new InvalidOperationException("Project must have a name to be started");
            }

            if (string.IsNullOrWhiteSpace(project.ProjectDescription))
            {
                throw new InvalidOperationException("Project must have a description to be started");
            }

            if (project.Goals == null || !project.Goals.Any())
            {
                throw new InvalidOperationException("Project must have at least one goal to be started");
            }
        }

        /// <summary>
        /// Generates a roadmap for the project using AI planning
        /// </summary>
        private async Task<PlanApplyResultViewModel> generateProjectRoadmapAsync(UserProject project)
        {
            // Create planning request using project data
            var planRequest = new PlanRequestDto
            {
                Goal = string.Join(", ", project.Goals ?? new List<string>()),
                Constraints = null, // Let AI determine constraints based on project
                PreferredTechnologies = string.Join(", ", project.Technologies ?? new List<string>()),
                StartDateUtc = DateTime.UtcNow,
                TargetDueDateUtc = DateTime.UtcNow.Add(project.Duration)
            };

            // Get team members for planning context
            var teamMembers = project.ProjectMembers
                .Where(pm => pm.LeftAtUtc == null)
                .Select(pm => $"{pm.User.FullName} ({pm.UserRole})")
                .ToList();

            r_logger.LogInformation("Generating roadmap for project: ProjectId={ProjectId}, TeamMembers={TeamCount}", 
                project.ProjectId, teamMembers.Count);

            // Use the existing planning service to generate roadmap
            var roadmap = await r_PlanningService.GenerateForProjectAsync(
                project.ProjectId, 
                planRequest, 
                project.ProjectMembers.FirstOrDefault(pm => pm.IsAdmin && pm.LeftAtUtc == null)?.UserId ?? Guid.Empty);

            r_logger.LogInformation("Roadmap generated successfully: ProjectId={ProjectId}, Milestones={MilestoneCount}, Tasks={TaskCount}", 
                project.ProjectId, roadmap.CreatedMilestones.Count, roadmap.CreatedTasks.Count());

            return roadmap;
        }

        /// <summary>
        /// Sends notifications to all team members about project start
        /// </summary>
        private async Task<int> sendProjectStartedNotificationsAsync(UserProject project)
        {
            var notifiedCount = 0;
            // Get all team members (including the person who started the project)
            var teamMembersToNotify = project.ProjectMembers
                .Where(pm => pm.LeftAtUtc == null)
                .ToList();

            // Get the first admin as the "started by" user for the notification data
            var startedByUser = project.ProjectMembers.FirstOrDefault(pm => pm.IsAdmin && pm.LeftAtUtc == null)?.User;
            if (startedByUser == null)
            {
                r_logger.LogWarning("No admin found for project start notification: ProjectId={ProjectId}", project.ProjectId);
                return 0;
            }

            var notificationData = new ProjectStartedNotificationDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                ProjectDescription = project.Description ?? "",
                StartedByUserName = startedByUser.FullName ?? "Unknown User",
                StartedByUserId = startedByUser.UserId,
                StartedAtUtc = DateTime.UtcNow,
                TeamMembersCount = project.ProjectMembers.Count(pm => pm.LeftAtUtc == null),
                Technologies = project.Technologies ?? new List<string>()
            };

            foreach (var member in teamMembersToNotify)
            {
                if (!string.IsNullOrEmpty(member.User.ExternalId))
                {
                    try
                    {
                        // Send SignalR notification
                        await r_Hub.Clients.User(member.User.ExternalId)
                            .SendAsync(RealtimeEvents.Projects.ProjectStarted, notificationData);

                        // Send email notification
                        await r_EmailSender.SendAsync(
                            member.User.EmailAddress,
                            $"GainIt Notifications: Project '{project.ProjectName}' has started!",
                            $"Hi {member.User.FullName},\n\nGreat news! The project '{project.ProjectName}' has officially started and is now in progress.\n\nProject Description: {project.Description}\nTechnologies: {string.Join(", ", project.Technologies ?? new List<string>())}\nTeam Members: {project.ProjectMembers.Count(pm => pm.LeftAtUtc == null)}\n\nYou can now begin working on your assigned tasks and collaborate with your team members.\n\nGood luck with the project!",
                            null
                        );

                        notifiedCount++;
                        r_logger.LogInformation("Project start notification sent: UserId={UserId}, ExternalId={ExternalId}, ProjectId={ProjectId}", 
                            member.UserId, member.User.ExternalId, project.ProjectId);
                    }
                    catch (Exception ex)
                    {
                        r_logger.LogWarning(ex, "Failed to send notification to team member: UserId={UserId}, ProjectId={ProjectId}", 
                            member.UserId, project.ProjectId);
                    }
                }
                else
                {
                    r_logger.LogWarning("User has no ExternalId for SignalR notification: UserId={UserId}, Email={Email}, ProjectId={ProjectId}", 
                        member.UserId, member.User.EmailAddress, project.ProjectId);
                }
            }

            return notifiedCount;
        }

        /// <summary>
        /// Creates an announcement post in the project forum
        /// </summary>
        private async Task createProjectStartAnnouncementAsync(UserProject project)
        {
            try
            {
                // Get the first admin to be the author of the announcement
                var adminMember = project.ProjectMembers.FirstOrDefault(pm => pm.IsAdmin && pm.LeftAtUtc == null);
                if (adminMember == null)
                {
                    r_logger.LogWarning("No admin found to create project start announcement: ProjectId={ProjectId}", project.ProjectId);
                    return;
                }

                var announcementContent = $@"🎉 **Project '{project.ProjectName}' has officially started!**

We're excited to announce that our project is now **in progress** and ready for development!

**Project Overview:**
{project.Description}

**Technologies:**
{string.Join(", ", project.Technologies ?? new List<string>())}

**Team Members:** {project.ProjectMembers.Count(pm => pm.LeftAtUtc == null)}

**What's Next:**
- Check your assigned tasks in the project dashboard
- Review the project roadmap and milestones
- Start collaborating with your team members
- Use the project forum for discussions and updates

Let's make this project a success! 🚀

*This announcement was automatically created when the project was started.*";

                var createPostDto = new CreateForumPostDto
                {
                    ProjectId = project.ProjectId,
                    Content = announcementContent
                };

                await r_ForumService.CreatePostAsync(createPostDto, adminMember.UserId);

                r_logger.LogInformation("Project start announcement created: ProjectId={ProjectId}, AuthorId={AuthorId}", 
                    project.ProjectId, adminMember.UserId);
            }
            catch (Exception ex)
            {
                r_logger.LogWarning(ex, "Failed to create project start announcement: ProjectId={ProjectId}", project.ProjectId);
                // Don't throw - this is optional functionality
            }
        }

        /// <summary>
        /// Export all projects for Azure Cognitive Search vector indexing and upload to blob storage
        /// Deletes all previous export files to keep only the latest export
        /// </summary>
        /// <returns>Blob URL where the file was uploaded</returns>
        public async Task<string> ExportAndUploadProjectsAsync()
        {
            r_logger.LogInformation("Starting project export for Azure vector search and blob upload");
            
            try
            {
                // First, delete all existing export files to keep only the latest
                await DeleteAllExistingExportFilesAsync();
                
                // Clear the search index to prevent stale data
                await ClearSearchIndexAsync();
                
                var azureVectorProjects = await ExportProjectsForAzureVectorSearchAsync();
                
                // Convert to JSONL format (one JSON object per line)
                var jsonlContent = new System.Text.StringBuilder();
                foreach (var project in azureVectorProjects)
                {
                    jsonlContent.AppendLine(
                        System.Text.Json.JsonSerializer.Serialize(project, new System.Text.Json.JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                        })
                    );
                }
                
                var fileName = $"projects-azure-vector-search-{DateTime.UtcNow:yyyyMMdd-HHmmss}.jsonl";
                var fileContent = jsonlContent.ToString();
                var fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
                
                // Create a memory stream from the file content
                using var memoryStream = new System.IO.MemoryStream(fileBytes);
                
                // Create a FormFile from the memory stream
                var formFile = new Microsoft.AspNetCore.Http.FormFile(memoryStream, 0, fileBytes.Length, fileName, fileName)
                {
                    Headers = new Microsoft.AspNetCore.Http.HeaderDictionary(),
                    ContentType = "application/jsonl"
                };
                
                // Upload to blob storage in projects container
                var blobUrl = await r_FileUploadService.UploadFileAsync(
                    formFile, 
                    r_AzureStorageOptions.ProjectsContainerName);
                
                r_logger.LogInformation("Successfully exported and uploaded projects to blob storage: FileName={FileName}, BlobUrl={BlobUrl}", 
                    fileName, blobUrl);
                
                return blobUrl;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error exporting projects for Azure vector search and blob upload");
                throw;
            }
        }

        /// <summary>
        /// Deletes all existing export files from the projects container
        /// </summary>
        private async Task DeleteAllExistingExportFilesAsync()
        {
            r_logger.LogInformation("Deleting all existing export files from blob storage");
            
            try
            {
                var containerClient = r_BlobServiceClient.GetBlobContainerClient(r_AzureStorageOptions.ProjectsContainerName);
                var deletedCount = 0;
                
                // List all blobs that match our export pattern
                // Note: FileUploadService generates GUID names, so we look for .jsonl files
                await foreach (var blob in containerClient.GetBlobsAsync())
                {
                    if (blob.Name.EndsWith(".jsonl"))
                    {
                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        await blobClient.DeleteIfExistsAsync();
                        deletedCount++;
                        r_logger.LogDebug("Deleted old export file: {BlobName}", blob.Name);
                    }
                }
                
                r_logger.LogInformation("Successfully deleted {DeletedCount} old export files", deletedCount);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error deleting old export files");
                // Don't throw - we want to continue with the export even if cleanup fails
            }
        }

        /// <summary>
        /// Clears the Azure Cognitive Search index to prevent stale data
        /// This ensures that only current projects are returned in search results
        /// </summary>
        private async Task ClearSearchIndexAsync()
        {
            r_logger.LogInformation("Clearing Azure Cognitive Search index to prevent stale data");
            
            try
            {
                // Get all documents from the search index
                var searchOptions = new SearchOptions
                {
                    Size = 1000, // Get up to 1000 documents at a time
                    Select = { "chunk_id" } // Only select the key field for deletion
                };

                var documentsToDelete = new List<string>();
                
                // Search for all documents in the index
                var results = await r_SearchClient.SearchAsync<JsonElement>(null, searchOptions);
                
                await foreach (var result in results.Value.GetResultsAsync())
                {
                    // Extract the chunk_id from the document
                    if (result.Document.TryGetProperty("chunk_id", out var chunkIdElement))
                    {
                        var chunkId = chunkIdElement.GetString();
                        if (!string.IsNullOrEmpty(chunkId))
                        {
                            documentsToDelete.Add(chunkId);
                        }
                    }
                }

                if (documentsToDelete.Count > 0)
                {
                    r_logger.LogInformation("Found {DocumentCount} documents to delete from search index", documentsToDelete.Count);
                    
                    // Delete documents in batches
                    var batchSize = 50; // Azure Cognitive Search batch limit
                    for (int i = 0; i < documentsToDelete.Count; i += batchSize)
                    {
                        var batch = documentsToDelete.Skip(i).Take(batchSize);
                        
                        // Create batch with proper constructor
                        var batchDocuments = new IndexDocumentsBatch<SearchDocument>();
                        
                        foreach (var id in batch)
                        {
                            var doc = new SearchDocument { ["chunk_id"] = id };
                            batchDocuments.Actions.Add(IndexDocumentsAction.Delete(doc));
                        }
                        
                        await r_SearchClient.IndexDocumentsAsync(batchDocuments);
                        
                        r_logger.LogInformation("Deleted batch {BatchNumber} of documents from search index", (i / batchSize) + 1);
                    }
                    
                    r_logger.LogInformation("Successfully cleared {DocumentCount} documents from search index", documentsToDelete.Count);
                }
                else
                {
                    r_logger.LogInformation("No documents found in search index to delete");
                }
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error clearing search index");
                // Don't throw - we want to continue with the export even if index clearing fails
                // The indexer will still work, just might have some stale data temporarily
            }
        }




                /// <summary>
        /// Export all projects for Azure Cognitive Search vector indexing
        /// Creates the exact JSON structure needed for the projects-rag index
        /// </summary>
        /// <returns>List of projects formatted for Azure Cognitive Search</returns>
        public async Task<List<AzureVectorSearchProjectViewModel>> ExportProjectsForAzureVectorSearchAsync()
        {
            r_logger.LogInformation("Starting project export for Azure vector search");
            
            try
            {
                // Get all projects from both tables with proper includes
                var templateProjects = await r_DbContext.TemplateProjects
                    .Include(p => p.RagContext)
                    .ToListAsync();
                    
                var userProjects = await r_DbContext.Projects
                    .Include(p => p.RagContext)
                    .ToListAsync();
                
                r_logger.LogInformation("Retrieved projects: Template={TemplateCount}, User={UserCount}", 
                    templateProjects.Count, userProjects.Count);
                
                // Log all project IDs for debugging
                r_logger.LogInformation("Template project IDs: {TemplateIds}", 
                    string.Join(", ", templateProjects.Select(p => p.ProjectId)));
                r_logger.LogInformation("User project IDs: {UserIds}", 
                    string.Join(", ", userProjects.Select(p => p.ProjectId)));
                
                var allProjects = new List<AzureVectorSearchProjectViewModel>();
                
                // Use a single HashSet to track ALL processed project IDs across both collections
                var processedAllIds = new HashSet<string>();
                
                // Process template projects first
                foreach (var project in templateProjects)
                {
                    var projectId = project.ProjectId.ToString();
                    var projectName = project.ProjectName?.Trim();
                    
                    // Skip if project name is empty or if we've already processed this ID anywhere
                    if (string.IsNullOrEmpty(projectName) || processedAllIds.Contains(projectId))
                    {
                        r_logger.LogWarning("Skipping template project: ID={ProjectId}, Name={ProjectName}, Reason={Reason}", 
                            projectId, projectName, 
                            string.IsNullOrEmpty(projectName) ? "Empty name" : "Duplicate ID across all projects");
                        continue;
                    }
                    
                    processedAllIds.Add(projectId);
                    
                    var exportProject = new AzureVectorSearchProjectViewModel
                    {
                        ProjectId = projectId,
                        ProjectName = projectName,
                        ProjectDescription = project.ProjectDescription,
                        DifficultyLevel = project.DifficultyLevel.ToString(),
                        DurationDays = (int)(project.Duration.TotalDays > 0 ? project.Duration.TotalDays : 30),
                        Goals = project.Goals?.ToArray() ?? Array.Empty<string>(),
                        Technologies = project.Technologies?.ToArray() ?? Array.Empty<string>(),
                        RequiredRoles = project.RequiredRoles?.ToArray() ?? Array.Empty<string>(),
                        ProgrammingLanguages = Array.Empty<string>(), // Template projects don't have this
                        ProjectSource = "Template",
                        ProjectStatus = "NotActive",
                        
                        // RAG context - ensure it exists
                        RagContext = new RagContextViewModel
                        {
                            SearchableText = project.RagContext?.SearchableText ?? $"{project.ProjectName} - {project.ProjectDescription}",
                            Tags = project.RagContext?.Tags?.ToArray() ?? Array.Empty<string>(),
                            SkillLevels = project.RagContext?.SkillLevels?.ToArray() ?? Array.Empty<string>(),
                            ProjectType = project.RagContext?.ProjectType ?? "general-project",
                            Domain = project.RagContext?.Domain ?? "general",
                            LearningOutcomes = project.RagContext?.LearningOutcomes?.ToArray() ?? Array.Empty<string>(),
                            ComplexityFactors = project.RagContext?.ComplexityFactors?.ToArray() ?? Array.Empty<string>()
                        }
                    };
                    
                    allProjects.Add(exportProject);
                }
                
                // Process user projects - now checking against ALL previously processed IDs
                foreach (var userProject in userProjects)
                {
                    var projectId = userProject.ProjectId.ToString();
                    var projectName = userProject.ProjectName?.Trim();
                    
                    // Skip if project name is empty or if we've already processed this ID anywhere
                    if (string.IsNullOrEmpty(projectName) || processedAllIds.Contains(projectId))
                    {
                        r_logger.LogWarning("Skipping user project: ID={ProjectId}, Name={ProjectName}, Reason={Reason}", 
                            projectId, projectName, 
                            string.IsNullOrEmpty(projectName) ? "Empty name" : "Duplicate ID across all projects");
                        continue;
                    }
                    
                    processedAllIds.Add(projectId);
                    
                    var exportProject = new AzureVectorSearchProjectViewModel
                    {
                        ProjectId = projectId,
                        ProjectName = projectName,
                        ProjectDescription = userProject.ProjectDescription,
                        DifficultyLevel = userProject.DifficultyLevel.ToString(),
                        DurationDays = (int)(userProject.Duration.TotalDays > 0 ? userProject.Duration.TotalDays : 30),
                        Goals = userProject.Goals?.ToArray() ?? Array.Empty<string>(),
                        Technologies = userProject.Technologies?.ToArray() ?? Array.Empty<string>(),
                        RequiredRoles = userProject.RequiredRoles?.ToArray() ?? Array.Empty<string>(),
                        ProgrammingLanguages = userProject.ProgrammingLanguages?.ToArray() ?? Array.Empty<string>(),
                        ProjectSource = userProject.ProjectSource.ToString(),
                        ProjectStatus = userProject.ProjectStatus.ToString(),
                        
                        // RAG context - ensure it exists
                        RagContext = new RagContextViewModel
                        {
                            SearchableText = userProject.RagContext?.SearchableText ?? $"{userProject.ProjectName} - {userProject.ProjectDescription}",
                            Tags = userProject.RagContext?.Tags?.ToArray() ?? Array.Empty<string>(),
                            SkillLevels = userProject.RagContext?.SkillLevels?.ToArray() ?? Array.Empty<string>(),
                            ProjectType = userProject.RagContext?.ProjectType ?? "general-project",
                            Domain = userProject.RagContext?.Domain ?? "general",
                            LearningOutcomes = userProject.RagContext?.LearningOutcomes?.ToArray() ?? Array.Empty<string>(),
                            ComplexityFactors = userProject.RagContext?.ComplexityFactors?.ToArray() ?? Array.Empty<string>()
                        }
                    };
                    
                    allProjects.Add(exportProject);
                    r_logger.LogDebug("Exported user project: {ProjectId} - {ProjectName}", projectId, projectName);
                }
                
                r_logger.LogInformation("Successfully exported {Count} unique projects for Azure vector search", allProjects.Count);
                r_logger.LogInformation("Exported project IDs: {ExportedIds}", 
                    string.Join(", ", allProjects.Select(p => p.ProjectId)));
                return allProjects;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error exporting projects for Azure vector search");
                throw;
            }
        }




    }
}
