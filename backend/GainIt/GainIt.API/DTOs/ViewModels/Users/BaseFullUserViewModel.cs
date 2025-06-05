using GainIt.API.Models.Users;

namespace GainIt.API.DTOs.ViewModels.Users
{
    public class BaseFullUserViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Biography { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? FacebookPageURL { get; set; }
        public string? LinkedInURL { get; set; }
        public string? GitHubURL { get; set; }
        public string UserType { get; set; }

        public BaseFullUserViewModel(User user)
        {
            UserId = user.UserId.ToString();
            FullName = user.FullName;
            Biography = user.Biography;
            ProfilePictureUrl = user.ProfilePictureURL;
            FacebookPageURL = user.FacebookPageURL;
            LinkedInURL = user.LinkedInURL;
            GitHubURL = user.GitHubURL;
            UserType = user.UserRole.ToString();
        }
    }
}
