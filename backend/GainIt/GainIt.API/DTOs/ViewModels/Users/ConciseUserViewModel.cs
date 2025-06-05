using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Users
{
    public class ConciseUserViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string UserType { get; set; }
        public string RoleInProject { get; set; }

        public ConciseUserViewModel(ProjectMember i_ProjectMember)
        {
            UserId = i_ProjectMember.User.UserId.ToString();
            FullName = i_ProjectMember.User.FullName;
            ProfilePictureUrl = i_ProjectMember.User.ProfilePictureURL;
            UserType = i_ProjectMember.User.UserRole.ToString();
            RoleInProject = i_ProjectMember.UserRole;
        }
    }
}
