using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitHubUsername",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GitHubRepositories",
                columns: table => new
                {
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RepositoryUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StarsCount = table.Column<int>(type: "integer", nullable: true),
                    ForksCount = table.Column<int>(type: "integer", nullable: true),
                    OpenIssuesCount = table.Column<int>(type: "integer", nullable: true),
                    OpenPullRequestsCount = table.Column<int>(type: "integer", nullable: true),
                    DefaultBranch = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PrimaryLanguage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Languages = table.Column<string>(type: "text", nullable: false),
                    License = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    IsFork = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubRepositories", x => x.RepositoryId);
                    table.ForeignKey(
                        name: "FK_GitHubRepositories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GitHubAnalytics",
                columns: table => new
                {
                    AnalyticsId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DaysPeriod = table.Column<int>(type: "integer", nullable: false),
                    TotalCommits = table.Column<int>(type: "integer", nullable: false),
                    TotalAdditions = table.Column<int>(type: "integer", nullable: false),
                    TotalDeletions = table.Column<int>(type: "integer", nullable: false),
                    TotalLinesChanged = table.Column<int>(type: "integer", nullable: false),
                    TotalIssues = table.Column<int>(type: "integer", nullable: false),
                    OpenIssues = table.Column<int>(type: "integer", nullable: false),
                    ClosedIssues = table.Column<int>(type: "integer", nullable: false),
                    TotalPullRequests = table.Column<int>(type: "integer", nullable: false),
                    OpenPullRequests = table.Column<int>(type: "integer", nullable: false),
                    MergedPullRequests = table.Column<int>(type: "integer", nullable: false),
                    ClosedPullRequests = table.Column<int>(type: "integer", nullable: false),
                    TotalBranches = table.Column<int>(type: "integer", nullable: false),
                    TotalReleases = table.Column<int>(type: "integer", nullable: false),
                    TotalTags = table.Column<int>(type: "integer", nullable: false),
                    AverageTimeToCloseIssues = table.Column<double>(type: "double precision", nullable: true),
                    AverageTimeToMergePRs = table.Column<double>(type: "double precision", nullable: true),
                    ActiveContributors = table.Column<int>(type: "integer", nullable: false),
                    TotalContributors = table.Column<int>(type: "integer", nullable: false),
                    FirstCommitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCommitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalStars = table.Column<int>(type: "integer", nullable: false),
                    TotalForks = table.Column<int>(type: "integer", nullable: false),
                    TotalWatchers = table.Column<int>(type: "integer", nullable: false),
                    LanguageStats = table.Column<string>(type: "text", nullable: false),
                    WeeklyCommits = table.Column<string>(type: "text", nullable: false),
                    WeeklyIssues = table.Column<string>(type: "text", nullable: false),
                    WeeklyPullRequests = table.Column<string>(type: "text", nullable: false),
                    MonthlyCommits = table.Column<string>(type: "text", nullable: false),
                    MonthlyIssues = table.Column<string>(type: "text", nullable: false),
                    MonthlyPullRequests = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubAnalytics", x => x.AnalyticsId);
                    table.ForeignKey(
                        name: "FK_GitHubAnalytics_GitHubRepositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "GitHubRepositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GitHubContributions",
                columns: table => new
                {
                    ContributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GitHubUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DaysPeriod = table.Column<int>(type: "integer", nullable: false),
                    TotalCommits = table.Column<int>(type: "integer", nullable: false),
                    TotalAdditions = table.Column<int>(type: "integer", nullable: false),
                    TotalDeletions = table.Column<int>(type: "integer", nullable: false),
                    TotalLinesChanged = table.Column<int>(type: "integer", nullable: false),
                    UniqueDaysWithCommits = table.Column<int>(type: "integer", nullable: false),
                    FirstCommitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCommitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalIssuesCreated = table.Column<int>(type: "integer", nullable: false),
                    OpenIssuesCreated = table.Column<int>(type: "integer", nullable: false),
                    ClosedIssuesCreated = table.Column<int>(type: "integer", nullable: false),
                    IssuesCommentedOn = table.Column<int>(type: "integer", nullable: false),
                    IssuesAssigned = table.Column<int>(type: "integer", nullable: false),
                    IssuesClosed = table.Column<int>(type: "integer", nullable: false),
                    TotalPullRequestsCreated = table.Column<int>(type: "integer", nullable: false),
                    OpenPullRequestsCreated = table.Column<int>(type: "integer", nullable: false),
                    MergedPullRequestsCreated = table.Column<int>(type: "integer", nullable: false),
                    ClosedPullRequestsCreated = table.Column<int>(type: "integer", nullable: false),
                    PullRequestsReviewed = table.Column<int>(type: "integer", nullable: false),
                    PullRequestsApproved = table.Column<int>(type: "integer", nullable: false),
                    PullRequestsRequestedChanges = table.Column<int>(type: "integer", nullable: false),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    ReviewsApproved = table.Column<int>(type: "integer", nullable: false),
                    ReviewsRequestedChanges = table.Column<int>(type: "integer", nullable: false),
                    ReviewsCommented = table.Column<int>(type: "integer", nullable: false),
                    AverageReviewTime = table.Column<double>(type: "double precision", nullable: true),
                    CommitsByDayOfWeek = table.Column<string>(type: "text", nullable: false),
                    CommitsByHour = table.Column<string>(type: "text", nullable: false),
                    ActivityByMonth = table.Column<string>(type: "text", nullable: false),
                    FilesModified = table.Column<int>(type: "integer", nullable: false),
                    LanguagesContributed = table.Column<string>(type: "text", nullable: false),
                    AverageCommitSize = table.Column<double>(type: "double precision", nullable: true),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    CollaboratorsInteractedWith = table.Column<int>(type: "integer", nullable: false),
                    DiscussionsParticipated = table.Column<int>(type: "integer", nullable: false),
                    WikiPagesEdited = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubContributions", x => x.ContributionId);
                    table.ForeignKey(
                        name: "FK_GitHubContributions_GitHubRepositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "GitHubRepositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GitHubContributions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GitHubSyncLogs",
                columns: table => new
                {
                    SyncLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ItemsProcessed = table.Column<int>(type: "integer", nullable: true),
                    TotalItems = table.Column<int>(type: "integer", nullable: true),
                    RemainingRequests = table.Column<int>(type: "integer", nullable: true),
                    RateLimitResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncDetails = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubSyncLogs", x => x.SyncLogId);
                    table.ForeignKey(
                        name: "FK_GitHubSyncLogs_GitHubRepositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "GitHubRepositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GitHubAnalytics_RepositoryId",
                table: "GitHubAnalytics",
                column: "RepositoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GitHubContributions_RepositoryId",
                table: "GitHubContributions",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_GitHubContributions_UserId",
                table: "GitHubContributions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GitHubRepositories_ProjectId",
                table: "GitHubRepositories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_GitHubSyncLogs_RepositoryId",
                table: "GitHubSyncLogs",
                column: "RepositoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GitHubAnalytics");

            migrationBuilder.DropTable(
                name: "GitHubContributions");

            migrationBuilder.DropTable(
                name: "GitHubSyncLogs");

            migrationBuilder.DropTable(
                name: "GitHubRepositories");

            migrationBuilder.DropColumn(
                name: "GitHubUsername",
                table: "Users");
        }
    }
}
