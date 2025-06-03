using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Users
{
    public class MentorProfileUpdateDto
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

        // Mentor-specific properties
        [Required(ErrorMessage = "Professional Title is required")]
        [StringLength(100, ErrorMessage = "Professional Title cannot exceed 100 characters")]
        public string ProfessionalTitle { get; set; }

        [Required(ErrorMessage = "Years of Mentoring Experience is required")]
        [Range(0, 50, ErrorMessage = "Years of Mentoring Experience must be between 0 and 50")]
        public int YearsOfMentoringExperience { get; set; }

        [Required(ErrorMessage = "Mentoring Style is required")]
        [StringLength(500, ErrorMessage = "Mentoring Style cannot exceed 500 characters")]
        public string MentoringStyle { get; set; }

        [Required(ErrorMessage = "Areas of Mentorship is required")]
        public List<string> AreasOfMentorship { get; set; }

        [Required(ErrorMessage = "Availability is required")]
        public string Availability { get; set; }

        [Required(ErrorMessage = "Preferred Communication Method is required")]
        public string PreferredCommunicationMethod { get; set; }
    }
} 