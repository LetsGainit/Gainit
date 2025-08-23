using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.Projects
{
    public class GitHubAnalytics
    {
        [Key]
        public Guid AnalyticsId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RepositoryId { get; set; }

        [Required]
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;

        // Time period for these analytics (e.g., last 30 days, last 90 days, last year)
        [Required]
        public int DaysPeriod { get; set; } = 30;

        // Repository Health Metrics
        public int TotalCommits { get; set; }
        public int TotalAdditions { get; set; }
        public int TotalDeletions { get; set; }
        public int TotalLinesChanged { get; set; }

        // Issue and PR Metrics
        public int TotalIssues { get; set; }
        public int OpenIssues { get; set; }
        public int ClosedIssues { get; set; }
        public int TotalPullRequests { get; set; }
        public int OpenPullRequests { get; set; }
        public int MergedPullRequests { get; set; }
        public int ClosedPullRequests { get; set; }

        // Code Quality Metrics
        public int TotalBranches { get; set; }
        public int TotalReleases { get; set; }
        public int TotalTags { get; set; }
        public double? AverageTimeToCloseIssues { get; set; } // in days
        public double? AverageTimeToMergePRs { get; set; } // in days

        // Activity Metrics
        public int ActiveContributors { get; set; }
        public int TotalContributors { get; set; }
        public DateTime? FirstCommitDate { get; set; }
        public DateTime? LastCommitDate { get; set; }

        // Engagement Metrics
        public int TotalStars { get; set; }
        public int TotalForks { get; set; }
        public int TotalWatchers { get; set; }

        // Language Distribution (stored as JSON)
        public Dictionary<string, int> LanguageStats { get; set; } = new();

        // Weekly Activity (last 52 weeks)
        public Dictionary<string, int> WeeklyCommits { get; set; } = new();
        public Dictionary<string, int> WeeklyIssues { get; set; } = new();
        public Dictionary<string, int> WeeklyPullRequests { get; set; } = new();

        // Monthly Activity (last 12 months)
        public Dictionary<string, int> MonthlyCommits { get; set; } = new();
        public Dictionary<string, int> MonthlyIssues { get; set; } = new();
        public Dictionary<string, int> MonthlyPullRequests { get; set; } = new();

        // Navigation properties
        [ForeignKey("RepositoryId")]
        public virtual GitHubRepository Repository { get; set; } = null!;
    }
}
