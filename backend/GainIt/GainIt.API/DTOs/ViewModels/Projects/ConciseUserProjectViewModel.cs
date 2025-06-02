using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class ConciseUserProjectViewModel
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public List<string> Technologies { get; set; } = new List<string>();
        public string ProjectStatus { get; set; }
        public string MyRoleInProject { get; set; } 
        public List<string> TeamMembersPictureUrls { get; set; } = new List<string>();
        public string ProjectPictureUrl { get; set; }

        public ConciseUserProjectViewModel(UserProject i_Project, Guid? i_TeamMemberId)
        {
            ProjectId = i_Project.ProjectId.ToString();
            ProjectName = i_Project.ProjectName;
            ProjectDescription = i_Project.ProjectDescription;
            ProjectPictureUrl = i_Project.ProjectPictureUrl;
            Technologies = i_Project.Technologies;
            ProjectStatus = i_Project.ProjectStatus.ToString();

            if (i_TeamMemberId.HasValue && i_Project.RoleToIdMap.Count > 0)
            {
                string role = i_Project.RoleToIdMap
                .Where(roleToIdEntry => roleToIdEntry.Value == i_TeamMemberId.Value)
                .Select(roleToIdEntry => roleToIdEntry.Key)
                .FirstOrDefault() ??
                 string.Empty;

                MyRoleInProject = role;
            }
            else
            {
                MyRoleInProject = string.Empty;
            }

            TeamMembersPictureUrls = i_Project.TeamMembers?
                   .Select(member => member.ProfilePictureURL ?? string.Empty)
                   .Where(url => !string.IsNullOrEmpty(url))
                   .ToList() ?? new List<string>();
        }
    }
}
