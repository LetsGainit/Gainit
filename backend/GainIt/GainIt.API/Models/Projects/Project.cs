using GainIt.API.Models.Enums.Projects;
using System.ComponentModel.DataAnnotations;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;

namespace GainIt.API.Models.Projects
{
    public class Project
    {
        [Key]
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Project Name is required")]
        [StringLength(200, ErrorMessage = "Project Name cannot exceed 200 characters")]
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Project Description is required")]
        [StringLength(1000, ErrorMessage = "Project Description cannot exceed 1000 characters")]
        public string ProjectDescription { get; set; }

        [Required]
        public eProjectStatus ProjectStatus { get; set; } // "Pending" , "In Progress", "Completed"

        [Required]
        public eDifficultyLevel? DifficultyLevel { get; set; }

        [Required]
        public eProjectSource ProjectSource { get; set; } // If the project is from a NonprofitOrganization or a built-in project

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public List<Gainer> TeamMembers { get; set; } = new(); // IDs of users (Gainers)

        [Url(ErrorMessage = "Invalid Repository URL")]
        public string? RepositoryLink { get; set; }

        public Mentor? AssignedMentor { get; set; }

        public NonprofitOrganization? OwningOrganization{ get; set; }
    }
}   