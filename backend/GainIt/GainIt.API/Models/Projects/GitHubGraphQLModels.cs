using System.Text.Json.Serialization;

namespace GainIt.API.Models.Projects
{
    #region Repository Data Models

    public class GitHubRepositoryData
    {
        [JsonPropertyName("repository")]
        public GitHubRepositoryNode? Repository { get; set; }
    }

    public class GitHubRepositoryNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("nameWithOwner")]
        public string NameWithOwner { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("isPrivate")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; }

        [JsonPropertyName("isFork")]
        public bool IsFork { get; set; }

        [JsonPropertyName("defaultBranchRef")]
        public GitHubRefNode? DefaultBranchRef { get; set; }

        [JsonPropertyName("primaryLanguage")]
        public GitHubLanguageNode? PrimaryLanguage { get; set; }

        [JsonPropertyName("languages")]
        public GitHubLanguageConnection? Languages { get; set; }

        [JsonPropertyName("licenseInfo")]
        public GitHubLicenseNode? LicenseInfo { get; set; }

        [JsonPropertyName("stargazerCount")]
        public int StargazerCount { get; set; }

        [JsonPropertyName("forkCount")]
        public int ForkCount { get; set; }

        [JsonPropertyName("watchers")]
        public GitHubUserConnection? Watchers { get; set; }

        [JsonPropertyName("issues")]
        public GitHubIssueConnection? Issues { get; set; }

        [JsonPropertyName("pullRequests")]
        public GitHubPullRequestConnection? PullRequests { get; set; }

        [JsonPropertyName("refs")]
        public GitHubRefConnection? Refs { get; set; }

        [JsonPropertyName("releases")]
        public GitHubReleaseConnection? Releases { get; set; }

        [JsonPropertyName("object")]
        public GitHubCommitNode? Object { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("pushedAt")]
        public DateTime? PushedAt { get; set; }
    }

    #endregion

    #region Connection Models

    public class GitHubLanguageConnection
    {
        [JsonPropertyName("nodes")]
        public List<GitHubLanguageNode>? Nodes { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class GitHubIssueConnection
    {
        [JsonPropertyName("nodes")]
        public List<GitHubIssueNode>? Nodes { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class GitHubPullRequestConnection
    {
        [JsonPropertyName("nodes")]
        public List<GitHubPullRequestNode>? Nodes { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class GitHubRefConnection
    {
        [JsonPropertyName("nodes")]
        public List<GitHubRefNode>? Nodes { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class GitHubReleaseConnection
    {
        [JsonPropertyName("nodes")]
        public List<GitHubReleaseNode>? Nodes { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class GitHubUserConnection
    {
        [JsonPropertyName("nodes")]
        public List<GitHubUserNode>? Nodes { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class GitHubCommitConnection
    {
        [JsonPropertyName("nodes")]
        public List<GitHubCommitNode>? Nodes { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("pageInfo")]
        public GitHubPageInfo? PageInfo { get; set; }
    }

    #endregion

    #region Node Models

    public class GitHubLanguageNode
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string? Color { get; set; }
    }

    public class GitHubRefNode
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class GitHubLicenseNode
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class GitHubIssueNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("closedAt")]
        public DateTime? ClosedAt { get; set; }

        [JsonPropertyName("author")]
        public GitHubUserNode? Author { get; set; }

        [JsonPropertyName("assignees")]
        public GitHubUserConnection? Assignees { get; set; }
    }

    public class GitHubPullRequestNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("closedAt")]
        public DateTime? ClosedAt { get; set; }

        [JsonPropertyName("mergedAt")]
        public DateTime? MergedAt { get; set; }

        [JsonPropertyName("author")]
        public GitHubUserNode? Author { get; set; }

        [JsonPropertyName("reviews")]
        public GitHubReviewConnection? Reviews { get; set; }
    }

    public class GitHubReleaseNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("tagName")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class GitHubUserNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("avatarUrl")]
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class GitHubCommitNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("committedDate")]
        public DateTime CommittedDate { get; set; }

        [JsonPropertyName("author")]
        public GitHubCommitAuthor? Author { get; set; }

        [JsonPropertyName("additions")]
        public int Additions { get; set; }

        [JsonPropertyName("deletions")]
        public int Deletions { get; set; }

        [JsonPropertyName("changedFiles")]
        public int ChangedFiles { get; set; }
    }

    public class GitHubCommitAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public GitHubUserNode? User { get; set; }
    }

    public class GitHubReviewConnection
    {
        [JsonPropertyName("nodes")]
        public List<GitHubReviewNode>? Nodes { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class GitHubReviewNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("author")]
        public GitHubUserNode? Author { get; set; }
    }

    public class GitHubPageInfo
    {
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }

        [JsonPropertyName("endCursor")]
        public string? EndCursor { get; set; }
    }

    #endregion

    #region Analytics Models

    public class GitHubAnalyticsData
    {
        [JsonPropertyName("repository")]
        public GitHubAnalyticsRepository? Repository { get; set; }
    }

    public class GitHubAnalyticsRepository
    {
        [JsonPropertyName("defaultBranchRef")]
        public GitHubAnalyticsRef? DefaultBranchRef { get; set; }

        [JsonPropertyName("object")]
        public GitHubAnalyticsCommit? Object { get; set; }
    }

    public class GitHubAnalyticsRef
    {
        [JsonPropertyName("target")]
        public GitHubAnalyticsCommit? Target { get; set; }
    }

    public class GitHubAnalyticsCommit
    {
        [JsonPropertyName("history")]
        public GitHubCommitHistory? History { get; set; }
    }

    public class GitHubCommitHistory
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("nodes")]
        public List<GitHubAnalyticsCommitNode>? Nodes { get; set; }
    }

    public class GitHubAnalyticsCommitNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("committedDate")]
        public DateTime CommittedDate { get; set; }

        [JsonPropertyName("author")]
        public GitHubCommitAuthor? Author { get; set; }

        [JsonPropertyName("additions")]
        public int Additions { get; set; }

        [JsonPropertyName("deletions")]
        public int Deletions { get; set; }

        [JsonPropertyName("changedFiles")]
        public int ChangedFiles { get; set; }
    }

    #endregion
}
