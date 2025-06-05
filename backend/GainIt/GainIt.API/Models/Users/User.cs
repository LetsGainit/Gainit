using System.ComponentModel.DataAnnotations;
using GainIt.API.Models.Enums.Users;
using GainIt.API.Models.Projects;

namespace GainIt.API.Models.Users
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = new Guid();  


        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(200, ErrorMessage = "Email Address cannot exceed 200 characters")]
        public string EmailAddress { get; set; }

        [Required]
        public eUserType UserRole { get; protected set; } // Will include: "NonprofitOrganization", "Mentor", or "Gainer"

        [Required]
        [StringLength(1000, ErrorMessage = "Biography cannot exceed 1000 characters")]
        public string Biography { get; set; }

        [Url(ErrorMessage = "Invalid Facebook URL")]
        [StringLength(200, ErrorMessage = "Facebook URL cannot exceed 200 characters")]
        public string? FacebookPageURL { get; set; }

        [Url(ErrorMessage = "Invalid LinkedIn URL")]
        [StringLength(200, ErrorMessage = "LinkedIn URL cannot exceed 200 characters")]
        public string? LinkedInURL { get; set; }

        [Url(ErrorMessage = "Invalid GitHub URL")]
        [StringLength(200, ErrorMessage = "GitHub URL cannot exceed 200 characters")]
        public string? GitHubURL { get; set; }

        [Url(ErrorMessage = "Invalid Profile picture URL")]
        [StringLength(200, ErrorMessage = "Profile picture URL cannot exceed 200 characters")]   // gives the option to bring URL or upload
                                                                                                 // a picture and get a url from the system
        public string? ProfilePictureURL { get; set; }


        public List<UserAchievement> Achievements { get; set; } = new();
    

    /// mentors review - > do be decided ( if any or just mentors) 
}
}
