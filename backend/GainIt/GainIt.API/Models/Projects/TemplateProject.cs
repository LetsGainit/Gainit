using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.Models.Projects
{
    public class TemplateProject
    {
        // in project Services StartProjectFromTemplateAsync uses template project to create a new project.
        // This is how templates saved in the system and from the templates the user create a project.

        [Key]
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Project Name is required")]
        [StringLength(200, ErrorMessage = "Project Name cannot exceed 200 characters")]
        public required string ProjectName { get; set; }

        [Required(ErrorMessage = "Project Description is required")]
        [StringLength(1000, ErrorMessage = "Project Description cannot exceed 1000 characters")]
        public required string ProjectDescription { get; set; }

        [Required]
        public eDifficultyLevel DifficultyLevel { get; set; }

        //new 
        [Url(ErrorMessage = "Invalid Project Picture URL")]
        [StringLength(500, ErrorMessage = "Project Picture URL cannot exceed 500 characters")]
        public required string ProjectPictureUrl { get; set; }

        public TimeSpan Duration { get; set; }

        [Required(ErrorMessage = "Project Goals are required")]
        [StringLength(2000, ErrorMessage = "Project Goals cannot exceed 2000 characters")]
        public required List<string> Goals { get; set; }

        [Required(ErrorMessage = "Technologies are required")]
        public List<string> Technologies { get; set; } = new();

        [Required(ErrorMessage = "Open Roles are required")]
        public List<string> RequiredRoles { get; set; } = new();
    }
}