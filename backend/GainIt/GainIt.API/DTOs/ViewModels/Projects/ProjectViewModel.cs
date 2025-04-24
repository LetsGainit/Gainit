using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class ProjectViewModel
    {
        public ProjectViewModel(Project i_Project)
        {
            projectId = i_Project.ProjectId.ToString();
            projectName = i_Project.ProjectName;
            projectDescription = i_Project.ProjectDescription;
            projectStatus = i_Project.ProjectStatus.ToString();
            difficultyLevel = i_Project.DifficultyLevel?.ToString();
            projectSource = i_Project.ProjectSource.ToString();
            createdAtUtc = i_Project.CreatedAtUtc;
            repositoryLink = i_Project.RepositoryLink;
            assignedMentorName = i_Project.AssignedMentor?.FullName;
            owningOrganizationName = i_Project.OwningOrganization?.FullName;

            // Populate teamMemberFullNames from TeamMembers
            teamMemberFullNames = i_Project.TeamMembers
                .Select(member => member.FullName)
                .ToList();
        }

        public string projectId { get; set; }
        public string projectName { get; set; }
        public string projectDescription { get; set; }
        public string projectStatus { get; set; }
        public string difficultyLevel { get; set; }
        public string projectSource { get; set; }
        public DateTime createdAtUtc { get; set; }
        public List<string> teamMemberFullNames { get; set; }
        public string? repositoryLink { get; set; }
        public string? assignedMentorName { get; set; }
        public string? owningOrganizationName { get; set; }
    }
}
