using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Users
{
    public class NonprofitProfileUpdateDto
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

        // Nonprofit-specific properties
        [Required(ErrorMessage = "Organization Type is required")]
        public string OrganizationType { get; set; }

        [Required(ErrorMessage = "Mission Statement is required")]
        [StringLength(1000, ErrorMessage = "Mission Statement cannot exceed 1000 characters")]
        public string MissionStatement { get; set; }

        [Required(ErrorMessage = "Website URL is required")]
        [Url(ErrorMessage = "Invalid Website URL")]
        [StringLength(200, ErrorMessage = "Website URL cannot exceed 200 characters")]
        public string WebsiteURL { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Focus Areas are required")]
        public List<string> FocusAreas { get; set; }

        [Required(ErrorMessage = "Year Founded is required")]
        [Range(1800, 2100, ErrorMessage = "Year Founded must be between 1800 and 2100")]
        public int YearFounded { get; set; }

        [Required(ErrorMessage = "Team Size is required")]
        [Range(1, 10000, ErrorMessage = "Team Size must be between 1 and 10000")]
        public int TeamSize { get; set; }
    }
} 