using System.ComponentModel.DataAnnotations;
using GainIt.API.Models.Users.Expertise;

namespace GainIt.API.DTOs.Requests.Users
{
    public class NonprofitProfileUpdateDTO
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Biography is required")]
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
        [StringLength(200, ErrorMessage = "Profile picture URL cannot exceed 200 characters")]
        public string? ProfilePictureURL { get; set; }

        // Nonprofit-specific fields
        [Required(ErrorMessage = "Website URL is required")]
        [Url(ErrorMessage = "Invalid Website URL")]
        public string WebsiteUrl { get; set; }

        public NonprofitExpertise NonprofitExpertise { get; set; }
    }
} 