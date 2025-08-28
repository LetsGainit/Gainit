using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GainIt.API.Models.Users;

namespace GainIt.API.Models.Projects
{
    public class GitHubContribution
    {
        [Key]
        public Guid ContributionId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RepositoryId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string GitHubUsername { get; set; } = string.Empty;

        [Required]
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;

        // Time period for these analytics
        [Required]
        public int DaysPeriod { get; set; } = 30;

        // Commit Metrics
        public int TotalCommits { get; set; }
        public int TotalAdditions { get; set; }
        public int TotalDeletions { get; set; }
        public int TotalLinesChanged { get; set; }
        public int UniqueDaysWithCommits { get; set; }
        public DateTime? FirstCommitDate { get; set; }
        public DateTime? LastCommitDate { get; set; }

        // Issue Metrics
        public int TotalIssuesCreated { get; set; }
        public int OpenIssuesCreated { get; set; }
        public int ClosedIssuesCreated { get; set; }
        public int IssuesCommentedOn { get; set; }
        public int IssuesAssigned { get; set; }
        public int IssuesClosed { get; set; }

        // Pull Request Metrics
        public int TotalPullRequestsCreated { get; set; }
        public int OpenPullRequestsCreated { get; set; }
        public int MergedPullRequestsCreated { get; set; }
        public int ClosedPullRequestsCreated { get; set; }
        public int PullRequestsReviewed { get; set; }
        public int PullRequestsApproved { get; set; }
        public int PullRequestsRequestedChanges { get; set; }

        // Code Review Metrics
        public int TotalReviews { get; set; }
        public int ReviewsApproved { get; set; }
        public int ReviewsRequestedChanges { get; set; }
        public int ReviewsCommented { get; set; }
        public double? AverageReviewTime { get; set; } // in hours

        // Activity Patterns
        public Dictionary<string, int> CommitsByDayOfWeek { get; set; } = new();
        public Dictionary<string, int> CommitsByHour { get; set; } = new();
        public Dictionary<string, int> ActivityByMonth { get; set; } = new();

        // Contribution Quality Metrics
        public int FilesModified { get; set; }
        public List<string> LanguagesContributed { get; set; } = new();
        public double? AverageCommitSize { get; set; } // lines changed per commit
        public int LongestStreak { get; set; } // consecutive days with commits
        public int CurrentStreak { get; set; } // current consecutive days

        // Collaboration Metrics
        public int CollaboratorsInteractedWith { get; set; }
        public int DiscussionsParticipated { get; set; }
        public int WikiPagesEdited { get; set; }

        // Transient, non-persisted context about latest work items
        [NotMapped]
        public string? LatestPullRequestTitle { get; set; }
        [NotMapped]
        public int? LatestPullRequestNumber { get; set; }
        [NotMapped]
        public DateTime? LatestPullRequestCreatedAt { get; set; }
        [NotMapped]
        public string? LatestCommitMessage { get; set; }
        [NotMapped]
        public DateTime? LatestCommitDate { get; set; }

        // Navigation properties
        [ForeignKey("RepositoryId")]
        public virtual GitHubRepository Repository { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
