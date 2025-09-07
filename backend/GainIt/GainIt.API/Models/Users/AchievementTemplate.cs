using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GainIt.API.Models.Users;

namespace GainIt.API.Models.Users
{
    public class AchievementTemplate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(500)]
        public string IconUrl { get; set; }

        // Criteria for unlocking the achievement
        [Required]
        [StringLength(1000)]
        public string UnlockCriteria { get; set; }

        // Category of achievement (e.g., "Project Completion", "Community", "Skills")
        [Required]
        public string Category { get; set; }
    }
}

 


