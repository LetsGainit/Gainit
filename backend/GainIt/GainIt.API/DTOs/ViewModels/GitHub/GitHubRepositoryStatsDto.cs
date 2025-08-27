namespace GainIt.API.DTOs.ViewModels.GitHub
{
    public class GitHubRepositoryStatsDto
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public int? StarsCount { get; set; }
        public int? ForksCount { get; set; }
        public string? PrimaryLanguage { get; set; }
        public List<string>? Languages { get; set; }
        public DateTime? LastActivityAtUtc { get; set; }
        public DateTime? LastSyncedAtUtc { get; set; }

        public int IssueCount { get; set; }
        public int PullRequestCount { get; set; }
        public int BranchCount { get; set; }
        public List<string> Branches { get; set; } = new();
        public int ReleaseCount { get; set; }

        public int Contributors { get; set; }
        public List<TopContributorDto> TopContributors { get; set; } = new();
    }

    public class TopContributorDto
    {
        public string GitHubUsername { get; set; } = string.Empty;
        public int TotalCommits { get; set; }
        public int TotalLinesChanged { get; set; }
        public int UniqueDaysWithCommits { get; set; }
    }
}


