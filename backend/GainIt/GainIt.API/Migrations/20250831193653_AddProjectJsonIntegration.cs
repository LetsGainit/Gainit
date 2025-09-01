using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectJsonIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_ProjectMilestones_ProjectId",
                table: "ProjectMilestones",
                column: "ProjectId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForumPostLikes");

            migrationBuilder.DropTable(
                name: "ForumReplyLikes");

            migrationBuilder.DropTable(
                name: "ProjectSubtasks");

            migrationBuilder.DropTable(
                name: "ProjectTaskReferences");

            migrationBuilder.DropTable(
                name: "TaskDependencies");

            migrationBuilder.DropTable(
                name: "ForumReplies");

            migrationBuilder.DropTable(
                name: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "ForumPosts");

            migrationBuilder.DropTable(
                name: "ProjectMilestones");
        }
    }
}
