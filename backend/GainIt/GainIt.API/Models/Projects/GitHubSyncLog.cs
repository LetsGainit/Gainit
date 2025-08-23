using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.Projects
{
    public class GitHubSyncLog
    {
        [Key]
        public Guid SyncLogId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RepositoryId { get; set; }

        [Required]
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAtUtc { get; set; }

        [Required]
        public string SyncType { get; set; } = string.Empty; // "Repository", "Analytics", "Contributions", "Full"

        [Required]
        public string Status { get; set; } = "InProgress"; // "InProgress", "Completed", "Failed", "Partial"

        public string? ErrorMessage { get; set; }

        public int? ItemsProcessed { get; set; }
        public int? TotalItems { get; set; }

        // API Rate Limiting Info
        public int? RemainingRequests { get; set; }
        public DateTime? RateLimitResetAt { get; set; }

        // Sync Details
        public string? SyncDetails { get; set; } // JSON string with detailed sync information

        // Navigation properties
        [ForeignKey("RepositoryId")]
        public virtual GitHubRepository Repository { get; set; } = null!;
    }
}
