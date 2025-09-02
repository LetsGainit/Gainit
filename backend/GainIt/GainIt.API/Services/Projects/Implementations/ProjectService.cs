using Azure.AI.OpenAI;
using GainIt.API.Data;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Options;
using GainIt.API.Services.GitHub.Interfaces;
using GainIt.API.Services.Projects.Interfaces;
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
        private readonly AzureOpenAIClient r_azureOpenAIClient;
        private readonly ChatClient r_chatClient;

        public ProjectService(GainItDbContext i_DbContext, ILogger<ProjectService> i_logger, IGitHubService i_gitHubService, AzureOpenAIClient i_azureOpenAIClient, IOptions<OpenAIOptions> i_openAIOptions)
        {
            r_DbContext = i_DbContext;
            r_logger = i_logger;
            r_gitHubService = i_gitHubService;
            r_azureOpenAIClient = i_azureOpenAIClient;
            r_chatClient = i_azureOpenAIClient.GetChatClient(i_openAIOptions.Value.ChatDeploymentName);
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
                    IsAdmin = false,
                    Project = project,
                    User = mentor,
                    JoinedAtUtc = DateTime.UtcNow
                });

                await r_DbContext.SaveChangesAsync();
                
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
                    Duration = i_Project.Duration ?? TimeSpan.Zero,
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

                r_DbContext.Projects.Add(newProject);
                await r_DbContext.SaveChangesAsync();

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
                r_logger.LogInformation("Successfully removed mentor from project: ProjectId={ProjectId}", i_ProjectId);
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
            r_logger.LogInformation("Successfully removed team member from project: ProjectId={ProjectId}, UserId={UserId}", i_ProjectId, i_UserId);

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

        public async Task<UserProject> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId)
        {
            r_logger.LogInformation("Starting project from template: TemplateId={TemplateId}, UserId={UserId}", i_TemplateId, i_UserId);

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
                Goals = template.Goals,
                Technologies = template.Technologies,
                RequiredRoles = template.RequiredRoles

            };

            // Add the user as a project member
            newProject.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = newProject.ProjectId,
                UserId = i_UserId,
                UserRole = "Team Member",
                IsAdmin = true,  // the first user is the administrator 
                Project = newProject,
                User = user,
                JoinedAtUtc = DateTime.UtcNow
            });

            await r_DbContext.Projects.AddAsync(newProject);
            await r_DbContext.SaveChangesAsync();

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
            return project;
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
        /// Export all projects for Azure Cognitive Search vector indexing
        /// Creates the exact JSON structure needed for the projects-rag index
        /// </summary>
        /// <returns>List of projects formatted for Azure Cognitive Search</returns>
        public async Task<List<AzureVectorSearchProjectViewModel>> ExportProjectsForAzureVectorSearchAsync()
        {
            // Get all projects from both tables
            var templateProjects = await r_DbContext.TemplateProjects.ToListAsync();
            var userProjects = await r_DbContext.Projects.ToListAsync();
            
            var allProjects = new List<TemplateProject>();
            allProjects.AddRange(templateProjects);
            allProjects.AddRange(userProjects);
            
                                       return allProjects.Select(p => new AzureVectorSearchProjectViewModel
             {
                 ProjectId = p.ProjectId.ToString(),
                 ProjectName = p.ProjectName,
                 ProjectDescription = p.ProjectDescription,
                 DifficultyLevel = p.DifficultyLevel.ToString(),
                 DurationDays = (int)(p.Duration?.TotalDays ?? 30),
                 Goals = p.Goals?.ToArray() ?? new string[0],
                 Technologies = p.Technologies?.ToArray() ?? new string[0],
                 RequiredRoles = p.RequiredRoles?.ToArray() ?? new string[0],
                 
                 // UserProject-specific fields (if available)
                 ProgrammingLanguages = p is UserProject userProject ? userProject.ProgrammingLanguages?.ToArray() ?? new string[0] : new string[0],
                 ProjectSource = p is UserProject userProject2 ? userProject2.ProjectSource.ToString() : null,
                 ProjectStatus = p is UserProject userProject3 ? userProject3.ProjectStatus.ToString() : null,

                 // RAG context - CRITICAL for vector search
                 RagContext = new RagContextViewModel
                 {
                     SearchableText = p.RagContext?.SearchableText ?? $"{p.ProjectName} - {p.ProjectDescription}",
                     Tags = p.RagContext?.Tags?.ToArray() ?? new string[0],
                     SkillLevels = p.RagContext?.SkillLevels?.ToArray() ?? new string[0],
                     ProjectType = p.RagContext?.ProjectType ?? "general-project",
                     Domain = p.RagContext?.Domain ?? "general",
                     LearningOutcomes = p.RagContext?.LearningOutcomes?.ToArray() ?? new string[0],
                     ComplexityFactors = p.RagContext?.ComplexityFactors?.ToArray() ?? new string[0]
                 }
             }).ToList();
        }




    }
}
