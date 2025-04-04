using GainIt.API.Data;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Enums.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
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

        // Retrieve a project by its ID
        public ProjectViewModel? GetProjectByProjectId(Guid i_ProjectId)
        {
            Project? o_project = r_DbContext.Projects
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .FirstOrDefault(p => p.ProjectId == i_ProjectId);

            return o_project == null ? null : new ProjectViewModel(o_project);
        }

        // Retrieve all projects that are templates
        public IEnumerable<ProjectViewModel> GetAllTemplatesProjects()
        {
            return r_DbContext.Projects
                .Where(p => p.ProjectSource == eProjectSource.Template)
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .ToList()
                .Select(p => new ProjectViewModel(p));
        }

        // Retrieve all projects that are nonprofit projects
        public IEnumerable<ProjectViewModel> GetAllNonprofitProjects()
        {
            return r_DbContext.Projects
                .Where(p => p.ProjectSource == eProjectSource.NonprofitOrganization)
                .Include(p => p.TeamMembers)
                .Include(p => p.OwningOrganization)
                .ToList()
                .Select(p => new ProjectViewModel(p));
        }

        // Retrieve all projects that are built-in projects
        public IEnumerable<ProjectViewModel> GetProjectsByUserId(Guid i_UserId)
        {
            return r_DbContext.Projects
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .ToList()
                .Where(p => p.TeamMembers.Any(u => u.UserId == i_UserId)) 
                .Select(p => new ProjectViewModel(p));
        }

        // Retrieve all projects that are assigned to a specific mentor
        public IEnumerable<ProjectViewModel> GetProjectsByMentorId(Guid i_MentorId)
        {
            return r_DbContext.Projects
                .Where(p => p.AssignedMentor.UserId == i_MentorId)
                .Include(p => p.TeamMembers)
                .Include(p => p.OwningOrganization)
                .ToList()
                .Select(p => new ProjectViewModel(p));
        }

        // Retrieve all projects that are owned by a specific nonprofit organization
        public IEnumerable<ProjectViewModel> GetProjectsByNonprofitId(Guid i_NonprofitId)
        {
            return r_DbContext.Projects
                .Where(p => p.OwningOrganization.UserId == i_NonprofitId)
                .Include(p => p.OwningOrganization)
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .ToList()
                .Select(p => new ProjectViewModel(p));
        }

        // Add a new project to the database
        public void AddProject(Project i_Project)
        {
            r_DbContext.Projects.Add(i_Project);
            r_DbContext.SaveChanges();
        }

        // Update the status of an existing project
        public void UpdateProjectStatus(Guid i_ProjectId, eProjectStatus i_Status)
        {
            Project? o_Project = r_DbContext.Projects.Find(i_ProjectId); // Get the project by ID

            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found"); // Throw an exception if the project is not found
            }
            o_Project.ProjectStatus = i_Status; // Update the project status
            r_DbContext.SaveChanges(); // Save changes to the database

        }

        // Assign a mentor to a project
        public void AssignMentor(Guid i_ProjectId, Guid i_MentorId)
        {
            Project? o_Project = r_DbContext.Projects.Find(i_ProjectId); // Get the project by ID

            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found");
            }

            Mentor? o_Mentor = r_DbContext.Mentors.Find(i_MentorId); // Get the mentor by ID

            if (o_Mentor == null)
            {
                throw new KeyNotFoundException("Mentor not found"); // Throw an exception if the mentor is not found
            }
            o_Project.AssignedMentor = o_Mentor; // Assign the mentor to the project
            r_DbContext.SaveChanges();
        }

        // Remove a mentor from a project
        public void RemoveMentor(Guid i_ProjectId)
        {
            Project? o_Project = r_DbContext.Projects.Find(i_ProjectId); // Get the project by ID
            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found");
            }
            o_Project.AssignedMentor = null; // Remove the mentor from the project
            r_DbContext.SaveChanges();
        }

        // Update the repository link of a project
        public void UpdateRepositoryLink(Guid i_ProjectId, string i_RepositoryLink)
        {
          Project? o_Project = r_DbContext.Projects.Find(i_ProjectId); 
            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found");
            }
            o_Project.RepositoryLink = i_RepositoryLink; // Update the repository link
            r_DbContext.SaveChanges();
        }

        // Add a team member to a project
        public void AddTeamMember(Guid i_ProjectId, Guid i_UserId)
        {
            var o_Project = r_DbContext.Projects
                .Include(p => p.TeamMembers)
                .FirstOrDefault(p => p.ProjectId == i_ProjectId); // Get the project by ID including team members

            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found.");
            }

            var o_Gainer = r_DbContext.Gainers.Find(i_UserId); // Get the user by ID

            if (o_Gainer == null)
            {
                throw new KeyNotFoundException("User not found or is not a Gainer."); // Throw an exception if the user is not found
            }

            if (o_Project.TeamMembers.Any(u => u.UserId == i_UserId)) // Check if the user is already a team member
            {
                throw new InvalidOperationException("User is already a team member in this project.");
            }

            o_Project.TeamMembers.Add(o_Gainer); // Add the user to the project team
            r_DbContext.SaveChanges();
        }

        // Remove a team member from a project
        public void RemoveTeamMember(Guid i_ProjectId, Guid i_UserId)
        {
            var o_Project = r_DbContext.Projects
                .Include(p => p.TeamMembers)
                .FirstOrDefault(p => p.ProjectId == i_ProjectId); // Get the project by ID including team members
            if (o_Project == null)
            {
                throw new KeyNotFoundException("Project not found.");
            }
            var o_Gainer = r_DbContext.Gainers.Find(i_UserId); // Get the user by ID
            if (o_Gainer == null)
            {
                throw new KeyNotFoundException("User not found or is not a Gainer.");
            }
            if (!o_Project.TeamMembers.Any(u => u.UserId == i_UserId)) // Check if the user is a team member
            {
                throw new InvalidOperationException("User is not a team member in this project.");
            }
            o_Project.TeamMembers.Remove(o_Gainer); // Remove the user from the project team
            r_DbContext.SaveChanges();
        }

        // Search for projects by name or description
        public IEnumerable<ProjectViewModel> SearchProjectsByNameOrDescription(string i_SearchQuery)
        {
            return r_DbContext.Projects
                .Where(p => p.ProjectName.Contains(i_SearchQuery) || p.ProjectDescription.Contains(i_SearchQuery))
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .ToList()
                .Select(p => new ProjectViewModel(p));
        }

        // Filter projects by status and difficulty level
        public IEnumerable<ProjectViewModel> FilterProjectsByStatusAndDifficulty(eProjectStatus i_Status,
            eDifficultyLevel i_Difficulty)
        {
            return r_DbContext.Projects
                .Where(p => p.ProjectStatus == i_Status && p.DifficultyLevel == i_Difficulty)
                .Include(p => p.TeamMembers)
                .Include(p => p.AssignedMentor)
                .Include(p => p.OwningOrganization)
                .ToList()
                .Select(p => new ProjectViewModel(p));
        }
    }
}
