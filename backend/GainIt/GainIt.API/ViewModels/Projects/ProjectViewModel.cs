using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.ViewModels.Projects
{
    public class ProjectViewModel
    {
        public ProjectViewModel(Project i_Project)
        {
            projectName = i_Project.ProjectName;
            projectDescription = i_Project.ProjectDescription;
            projectStatus = i_Project.ProjectStatus.ToString();
            difficultyLevel = i_Project.DifficultyLevel.ToString();
            projectSource = i_Project.ProjectSource.ToString();
            createdAtUtc = i_Project.CreatedAtUtc;
            repositoryLink = i_Project.RepositoryLink;
            assignedMentorName = i_Project.AssignedMentor?.FullName;
            owningOrganizationName = i_Project.OwningOrganization.FullName;

            teamMemberFullNames = new List<string>();

            foreach (User user in i_Project.TeamMembers)
            {
                teamMemberFullNames.Add(user.FullName);
            }
        }

        public string projectName { get; set; }

        public string projectDescription { get; set; }

        public string projectStatus { get; set; } // "Pending" , "In Progress", "Completed"

        public string difficultyLevel { get; set; }

        public string projectSource { get; set; } // If the project is from a NonprofitOrganization or a built-in project

        public DateTime createdAtUtc { get; set; } = DateTime.UtcNow;

        public List<string> teamMemberFullNames { get; set; } = new(); // names of users (Gainers)

        public string? repositoryLink { get; set; } 
        
        public string? assignedMentorName { get; set; }
        
        public string? owningOrganizationName { get; set; }
        

    }
}
