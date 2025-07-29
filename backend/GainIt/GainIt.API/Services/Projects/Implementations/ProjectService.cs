using GainIt.API.Data;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace GainIt.API.Services.Projects.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly GainItDbContext r_DbContext;

        public ProjectService(GainItDbContext i_DbContext)
        {
            r_DbContext = i_DbContext;
        }

        public async Task<UserProject> AddTeamMemberAsync(Guid i_ProjectId, Guid i_UserId, string i_Role)
        {
            var project = await r_DbContext.Projects
                .Include(p => p.ProjectMembers)
                .ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

            if (project == null)
            {
                throw new KeyNotFoundException("Project not found.");
            }

            var gainer = await r_DbContext.Gainers.FindAsync(i_UserId);
            if (gainer == null)
            {
                throw new KeyNotFoundException("User not found or is not a Gainer.");
            }

            // Check if the role is open in the project
            if (!project.RequiredRoles.Contains(i_Role))
            {
                throw new InvalidOperationException($"Role '{i_Role}' is not an open role in this project.");
            }

            // Check if the role is already filled
            if (project.ProjectMembers.Any(pm =>
                pm.UserRole == i_Role &&
                pm.LeftAtUtc == null))
            {
                throw new InvalidOperationException($"Role '{i_Role}' is already filled in this project.");
            }

            // Check if user is already a member
            if (project.ProjectMembers.Any(pm =>
                pm.UserId == i_UserId &&
                pm.LeftAtUtc == null))
            {
                throw new InvalidOperationException("User is already a team member in this project.");
            }

            project.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = i_ProjectId,
                UserId = i_UserId,
                UserRole = i_Role,
                IsAdmin = false,
                Project = project,
                User = gainer,
                JoinedAtUtc = DateTime.UtcNow
            });

            await r_DbContext.SaveChangesAsync();
            return project;
        }



        public async Task<UserProject> AssignMentorAsync(Guid i_ProjectId, Guid i_MentorId)
        {
            var project = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .FirstOrDefaultAsync(project => project.ProjectId == i_ProjectId);

            if (project == null)
            {
                throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
            }

            var mentor = await r_DbContext.Mentors.FindAsync(i_MentorId);
            if (mentor == null)
            {
                throw new KeyNotFoundException($"Mentor with ID {i_MentorId} not found");
            }

            // Check if mentor is already a project member
            if (project.ProjectMembers.Any(member =>
                member.UserId == i_MentorId &&
                member.LeftAtUtc == null))
            {
                throw new InvalidOperationException($"Mentor {i_MentorId} is already a member of project {i_ProjectId}");
            }

            // Remove current mentor if exists
            await RemoveMentorAsync(i_ProjectId);

            // Add new mentor as a project member
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
            return project;
        }

        public async Task<UserProject> CreateProjectForNonprofitAsync(UserProjectViewModel i_Project, Guid i_NonprofitOrgId)
        {
            var nonprofit = await r_DbContext.Nonprofits.FindAsync(i_NonprofitOrgId);

            if (nonprofit == null)
            {
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

            r_DbContext.Projects.Add(newProject);
            await r_DbContext.SaveChangesAsync();

            return newProject;
        }

        public async Task<IEnumerable<UserProject>> FilterActiveProjectsByStatusAndDifficultyAsync(eProjectStatus i_Status, eDifficultyLevel i_Difficulty)
        {
            return await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Where(project => project.ProjectStatus == i_Status &&
                                project.DifficultyLevel == i_Difficulty &&
                                project.ProjectMembers.Count(projectMember => projectMember.LeftAtUtc == null) > 0)  // Only include projects with active members
                .ToListAsync();
        }

        public async Task<IEnumerable<TemplateProject>> FilterTemplateProjectsByDifficultyAsync(eDifficultyLevel i_Difficulty)
        {
            return await r_DbContext.TemplateProjects
                .Where(templateProject => templateProject.DifficultyLevel == i_Difficulty)
                .ToListAsync();
        }

        public async Task<UserProject?> GetActiveProjectByProjectIdAsync(Guid i_ProjectId)
        {
            return await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .FirstOrDefaultAsync(project =>
                    project.ProjectId == i_ProjectId &&
                    project.ProjectMembers.Count(projectMember => projectMember.LeftAtUtc == null) > 0);

        }

        public async Task<IEnumerable<UserProject>> GetAllNonprofitProjectsAsync()
        {
            return await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Include(project => project.OwningOrganization)
                .Where(project => project.ProjectSource == eProjectSource.NonprofitOrganization)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserProject>> GetAllActiveProjectsAsync()
        {
            
            return await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Include(project => project.OwningOrganization!)
                .ThenInclude(org => org.NonprofitExpertise)
                .Where(project => project.ProjectSource != eProjectSource.Template
                                  && project.ProjectStatus == eProjectStatus.InProgress)
                .ToListAsync();
        }
        public async Task<IEnumerable<UserProject>> GetAllPendingUserTemplatesProjectsAsync()
        {
            return await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Where(project =>
                    project.ProjectSource == eProjectSource.Template &&
                    project.ProjectStatus == eProjectStatus.Pending)
                .ToListAsync();
        }

        public async Task<IEnumerable<TemplateProject>> GetAllTemplatesProjectsAsync()
        {
            return await r_DbContext.TemplateProjects
                .ToListAsync();
        }

        public async Task<IEnumerable<UserProject>> GetProjectsByMentorIdAsync(Guid i_MentorId)
        {
            // Verify mentor exists
            var mentor = await r_DbContext.Mentors.FindAsync(i_MentorId);
            if (mentor == null)
            {
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

            return projects;
        }

        public async Task<IEnumerable<UserProject>> GetProjectsByNonprofitIdAsync(Guid i_NonprofitId)
        {
            return await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Include(project => project.OwningOrganization)
                .Where(project => project.OwningOrganizationUserId == i_NonprofitId)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserProject>> GetProjectsByUserIdAsync(Guid i_UserId)
        {
            return await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(projectMember => projectMember.User)
                .Where(project => project.ProjectMembers
                    .Any(projectMember => projectMember.UserId == i_UserId))
                .ToListAsync();

        }

        public async Task<TemplateProject?> GetTemplateProjectByProjectIdAsync(Guid i_ProjectId)
        {
            return await r_DbContext.TemplateProjects
                .FirstOrDefaultAsync(templateProject => templateProject.ProjectId == i_ProjectId);
        }

        public async Task<UserProject> RemoveMentorAsync(Guid i_ProjectId)
        {
            var project = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(member => member.User)
                .FirstOrDefaultAsync(project => project.ProjectId == i_ProjectId);

            if (project == null)
            {
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
            }

            return project;
        }

        public async Task<UserProject> RemoveTeamMemberAsync(Guid i_ProjectId, Guid i_UserId)
        {
            var project = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(member => member.User)
                .FirstOrDefaultAsync(project => project.ProjectId == i_ProjectId);

            if (project == null)
            {
                throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
            }

            var teamMember = project.ProjectMembers
                .FirstOrDefault(member =>
                    member.UserId == i_UserId &&
                    member.UserRole == "Team Member" &&
                    member.LeftAtUtc == null);

            if (teamMember == null)
            {
                throw new KeyNotFoundException($"Active team member with ID {i_UserId} not found in project {i_ProjectId}");
            }

            teamMember.LeftAtUtc = DateTime.UtcNow;
            await r_DbContext.SaveChangesAsync();

            return project;
        }

        public async Task<IEnumerable<UserProject>> SearchActiveProjectsByNameOrDescriptionAsync(string i_SearchQuery)
        {
            if (string.IsNullOrWhiteSpace(i_SearchQuery))
            {
                throw new ArgumentException("Search query cannot be empty", nameof(i_SearchQuery));
            }

            var searchTerm = i_SearchQuery.ToLower();
            var projects = await r_DbContext.Projects
                .Include(project => project.ProjectMembers)
                .ThenInclude(member => member.User)
                .Where(project =>
                    project.ProjectStatus == eProjectStatus.InProgress &&
                    (project.ProjectName.ToLower().Contains(searchTerm) ||
                     project.ProjectDescription.ToLower().Contains(searchTerm)))
                .ToListAsync();

            return projects;
        }

        public async Task<IEnumerable<TemplateProject>> SearchTemplateProjectsByNameOrDescriptionAsync(string i_SearchQuery)
        {
            if (string.IsNullOrWhiteSpace(i_SearchQuery))
            {
                throw new ArgumentException("Search query cannot be empty", nameof(i_SearchQuery));
            }

            var searchTerm = i_SearchQuery.ToLower();
            var projects = await r_DbContext.TemplateProjects
                .Where(project =>
                    project.ProjectName.ToLower().Contains(searchTerm) ||
                    project.ProjectDescription.ToLower().Contains(searchTerm))
                .ToListAsync();

            return projects;
        }

        public async Task<UserProject> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId)
        {
            // Get the template project
            var template = await r_DbContext.TemplateProjects
                .FirstOrDefaultAsync(t => t.ProjectId == i_TemplateId);

            if (template == null)
            {
                throw new KeyNotFoundException($"Template project with ID {i_TemplateId} not found");
            }

            // Get the user
            var user = await r_DbContext.Users.FindAsync(i_UserId);
            if (user == null)
            {
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

            return newProject;
        }

        public async Task<UserProject> UpdateProjectStatusAsync(Guid i_ProjectId, eProjectStatus i_Status)
        {
            var project = await r_DbContext.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

            if (project == null)
            {
                throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
            }

            project.ProjectStatus = i_Status;
            await r_DbContext.SaveChangesAsync();

            return project;
        }

        public async Task<UserProject> UpdateRepositoryLinkAsync(Guid i_ProjectId, string i_RepositoryLink)
        {
            if (string.IsNullOrWhiteSpace(i_RepositoryLink))
            {
                throw new ArgumentException("Repository link cannot be empty", nameof(i_RepositoryLink));
            }

            var project = await r_DbContext.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

            if (project == null)
            {
                throw new KeyNotFoundException($"Project with ID {i_ProjectId} not found");
            }

            project.RepositoryLink = i_RepositoryLink;
            await r_DbContext.SaveChangesAsync();

            return project;
        }
    }
}
