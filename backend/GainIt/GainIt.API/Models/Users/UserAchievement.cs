using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Users
{
    public class UserAchievement
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Reference to the achievement template
        [Required]
        public Guid AchievementTemplateId { get; set; }
        public AchievementTemplate AchievementTemplate { get; set; } = null!;

        // The user who earned this achievement
        [Required]
        public Guid UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!; // tells EF to not ignore the User property

        // When the achievement was earned
        [Required]
        public DateTime EarnedAtUtc { get; set; } = DateTime.UtcNow;

        // Additional data about how it was earned (optional)
        [StringLength(1000)]
        public string? EarnedDetails { get; set; }

        // Optional custom icon URL for this earned achievement (can be null or empty)
        [StringLength(500)]
        public string? AchievementIconUrl { get; set; }
    }
}
