using GainIt.API.Data;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Enums.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Services.Projects.Interfaces;
using GainIt.API.ViewModels.Projects;
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

        //method to load a project with all its related data
        private async Task<Project> loadFullProjectAsync(Guid projectId)
        {
            return await r_DbContext.Projects
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .FirstAsync(p => p.ProjectId == projectId);
        }

        // Retrieve a project by its ID
        public async Task<ProjectViewModel?> GetProjectByProjectIdAsync(Guid i_ProjectId)
        {
            Project? o_project = await r_DbContext.Projects
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId);

            return o_project == null ? null : new ProjectViewModel(o_project);
        }

        // Retrieve all projects that are templates
        public async Task<IEnumerable<ProjectViewModel>> GetAllTemplatesProjectsAsync()
        {
            var o_project = await r_DbContext.Projects
                .Where(p => p.ProjectSource == eProjectSource.Template)
                .ToListAsync();

            return o_project.Select(p => new ProjectViewModel(p));  // consider doing a different model for templates
        }

        public async Task<IEnumerable<ProjectViewModel> GetAllPendingTemplatesProjectsAsync()
        {
            var o_project = await r_DbContext.Projects
                .Where(p => p.ProjectSource == eProjectSource.Template && p.ProjectStatus == eProjectStatus.Pending)
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .ToListAsync();
            return o_project.Select(p => new ProjectViewModel(p));
        }

        // Retrieve all projects that are nonprofit projects
        public async Task<IEnumerable<ProjectViewModel>> GetAllNonprofitProjectsAsync()
        {
            var o_project = await r_DbContext.Projects
                .Where(p => p.ProjectSource == eProjectSource.NonprofitOrganization)
                .Include(p => p.TeamMembers)
                .Include(p => p.OwningOrganization)
                .ToListAsync();

            return o_project.Select(p => new ProjectViewModel(p));
        }

        // Retrieve all projects that are assigned to a specific user
        public async Task<IEnumerable<ProjectViewModel>> GetProjectsByUserIdAsync(Guid i_UserId)
        {
            var o_project = await r_DbContext.Projects
                .Where(p => p.TeamMembers.Any(u => u.UserId == i_UserId))
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .ToListAsync();
                
            return o_project.Select(p => new ProjectViewModel(p));
        }

        // Retrieve all projects that are assigned to a specific mentor
        public async Task<IEnumerable<ProjectViewModel>> GetProjectsByMentorIdAsync(Guid i_MentorId)
        {
            var o_project = await r_DbContext.Projects
                .Where(p => p.AssignedMentor.UserId == i_MentorId)
                .Include(p => p.TeamMembers)
                .Include(p => p.OwningOrganization)
                .ToListAsync();

            return o_project.Select(p => new ProjectViewModel(p));
        }

        // Retrieve all projects that are owned by a specific nonprofit organization
        public async Task<IEnumerable<ProjectViewModel>> GetProjectsByNonprofitIdAsync(Guid i_NonprofitId)
        {
            var o_project = await r_DbContext.Projects
                .Where(p => p.OwningOrganization.UserId == i_NonprofitId)
                .Include(p => p.OwningOrganization)
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .ToListAsync();

            return o_project.Select(p => new ProjectViewModel(p));
        }

        // Update the status of an existing project
        public async Task<ProjectViewModel> UpdateProjectStatusAsync(Guid i_ProjectId, eProjectStatus i_Status)
        {
            Project? o_Project = await loadFullProjectAsync(i_ProjectId); // Get the project by ID

            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found"); // Throw an exception if the project is not found
            }
            o_Project.ProjectStatus = i_Status; // Update the project status
            await r_DbContext.SaveChangesAsync(); // Save changes to the database

            return new ProjectViewModel(o_Project); // Return the updated project as a view model
        }

        // Assign a mentor to a project
        public async Task<ProjectViewModel> AssignMentorAsync(Guid i_ProjectId, Guid i_MentorId)
        {
            Project? o_Project = await r_DbContext.Projects.FindAsync(i_ProjectId); // Get the project by ID

            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found");
            }

            Mentor? o_Mentor = await r_DbContext.Mentors.FindAsync(i_MentorId); // Get the mentor by ID

            if (o_Mentor == null)
            {
                throw new KeyNotFoundException("Mentor not found"); // Throw an exception if the mentor is not found
            }
            o_Project.AssignedMentor = o_Mentor; // Assign the mentor to the project
            await r_DbContext.SaveChangesAsync();

            return new ProjectViewModel(await loadFullProjectAsync(i_ProjectId)); // Return the updated project as a view model
        }

        // Remove a mentor from a project
        public async Task<ProjectViewModel> RemoveMentorAsync(Guid i_ProjectId)
        {
            Project? o_Project = await r_DbContext.Projects.FindAsync(i_ProjectId); // Get the project by ID
            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found");
            }
            o_Project.AssignedMentor = null;
            o_Project.AssignedMentorUserId = null; // Remove the mentor from the project
            await r_DbContext.SaveChangesAsync();

            return new ProjectViewModel(await loadFullProjectAsync(i_ProjectId));
        }

        // Update the repository link of a project
        public async Task<ProjectViewModel> UpdateRepositoryLinkAsync(Guid i_ProjectId, string i_RepositoryLink)
        {
          Project? o_Project = await r_DbContext.Projects.FindAsync(i_ProjectId); 
            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found");
            }
            o_Project.RepositoryLink = i_RepositoryLink; // Update the repository link
            await r_DbContext.SaveChangesAsync();

            return new ProjectViewModel(await loadFullProjectAsync(i_ProjectId)); // Return the updated project as a view model
        }

        // Add a team member to a project
        public async Task<ProjectViewModel> AddTeamMemberAsync(Guid i_ProjectId, Guid i_UserId)
        {
            var o_Project = await r_DbContext.Projects
                .Include(p => p.TeamMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId); // Get the project by ID including team members

            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found.");
            }

            var o_Gainer = await r_DbContext.Gainers.FindAsync(i_UserId); // Get the user by ID

            if (o_Gainer == null)
            {
                throw new KeyNotFoundException("User not found or is not a Gainer."); // Throw an exception if the user is not found
            }

            if (o_Project.TeamMembers.Any(u => u.UserId == i_UserId)) // Check if the user is already a team member
            {
                throw new InvalidOperationException("User is already a team member in this project.");
            }

            o_Project.TeamMembers.Add(o_Gainer); // Add the user to the project team
            await r_DbContext.SaveChangesAsync();

            return new ProjectViewModel(await loadFullProjectAsync(i_ProjectId)); // Return the updated project as a view model
        }

        // Remove a team member from a project
        public async Task<ProjectViewModel> RemoveTeamMemberAsync(Guid i_ProjectId, Guid i_UserId)
        {
            var o_Project = await r_DbContext.Projects
                .Include(p => p.TeamMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId); // Get the project by ID including team members
            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found.");
            }
            var o_Gainer = await r_DbContext.Gainers.FindAsync(i_UserId); // Get the user by ID
            if (o_Gainer == null)
            {
                throw new KeyNotFoundException("User not found or is not a Gainer.");
            }
            if (!o_Project.TeamMembers.Any(u => u.UserId == i_UserId)) // Check if the user is a team member
            {
                throw new InvalidOperationException("User is not a team member in this project.");
            }
            o_Project.TeamMembers.Remove(o_Gainer); // Remove the user from the project team
            await r_DbContext.SaveChangesAsync();

            return new ProjectViewModel(await loadFullProjectAsync(i_ProjectId)); // Return the updated project as a view model
        }

        // Search for projects by name or description
        public async Task<IEnumerable<ProjectViewModel>> SearchProjectsByNameOrDescriptionAsync(string i_SearchQuery)
        {
            var o_project = await r_DbContext.Projects
                .Where(p => p.ProjectName.Contains(i_SearchQuery) || p.ProjectDescription.Contains(i_SearchQuery))
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .ToListAsync();


            return o_project.Select(p => new ProjectViewModel(p));
        }

        // Filter projects by status and difficulty level
        public async Task<IEnumerable<ProjectViewModel>> FilterProjectsByStatusAndDifficultyAsync(eProjectStatus i_Status,
            eDifficultyLevel i_Difficulty)
        {
            var o_projects = await r_DbContext.Projects
                .Where(p => p.ProjectStatus == i_Status && p.DifficultyLevel == i_Difficulty)
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .ToListAsync();

            return o_projects.Select(p => new ProjectViewModel(p));
        }

        public async Task<ProjectViewModel> StartProjectFromTemplateAsync(Guid i_TemplateId, Guid i_UserId)
        {
            // Get the template project by ID
            Project? o_project = await r_DbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == i_TemplateId && p.ProjectSource == eProjectSource.Template);

            if (o_project == null)
            {
                throw new KeyNotFoundException("Template project not found");
            }

            // Get the user (Gainer) by ID
            Gainer? o_Gainer = await r_DbContext.Gainers.FindAsync(i_UserId);

            if (o_Gainer == null)
            {
                throw new KeyNotFoundException("Gainer not found");
            }

            // Create a new project based on the template
            Project o_newProject = new Project
            {
                ProjectName = o_project.ProjectName,
                ProjectDescription = o_project.ProjectDescription,
                ProjectStatus = eProjectStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                DifficultyLevel = o_project.DifficultyLevel,
                ProjectSource = eProjectSource.Template,
                TeamMembers = new List<Gainer> { o_Gainer }, // add the Gainer as a team member
                RepositoryLink = o_project.RepositoryLink
            };

            // Add the new project to the database
            r_DbContext.Projects.Add(o_newProject);
            await r_DbContext.SaveChangesAsync();

            return new ProjectViewModel(await loadFullProjectAsync(o_newProject.ProjectId));
        }


        public async Task<ProjectViewModel> CreateProjectForNonprofitAsync(ProjectViewModel i_Project, Guid i_NonprofitOrgId)
        {
            // Get the nonprofit organization by ID
            NonprofitOrganization? o_Nonprofit = await r_DbContext.Nonprofits.FindAsync(i_NonprofitOrgId);

            Project o_NonprofitNewProject = new Project
            {
                ProjectName = i_Project.projectName,
                ProjectDescription = i_Project.projectDescription,
                ProjectStatus = eProjectStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                ProjectSource = eProjectSource.NonprofitOrganization,
                OwningOrganization = o_Nonprofit,
                TeamMembers = new List<Gainer>(), // Initialize with an empty list
            };

            // Add the new project to the database
            r_DbContext.Projects.Add(o_NonprofitNewProject);
            await r_DbContext.SaveChangesAsync();

            // Return the created project as a view model
            return new ProjectViewModel(await loadFullProjectAsync(o_NonprofitNewProject.ProjectId));
        }
    }
}
