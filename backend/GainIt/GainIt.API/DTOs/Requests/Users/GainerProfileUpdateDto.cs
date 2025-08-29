using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Users
{
    public class GainerProfileUpdateDTO
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

        // Gainer-specific properties
        [Required(ErrorMessage = "Current Role is required")]
        [StringLength(100, ErrorMessage = "Current Role cannot exceed 100 characters")]
        public string CurrentRole { get; set; } = null!;

        [Required(ErrorMessage = "Years of Experience is required")]
        [Range(0, 50, ErrorMessage = "Years of Experience must be between 0 and 50")]
        public int YearsOfExperience { get; set; }

        [Required(ErrorMessage = "Education Level is required")]
        public string EducationStatus { get; set; } = null!;

        [Required(ErrorMessage = "Areas of Interest are required")]
        [MinLength(1, ErrorMessage = "At least one area of interest is required")]
        public List<string> AreasOfInterest { get; set; } = null!;

        // Optional: include expertise strings to add in the same call
        public List<string>? ProgrammingLanguages { get; set; }
        public List<string>? Technologies { get; set; }
        public List<string>? Tools { get; set; }
    }
} 