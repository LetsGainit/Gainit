using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRagContextToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchievementTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnlockCriteria = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserRole = table.Column<int>(type: "integer", nullable: true),
                    Biography = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FacebookPageURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkedInURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GitHubURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GitHubUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProfilePictureURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EarnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EarnedDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_AchievementTemplates_AchievementTemplateId",
                        column: x => x.AchievementTemplateId,
                        principalTable: "AchievementTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserExpertises",
                columns: table => new
                {
                    ExpertiseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExpertises", x => x.ExpertiseId);
                    table.ForeignKey(
                        name: "FK_UserExpertises_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NonprofitExpertises",
                columns: table => new
                {
                    ExpertiseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldOfWork = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MissionStatement = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonprofitExpertises", x => x.ExpertiseId);
                    table.ForeignKey(
                        name: "FK_NonprofitExpertises_UserExpertises_ExpertiseId",
                        column: x => x.ExpertiseId,
                        principalTable: "UserExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechExpertises",
                columns: table => new
                {
                    ExpertiseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammingLanguages = table.Column<List<string>>(type: "text[]", nullable: false),
                    Technologies = table.Column<List<string>>(type: "text[]", nullable: false),
                    Tools = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechExpertises", x => x.ExpertiseId);
                    table.ForeignKey(
                        name: "FK_TechExpertises_UserExpertises_ExpertiseId",
                        column: x => x.ExpertiseId,
                        principalTable: "UserExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nonprofits",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: false),
                    NonprofitExpertiseExpertiseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nonprofits", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Nonprofits_NonprofitExpertises_NonprofitExpertiseExpertiseId",
                        column: x => x.NonprofitExpertiseExpertiseId,
                        principalTable: "NonprofitExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nonprofits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Gainers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EducationStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AreasOfInterest = table.Column<List<string>>(type: "text[]", nullable: false),
                    TechExpertiseExpertiseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gainers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Gainers_TechExpertises_TechExpertiseExpertiseId",
                        column: x => x.TechExpertiseExpertiseId,
                        principalTable: "TechExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Gainers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mentors",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false),
                    AreaOfExpertise = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TechExpertiseExpertiseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mentors", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Mentors_TechExpertises_TechExpertiseExpertiseId",
                        column: x => x.TechExpertiseExpertiseId,
                        principalTable: "TechExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mentors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProjectDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: false),
                    ProjectPictureUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Goals = table.Column<List<string>>(type: "text[]", maxLength: 2000, nullable: false),
                    Technologies = table.Column<List<string>>(type: "text[]", nullable: false),
                    RequiredRoles = table.Column<List<string>>(type: "text[]", nullable: false),
                    ProjectKind = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    ProjectStatus = table.Column<int>(type: "integer", nullable: true),
                    ProjectSource = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RepositoryLink = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OwningOrganizationUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProgrammingLanguages = table.Column<string>(type: "text", nullable: true),
                    GainerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MentorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RagContext = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_Gainers_GainerUserId",
                        column: x => x.GainerUserId,
                        principalTable: "Gainers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Projects_Mentors_MentorUserId",
                        column: x => x.MentorUserId,
                        principalTable: "Mentors",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Projects_Nonprofits_OwningOrganizationUserId",
                        column: x => x.OwningOrganizationUserId,
                        principalTable: "Nonprofits",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ForumPosts",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    ReplyCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumPosts", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_ForumPosts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumPosts_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    IsFork = table.Column<bool>(type: "boolean", nullable: false),
                    Branches = table.Column<string>(type: "text", nullable: false)
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
                name: "JoinRequests",
                columns: table => new
                {
                    JoinRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeciderUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecisionAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinRequests", x => x.JoinRequestId);
                    table.ForeignKey(
                        name: "FK_JoinRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JoinRequests_Users_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMilestones",
                columns: table => new
                {
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    TargetDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMilestones", x => x.MilestoneId);
                    table.ForeignKey(
                        name: "FK_ProjectMilestones_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForumPostLikes",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LikedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumPostLikes", x => new { x.PostId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ForumPostLikes_ForumPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "ForumPosts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumPostLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForumReplies",
                columns: table => new
                {
                    ReplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LikeCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumReplies", x => x.ReplyId);
                    table.ForeignKey(
                        name: "FK_ForumReplies_ForumPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "ForumPosts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumReplies_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
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

            migrationBuilder.CreateTable(
                name: "ProjectTasks",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    AssignedRole = table.Column<string>(type: "text", nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTasks", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_ProjectMilestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "ProjectMilestones",
                        principalColumn: "MilestoneId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForumReplyLikes",
                columns: table => new
                {
                    ReplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LikedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumReplyLikes", x => new { x.ReplyId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ForumReplyLikes_ForumReplies_ReplyId",
                        column: x => x.ReplyId,
                        principalTable: "ForumReplies",
                        principalColumn: "ReplyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumReplyLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSubtasks",
                columns: table => new
                {
                    SubtaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSubtasks", x => x.SubtaskId);
                    table.ForeignKey(
                        name: "FK_ProjectSubtasks_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTaskReferences",
                columns: table => new
                {
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTaskReferences", x => x.ReferenceId);
                    table.ForeignKey(
                        name: "FK_ProjectTaskReferences_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskDependencies",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependsOnTaskId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependencies", x => new { x.TaskId, x.DependsOnTaskId });
                    table.ForeignKey(
                        name: "FK_TaskDependencies_ProjectTasks_DependsOnTaskId",
                        column: x => x.DependsOnTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumPostLikes_PostId",
                table: "ForumPostLikes",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPostLikes_UserId",
                table: "ForumPostLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_AuthorId",
                table: "ForumPosts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_CreatedAtUtc",
                table: "ForumPosts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_ProjectId",
                table: "ForumPosts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_AuthorId",
                table: "ForumReplies",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_CreatedAtUtc",
                table: "ForumReplies",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_PostId",
                table: "ForumReplies",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplyLikes_ReplyId",
                table: "ForumReplyLikes",
                column: "ReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplyLikes_UserId",
                table: "ForumReplyLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Gainers_TechExpertiseExpertiseId",
                table: "Gainers",
                column: "TechExpertiseExpertiseId");

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

            migrationBuilder.CreateIndex(
                name: "IX_JoinRequests_ProjectId_RequesterUserId",
                table: "JoinRequests",
                columns: new[] { "ProjectId", "RequesterUserId" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JoinRequests_ProjectId_Status",
                table: "JoinRequests",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JoinRequests_RequesterUserId",
                table: "JoinRequests",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Mentors_TechExpertiseExpertiseId",
                table: "Mentors",
                column: "TechExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_Nonprofits_NonprofitExpertiseExpertiseId",
                table: "Nonprofits",
                column: "NonprofitExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_ProjectId",
                table: "ProjectMilestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_GainerUserId",
                table: "Projects",
                column: "GainerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_MentorUserId",
                table: "Projects",
                column: "MentorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwningOrganizationUserId",
                table: "Projects",
                column: "OwningOrganizationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSubtasks_TaskId_OrderIndex",
                table: "ProjectSubtasks",
                columns: new[] { "TaskId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskReferences_TaskId_Type",
                table: "ProjectTaskReferences",
                columns: new[] { "TaskId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_MilestoneId",
                table: "ProjectTasks",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_AssignedRole_AssignedUserId",
                table: "ProjectTasks",
                columns: new[] { "ProjectId", "AssignedRole", "AssignedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_Status_OrderIndex",
                table: "ProjectTasks",
                columns: new[] { "ProjectId", "Status", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_DependsOnTaskId",
                table: "TaskDependencies",
                column: "DependsOnTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementTemplateId",
                table: "UserAchievements",
                column: "AchievementTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementTemplateId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExpertises_UserId",
                table: "UserExpertises",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId",
                table: "Users",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForumPostLikes");

            migrationBuilder.DropTable(
                name: "ForumReplyLikes");

            migrationBuilder.DropTable(
                name: "GitHubAnalytics");

            migrationBuilder.DropTable(
                name: "GitHubContributions");

            migrationBuilder.DropTable(
                name: "GitHubSyncLogs");

            migrationBuilder.DropTable(
                name: "JoinRequests");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "ProjectSubtasks");

            migrationBuilder.DropTable(
                name: "ProjectTaskReferences");

            migrationBuilder.DropTable(
                name: "TaskDependencies");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "ForumReplies");

            migrationBuilder.DropTable(
                name: "GitHubRepositories");

            migrationBuilder.DropTable(
                name: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "AchievementTemplates");

            migrationBuilder.DropTable(
                name: "ForumPosts");

            migrationBuilder.DropTable(
                name: "ProjectMilestones");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Gainers");

            migrationBuilder.DropTable(
                name: "Mentors");

            migrationBuilder.DropTable(
                name: "Nonprofits");

            migrationBuilder.DropTable(
                name: "TechExpertises");

            migrationBuilder.DropTable(
                name: "NonprofitExpertises");

            migrationBuilder.DropTable(
                name: "UserExpertises");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
