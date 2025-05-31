using System.ComponentModel.DataAnnotations;

namespace GainIt.API.Models.Users
{
    public class UserAchievement
    {
        [Key]
        public Guid Id { get; set; }

        // Reference to the achievement template
        [Required]
        public Guid AchievementTemplateId { get; set; }
        public AchievementTemplate AchievementTemplate { get; set; }

        // The user who earned this achievement
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; }

        // When the achievement was earned
        [Required]
        public DateTime EarnedAtUtc { get; set; } = DateTime.UtcNow;

        // Additional data about how it was earned (optional)
        [StringLength(1000)]
        public string? EarnedDetails { get; set; }
    }
}
