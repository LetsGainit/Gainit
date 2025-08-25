using System.ComponentModel.DataAnnotations;
using GainIt.API.Models.Users.Expertise;

namespace GainIt.API.DTOs.Requests.Users
{
    public class MentorProfileUpdateDTO
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Biography is required")]
        [StringLength(1000, ErrorMessage = "Biography cannot exceed 1000 characters")]
        public string Biography { get; set; } = null!;

        [Url(ErrorMessage = "Invalid Facebook URL")]
        [StringLength(200, ErrorMessage = "Facebook URL cannot exceed 200 characters")]
        public string? FacebookPageURL { get; set; }

        [Url(ErrorMessage = "Invalid LinkedIn URL")]
        [StringLength(200, ErrorMessage = "LinkedIn URL cannot exceed 200 characters")]
        public string? LinkedInURL { get; set; }

        [Url(ErrorMessage = "Invalid GitHub URL")]
        [StringLength(200, ErrorMessage = "GitHub URL cannot exceed 200 characters")]
        public string? GitHubURL { get; set; }

        [StringLength(100, ErrorMessage = "GitHub username cannot exceed 100 characters")]
        public string? GitHubUsername { get; set; }

        [Url(ErrorMessage = "Invalid Profile picture URL")]
        [StringLength(200, ErrorMessage = "Profile picture URL cannot exceed 200 characters")]
        public string? ProfilePictureURL { get; set; }

        // Mentor-specific fields start here
        [Range(1, 50, ErrorMessage = "Years of Experience must be between 1 and 50")]
        public int YearsOfExperience { get; set; }

        [Required(ErrorMessage = "Area of Expertise is required")]
        [StringLength(200, ErrorMessage = "Area of Expertise cannot exceed 200 characters")]
        public string AreaOfExpertise { get; set; } = null!;

        public TechExpertise TechExpertise { get; set; } = null!;
    }
} 