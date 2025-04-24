using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.Models.Projects
{
    public class TemplateProject
    {
        [Key]
        public Guid TemplateProjectId { get; set; }

        [Required(ErrorMessage = "Project Name is required")]
        [StringLength(200, ErrorMessage = "Project Name cannot exceed 200 characters")]
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Project Description is required")]
        [StringLength(1000, ErrorMessage = "Project Description cannot exceed 1000 characters")]
        public string ProjectDescription { get; set; }

        [Required]
        public eDifficultyLevel DifficultyLevel { get; set; }
    }
}
