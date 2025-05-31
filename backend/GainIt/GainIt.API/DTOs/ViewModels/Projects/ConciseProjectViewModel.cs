using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class ConciseProjectViewModel
    {
        public string ProjectId { get; set; }
        public string PictureUrl { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public List<string> Technologies { get; set; }
        public string ProjectStatus { get; set; }
        public string UserRoles { get; set; }
        public List<string> TeamMembersPictureUrls { get; set; }
        public string ProjectPictureUrl { get; set; }

        public ConciseProjectViewModel(TemplateProject i_Project, Guid? i_TeamMemberId)
        {
            ProjectId = i_Project.ProjectId.ToString();
            ProjectName = i_Project.ProjectName;
            ProjectDescription = i_Project.ProjectDescription;
            ProjectPictureUrl = i_Project.ProjectPictureUrl;
            PictureUrl = i_Project.ProjectPictureUrl;
            Technologies = i_Project.Technologies ?? new List<string>();
            ProjectStatus = string.Empty;
            UserRoles = string.Empty;
            TeamMembersPictureUrls = new List<string>();

            if (i_Project is UserProject userProject)
            {
                ProjectStatus = userProject.ProjectStatus.ToString();

                if (i_TeamMemberId.HasValue && userProject.RoleToIdMap.Count > 0)
                {
                    string role = userProject.RoleToIdMap
                    .Where(roleToIdEntry => roleToIdEntry.Value == i_TeamMemberId.Value)
                    .Select(roleToIdEntry => roleToIdEntry.Key)
                    .FirstOrDefault() ??
                     string.Empty;

                    UserRoles = role;
                }
                else
                {
                    UserRoles = string.Empty;
                }

                TeamMembersPictureUrls = userProject.TeamMembers?
                    .Select(member => member.ProfilePictureURL ?? string.Empty)
                    .Where(url => !string.IsNullOrEmpty(url))
                    .ToList() ?? new List<string>();
            }
        }
    }
}
