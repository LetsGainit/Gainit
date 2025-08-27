using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.Projects
{
    public class GitHubRepository
    {
        [Key]
        public Guid RepositoryId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        [StringLength(200)]
        public string RepositoryName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FullName { get; set; } = string.Empty; // owner/repo-name

        [Required]
        [StringLength(2000)]
        public string RepositoryUrl { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public bool IsPublic { get; set; } = true;

        [Required]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastSyncedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;

        public int? StarsCount { get; set; }
        public int? ForksCount { get; set; }
        public int? OpenIssuesCount { get; set; }
        public int? OpenPullRequestsCount { get; set; }

        [StringLength(50)]
        public string? DefaultBranch { get; set; } = "main";

        [StringLength(50)]
        public string? PrimaryLanguage { get; set; }

        public List<string> Languages { get; set; } = new();

        [StringLength(50)]
        public string? License { get; set; }

        public bool IsArchived { get; set; } = false;
        public bool IsFork { get; set; } = false;

        // Store all repository branches
        public List<string> Branches { get; set; } = new();

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual UserProject Project { get; set; } = null!;

        public virtual GitHubAnalytics? Analytics { get; set; }
        public virtual List<GitHubContribution> Contributions { get; set; } = new();
        public virtual List<GitHubSyncLog> SyncLogs { get; set; } = new();
    }
}
