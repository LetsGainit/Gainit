using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class UserProjectViewModel
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string ProjectStatus { get; set; }
        public string DifficultyLevel { get; set; }
        public string ProjectSource { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<string> TeamMemberFullNames { get; set; } //pictures and roles
        public string? RepositoryLink { get; set; }
        public string? AssignedMentorName { get; set; }
        public string? OwningOrganizationName { get; set; }
        public string? ProjectPictureUrl { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool IsPublic { get; set; }
        public List<string> OpenRoles { get; set; }
        public List<string> ProgrammingLanguages { get; set; }
        public string Goals { get; set; }
        public List<string> Technologies { get; set; }


        public UserProjectViewModel(UserProject i_Project)
        {
            ProjectId = i_Project.ProjectId.ToString();
            ProjectName = i_Project.ProjectName;
            ProjectDescription = i_Project.ProjectDescription;
            ProjectStatus = i_Project.ProjectStatus.ToString();
            DifficultyLevel = i_Project.DifficultyLevel.ToString();
            ProjectSource = i_Project.ProjectSource.ToString();
            CreatedAtUtc = i_Project.CreatedAtUtc;
            RepositoryLink = i_Project.RepositoryLink;
            AssignedMentorName = i_Project.AssignedMentor?.FullName;
            OwningOrganizationName = i_Project.OwningOrganization?.FullName;

            // Populate teamMemberFullNames from TeamMembers
            TeamMemberFullNames = i_Project.TeamMembers
                .Select(member => member.FullName)
                .ToList();

            // Map new properties
            ProjectPictureUrl = i_Project.ProjectPictureUrl;
            Duration = i_Project.Duration;
            IsPublic = i_Project.IsPublic;
            OpenRoles = i_Project.OpenRoles;
            ProgrammingLanguages = i_Project.ProgrammingLanguages;
            Goals = i_Project.Goals;
            Technologies = i_Project.Technologies;
        }
    }
}
