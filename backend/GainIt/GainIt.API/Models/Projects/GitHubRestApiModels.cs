using System.Text.Json.Serialization;

namespace GainIt.API.Models.Projects
{
    #region Repository Data Models for REST API

    public class GitHubRestApiRepository
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("clone_url")]
        public string CloneUrl { get; set; } = string.Empty;

        [JsonPropertyName("git_url")]
        public string GitUrl { get; set; } = string.Empty;

        [JsonPropertyName("ssh_url")]
        public string SshUrl { get; set; } = string.Empty;

        [JsonPropertyName("private")]
        public bool Private { get; set; }

        [JsonPropertyName("archived")]
        public bool Archived { get; set; }

        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; }

        [JsonPropertyName("fork")]
        public bool Fork { get; set; }

        [JsonPropertyName("default_branch")]
        public string DefaultBranch { get; set; } = string.Empty;

        [JsonPropertyName("license")]
        public GitHubRestApiLicense? License { get; set; }

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        [JsonPropertyName("forks_count")]
        public int ForksCount { get; set; }

        [JsonPropertyName("open_issues_count")]
        public int OpenIssuesCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("pushed_at")]
        public DateTime? PushedAt { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("owner")]
        public GitHubRestApiUser Owner { get; set; } = new();
    }

    public class GitHubRestApiLicense
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("spdx_id")]
        public string? SpdxId { get; set; }
    }

    public class GitHubRestApiUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }

    #endregion

    #region Language Models for REST API

    public class GitHubRestApiLanguage
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string? Color { get; set; }
    }

    #endregion

    #region Contributor Models for REST API

    public class GitHubRestApiContributor
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("contributions")]
        public int Contributions { get; set; }
    }

    #endregion

    #region Branch Models for REST API

    public class GitHubRestApiBranch
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("commit")]
        public GitHubRestApiBranchCommit Commit { get; set; } = new();

        [JsonPropertyName("protected")]
        public bool Protected { get; set; }
    }

    public class GitHubRestApiBranchCommit
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    #endregion

    #region Commit Models for REST API

    public class GitHubCommitNode
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CommittedDate { get; set; }
        public GitHubCommitAuthor? Author { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int ChangedFiles { get; set; }
    }

    public class GitHubRestApiCommit
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; } = string.Empty;

        [JsonPropertyName("commit")]
        public GitHubRestApiCommitDetails Commit { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("comments_url")]
        public string CommentsUrl { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public GitHubRestApiCommitUser? Author { get; set; }

        [JsonPropertyName("committer")]
        public GitHubRestApiCommitUser? Committer { get; set; }

        [JsonPropertyName("parents")]
        public List<GitHubRestApiCommitParent> Parents { get; set; } = new();

        [JsonPropertyName("stats")]
        public GitHubRestApiCommitStats? Stats { get; set; }

        [JsonPropertyName("files")]
        public List<GitHubRestApiCommitFile>? Files { get; set; }
    }

    public class GitHubRestApiCommitDetails
    {
        [JsonPropertyName("author")]
        public GitHubRestApiCommitAuthor Author { get; set; } = new();

        [JsonPropertyName("committer")]
        public GitHubRestApiCommitAuthor Committer { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("tree")]
        public GitHubRestApiCommitTree Tree { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("comment_count")]
        public int CommentCount { get; set; }

        [JsonPropertyName("verification")]
        public GitHubRestApiCommitVerification? Verification { get; set; }
    }

    public class GitHubRestApiCommitAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }

    public class GitHubRestApiCommitTree
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class GitHubRestApiCommitUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }

    public class GitHubRestApiCommitParent
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }

    public class GitHubRestApiCommitStats
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("additions")]
        public int Additions { get; set; }

        [JsonPropertyName("deletions")]
        public int Deletions { get; set; }
    }

    public class GitHubRestApiCommitFile
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("additions")]
        public int Additions { get; set; }

        [JsonPropertyName("deletions")]
        public int Deletions { get; set; }

        [JsonPropertyName("changes")]
        public int Changes { get; set; }

        [JsonPropertyName("blob_url")]
        public string BlobUrl { get; set; } = string.Empty;

        [JsonPropertyName("raw_url")]
        public string RawUrl { get; set; } = string.Empty;

        [JsonPropertyName("contents_url")]
        public string ContentsUrl { get; set; } = string.Empty;
    }

    public class GitHubRestApiCommitVerification
    {
        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("signature")]
        public string? Signature { get; set; }

        [JsonPropertyName("payload")]
        public string? Payload { get; set; }
    }

    #endregion

    #region Issue Models for REST API

    public class GitHubRestApiIssue
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("locked")]
        public bool Locked { get; set; }

        [JsonPropertyName("assignees")]
        public List<GitHubRestApiUser> Assignees { get; set; } = new();

        [JsonPropertyName("labels")]
        public List<GitHubRestApiLabel> Labels { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("closed_at")]
        public DateTime? ClosedAt { get; set; }

        [JsonPropertyName("author_association")]
        public string AuthorAssociation { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public GitHubRestApiUser? User { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }

    public class GitHubRestApiLabel
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty;

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    #endregion

    #region Pull Request Models for REST API

    public class GitHubRestApiPullRequest
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("locked")]
        public bool Locked { get; set; }

        [JsonPropertyName("user")]
        public GitHubRestApiUser? User { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("closed_at")]
        public DateTime? ClosedAt { get; set; }

        [JsonPropertyName("merged_at")]
        public DateTime? MergedAt { get; set; }

        [JsonPropertyName("merge_commit_sha")]
        public string? MergeCommitSha { get; set; }

        [JsonPropertyName("assignee")]
        public GitHubRestApiUser? Assignee { get; set; }

        [JsonPropertyName("assignees")]
        public List<GitHubRestApiUser> Assignees { get; set; } = new();

        [JsonPropertyName("requested_reviewers")]
        public List<GitHubRestApiUser> RequestedReviewers { get; set; } = new();

        [JsonPropertyName("labels")]
        public List<GitHubRestApiLabel> Labels { get; set; } = new();

        [JsonPropertyName("head")]
        public GitHubRestApiPullRequestRef Head { get; set; } = new();

        [JsonPropertyName("base")]
        public GitHubRestApiPullRequestRef Base { get; set; } = new();

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("merged")]
        public bool Merged { get; set; }

        [JsonPropertyName("mergeable")]
        public bool? Mergeable { get; set; }

        [JsonPropertyName("mergeable_state")]
        public string MergeableState { get; set; } = string.Empty;

        [JsonPropertyName("merged_by")]
        public GitHubRestApiUser? MergedBy { get; set; }

        [JsonPropertyName("comments")]
        public int Comments { get; set; }

        [JsonPropertyName("review_comments")]
        public int ReviewComments { get; set; }

        [JsonPropertyName("commits")]
        public int Commits { get; set; }

        [JsonPropertyName("additions")]
        public int Additions { get; set; }

        [JsonPropertyName("deletions")]
        public int Deletions { get; set; }

        [JsonPropertyName("changed_files")]
        public int ChangedFiles { get; set; }
    }

    public class GitHubRestApiPullRequestRef
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("ref")]
        public string Ref { get; set; } = string.Empty;

        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public GitHubRestApiUser? User { get; set; }

        [JsonPropertyName("repo")]
        public GitHubRestApiRepository? Repo { get; set; }
    }

    #endregion

    #region Analytics Models for REST API

    public class GitHubAnalyticsRepository
    {
        public GitHubAnalyticsRef? DefaultBranchRef { get; set; }
    }

    public class GitHubAnalyticsRef
    {
        public GitHubAnalyticsCommit? Target { get; set; }
    }

    public class GitHubAnalyticsCommit
    {
        public GitHubCommitHistory? History { get; set; }
    }

    public class GitHubCommitHistory
    {
        public int TotalCount { get; set; }
        public List<GitHubAnalyticsCommitNode>? Nodes { get; set; }
    }

    public class GitHubAnalyticsCommitNode
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CommittedDate { get; set; }
        public GitHubCommitAuthor? Author { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int ChangedFiles { get; set; }
    }

    public class GitHubCommitAuthor
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public GitHubRestApiUser? User { get; set; }
    }

    public class GitHubIssueNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("closed_at")]
        public DateTime? ClosedAt { get; set; }
        [JsonPropertyName("user")]
        public GitHubRestApiUser? User { get; set; }
        public List<string> Labels { get; set; } = new List<string>();
        public int Comments { get; set; }
        public string? Body { get; set; }
    }

    public class GitHubPullRequestNode
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("closed_at")]
        public DateTime? ClosedAt { get; set; }
        [JsonPropertyName("merged_at")]
        public DateTime? MergedAt { get; set; }
        [JsonPropertyName("user")]
        public GitHubRestApiUser? User { get; set; }
        [JsonPropertyName("merged_by")]
        public GitHubRestApiUser? MergedBy { get; set; }
        [JsonPropertyName("body")]
        public string? Body { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int ChangedFiles { get; set; }
        [JsonPropertyName("head")]
        public object? Head { get; set; }
        [JsonPropertyName("base")]
        public object? Base { get; set; }
    }

    public class GitHubPullRequestReviewNode
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("user")]
        public GitHubRestApiCommitUser? User { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty; // APPROVED, CHANGES_REQUESTED, COMMENTED, etc.

        [JsonPropertyName("submitted_at")]
        public DateTime? SubmittedAt { get; set; }
    }

    #endregion
}
