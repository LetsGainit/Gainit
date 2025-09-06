using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Models.ProjectForum;
using GainIt.API.Models.Tasks;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Expertise;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;

namespace GainIt.API.Data
{
    public class GainItDbContext : DbContext
    {
        private readonly ILogger<GainItDbContext> r_logger;

        public GainItDbContext(DbContextOptions<GainItDbContext> i_Options, ILogger<GainItDbContext> i_logger) : base(i_Options)
        {
            r_logger = i_logger;
        }

        #region User Hierarchy
        /// <summary>
        /// Base users table - contains common user properties
        /// Uses TPT (Table Per Type) inheritance strategy
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gainers table - extends User with gainer-specific properties
        /// </summary>
        public DbSet<Gainer> Gainers { get; set; }

        /// <summary>
        /// Mentors table - extends User with mentor-specific properties
        /// </summary>
        public DbSet<Mentor> Mentors { get; set; }

        /// <summary>
        /// Nonprofit organizations table - extends User with nonprofit-specific properties
        /// </summary>
        public DbSet<NonprofitOrganization> Nonprofits { get; set; }
        #endregion

        #region Project Hierarchy
        /// <summary>
        /// User projects table - actual projects created by users
        /// Uses TPH (Table Per Hierarchy) inheritance strategy
        /// </summary>
        public DbSet<UserProject> Projects { get; set; }

        /// <summary>
        /// Template projects table - project templates that can be used as a base
        /// Uses TPH (Table Per Hierarchy) inheritance strategy
        /// Both DbSets map to the same "Projects" table
        /// </summary>
        public DbSet<TemplateProject> TemplateProjects { get; set; }
        #endregion

        #region Expertise System
        /// <summary>
        /// Base expertise table - contains common expertise properties
        /// Uses TPT (Table Per Type) inheritance strategy
        /// </summary>
        public DbSet<UserExpertise> UserExpertises { get; set; }

        /// <summary>
        /// Tech expertise table - extends UserExpertise with tech-specific properties
        /// </summary>
        public DbSet<TechExpertise> TechExpertises { get; set; }

        /// <summary>
        /// Nonprofit expertise table - extends UserExpertise with nonprofit-specific properties
        /// </summary>
        public DbSet<NonprofitExpertise> NonprofitExpertises { get; set; }
        #endregion

        #region Achievement System
        /// <summary>
        /// Achievement templates table - defines available achievements
        /// Contains the pool of all possible achievements users can earn
        /// </summary>
        public DbSet<AchievementTemplate> AchievementTemplates { get; set; }

        /// <summary>
        /// User achievements table - tracks which achievements users have earned
        /// Links users to their earned achievements with additional metadata
        /// </summary>
        public DbSet<UserAchievement> UserAchievements { get; set; }
        #endregion

        #region Project Member System
        /// <summary>
        /// Project members table - tracks team members and their roles in projects
        /// </summary>
        public DbSet<ProjectMember> ProjectMembers { get; set; }

        public DbSet<JoinRequest> JoinRequests { get; set; }
        #endregion

        #region Forum System
        /// <summary>
        /// Forum posts table - contains discussion posts for projects
        /// </summary>
        public DbSet<ForumPost> ForumPosts { get; set; }

        /// <summary>
        /// Forum replies table - contains replies to forum posts
        /// </summary>
        public DbSet<ForumReply> ForumReplies { get; set; }

        /// <summary>
        /// Forum post likes table - tracks likes on forum posts
        /// </summary>
        public DbSet<ForumPostLike> ForumPostLikes { get; set; }

        /// <summary>
        /// Forum reply likes table - tracks likes on forum replies
        /// </summary>
        public DbSet<ForumReplyLike> ForumReplyLikes { get; set; }
        #endregion

        #region Task System
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<ProjectSubtask> ProjectSubtasks { get; set; }
        public DbSet<ProjectMilestone> ProjectMilestones { get; set; }
        public DbSet<ProjectTaskReference> ProjectTaskReferences { get; set; }
        public DbSet<TaskDependency> TaskDependencies { get; set; }
        #endregion

        #region GitHub Integration
        /// <summary>
        /// GitHub repositories linked to projects
        /// </summary>
        public DbSet<GitHubRepository> GitHubRepositories { get; set; }

        /// <summary>
        /// GitHub analytics data for repositories
        /// </summary>
        public DbSet<GitHubAnalytics> GitHubAnalytics { get; set; }

        /// <summary>
        /// GitHub user contributions to repositories
        /// </summary>
        public DbSet<GitHubContribution> GitHubContributions { get; set; }

        /// <summary>
        /// GitHub synchronization logs
        /// </summary>
        public DbSet<GitHubSyncLog> GitHubSyncLogs { get; set; }
        #endregion

        #region Database Operation Logging
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var changeCount = ChangeTracker.Entries().Count(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted);
            
            r_logger.LogInformation("Starting database save operation: ChangeCount={ChangeCount}", changeCount);

            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);
                var duration = DateTime.UtcNow - startTime;
                
                r_logger.LogInformation("Database save completed successfully: ChangeCount={ChangeCount}, Duration={Duration}ms", changeCount, duration.TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                r_logger.LogError(ex, "Database save failed: ChangeCount={ChangeCount}, Duration={Duration}ms", changeCount, duration.TotalMilliseconds);
                throw;
            }
        }

        public override int SaveChanges()
        {
            var startTime = DateTime.UtcNow;
            var changeCount = ChangeTracker.Entries().Count(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted);
            
            r_logger.LogInformation("Starting database save operation (sync): ChangeCount={ChangeCount}", changeCount);

            try
            {
                var result = base.SaveChanges();
                var duration = DateTime.UtcNow - startTime;
                
                r_logger.LogInformation("Database save completed successfully (sync): ChangeCount={ChangeCount}, Duration={Duration}ms", changeCount, duration.TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                r_logger.LogError(ex, "Database save failed (sync): ChangeCount={ChangeCount}, Duration={Duration}ms", changeCount, duration.TotalMilliseconds);
                throw;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                r_logger.LogWarning("DbContext not configured, using default configuration");
            }
            
            base.OnConfiguring(optionsBuilder);
        }

        public override void Dispose()
        {
            r_logger.LogDebug("Disposing GainItDbContext");
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            r_logger.LogDebug("Disposing GainItDbContext asynchronously");
            return base.DisposeAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            r_logger.LogDebug("Configuring database model");
            
            try
            {
                // Reusable ValueComparers for JSON-backed collections to satisfy EF Core model validation
                var stringListComparer = new ValueComparer<List<string>>(
                    (a, b) => a == b || (a != null && b != null && a.SequenceEqual(b)),
                    v => v == null ? 0 : v.Aggregate(17, (hash, s) => HashCode.Combine(hash, s != null ? s.GetHashCode() : 0)),
                    v => v == null ? new List<string>() : v.ToList()
                );

                var stringIntDictComparer = new ValueComparer<Dictionary<string, int>>(
                    (a, b) => a == b || (a != null && b != null && a.Count == b.Count && !a.Except(b).Any()),
                    v => v == null ? 0 : v.OrderBy(k => k.Key).Aggregate(19, (hash, kv) => HashCode.Combine(hash, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
                    v => v == null ? new Dictionary<string, int>() : v.ToDictionary(e => e.Key, e => e.Value)
                );
                #region Inheritance Configuration
                // Configure TPT inheritance for User hierarchy
                modelBuilder.Entity<User>().UseTptMappingStrategy();

                // Configure TPH inheritance for Project hierarchy (required for JSON properties)
                modelBuilder.Entity<TemplateProject>().UseTphMappingStrategy();

                // Configure TPT inheritance for UserExpertise hierarchy
                modelBuilder.Entity<UserExpertise>().UseTptMappingStrategy();
                #endregion

                #region User Configuration
                // Configure User entity
                modelBuilder.Entity<User>(entity =>
                {
                    entity.Property(e => e.FullName)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(e => e.EmailAddress)
                        .IsRequired()
                        .HasMaxLength(200);
                });
                #endregion

                #region Project Configuration


                // Configure base TemplateProject (base entity for TPH inheritance)
                modelBuilder.Entity<TemplateProject>(entity =>
                {
                    // Set the table name to "Projects" (instead of default "TemplateProjects")
                    entity.ToTable("Projects");

                    // Configure discriminator for TPH inheritance (avoiding conflict with RagContext.ProjectType)
                    entity.HasDiscriminator<string>("ProjectKind")
                        .HasValue<TemplateProject>("TemplateProject")
                        .HasValue<UserProject>("UserProject");

                    entity.Property(e => e.ProjectName)
                        .IsRequired()
                        .HasMaxLength(200);

                    entity.Property(e => e.ProjectDescription)
                        .IsRequired()
                        .HasMaxLength(1000);

                        // Configure Duration to handle PostgreSQL interval type properly
                    entity.Property(e => e.Duration)
                        .HasConversion(
                            v => v.TotalDays, // Convert TimeSpan to days for storage
                            v => TimeSpan.FromDays(v)); // Convert days back to TimeSpan



                    // Configure RagContext as owned entity (stored as JSON in same table)
                    entity.OwnsOne(tp => tp.RagContext, rag =>
                    {
                        // Force EF to detect the RAG context as JSON
                        rag.ToJson();

                        rag.Property(r => r.SearchableText)
                            .HasMaxLength(5000);

                        rag.Property(r => r.ProjectType)
                            .HasMaxLength(100);

                        rag.Property(r => r.Domain)
                            .HasMaxLength(100);

                        // No need for HasConversion - JSON handles List<string> automatically
                        // Tags, SkillLevels, LearningOutcomes, ComplexityFactors will be arrays in JSON
                    });
                });

                // Configure UserProject (inherits from TemplateProject)
                modelBuilder.Entity<UserProject>(entity =>
                {
                    // Configure UserProject-specific properties
                    entity.Property(e => e.ProjectStatus)
                        .IsRequired();

                    entity.Property(e => e.ProjectSource)
                        .IsRequired();

                    entity.Property(e => e.CreatedAtUtc)
                        .IsRequired();

                    entity.Property(e => e.RepositoryLink)
                        .HasMaxLength(2048); // URL max length

                    entity.Property(e => e.OwningOrganizationUserId);

                    // Configure ProgrammingLanguages as JSON
                    entity.Property(e => e.ProgrammingLanguages)
                        .HasConversion(
                            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                        )
                        .Metadata.SetValueComparer(stringListComparer);

                    // Configure relationships
                    entity.HasOne<NonprofitOrganization>(p => p.OwningOrganization)
                        .WithMany(n => n.OwnedProjects)
                        .HasForeignKey("OwningOrganizationUserId")
                        .OnDelete(DeleteBehavior.SetNull);

                    // Configure collections
                    entity.HasMany(p => p.ProjectMembers)
                        .WithOne(pm => pm.Project)
                        .HasForeignKey(pm => pm.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(p => p.Tasks)
                        .WithOne(t => t.Project)
                        .HasForeignKey(t => t.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(p => p.Milestones)
                        .WithOne(m => m.Project)
                        .HasForeignKey(m => m.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
                #endregion

                #region JoinRequest Configuration
                modelBuilder.Entity<JoinRequest>(entity =>
                {
                    entity.HasKey(j => j.JoinRequestId);

                    // Relationships
                    entity.HasOne(j => j.Project)
                        .WithMany() // optional: add List<JoinRequest> in UserProject for reverse navigation
                        .HasForeignKey(j => j.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(j => j.RequesterUser)
                        .WithMany() // optional: add reverse navigation in User if needed
                        .HasForeignKey(j => j.RequesterUserId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // DeciderUserId is just a nullable Guid without navigation

                    // Properties
                    entity.Property(j => j.Status)
                        .IsRequired();

                    entity.Property(j => j.RequestedRole)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(j => j.Message)
                        .HasMaxLength(1000);

                    entity.Property(j => j.DecisionReason)
                        .HasMaxLength(1000);

                    entity.Property(j => j.CreatedAtUtc)
                        .IsRequired();

                    // Indexes
                    entity.HasIndex(j => new { j.ProjectId, j.Status });
                    entity.HasIndex(j => new { j.ProjectId, j.RequesterUserId });

                    // Prevent duplicate Pending request for the same Project+User (Partial Index)
                    entity.HasIndex(j => new { j.ProjectId, j.RequesterUserId })
                        .HasFilter("\"Status\" = 0") // 0 = Pending
                        .IsUnique();
                });
                #endregion

                #region Task System Configuration

                // ProjectTask
                modelBuilder.Entity<ProjectTask>(entity =>
                {
                    entity.HasKey(t => t.TaskId);

                    // FK to UserProject (one project -> many tasks)
                    entity.HasOne(t => t.Project)
                          .WithMany(p => p.Tasks)
                          .HasForeignKey(t => t.ProjectId)
                          .OnDelete(DeleteBehavior.Cascade);

                    // FK to ProjectMilestone (optional; one milestone -> many tasks)
                    entity.HasOne(t => t.Milestone)
                          .WithMany(m => m.Tasks)
                          .HasForeignKey(t => t.MilestoneId)
                          .OnDelete(DeleteBehavior.SetNull);

                    entity.HasIndex(t => new { t.ProjectId, t.AssignedRole, t.AssignedUserId });

                    // Useful index for Kanban queries and lists
                    entity.HasIndex(t => new { t.ProjectId, t.Status, t.OrderIndex });
                    entity.Property(t => t.Title).HasMaxLength(120).IsRequired();
                    entity.Property(t => t.Description).HasMaxLength(4000);
                });

                // ProjectSubtask
                modelBuilder.Entity<ProjectSubtask>(entity =>
                {
                    entity.HasKey(s => s.SubtaskId);

                    // FK to ProjectTask (one task -> many subtasks)
                    entity.HasOne(s => s.Task)
                          .WithMany(t => t.Subtasks)
                          .HasForeignKey(s => s.TaskId)
                          .OnDelete(DeleteBehavior.Cascade);

                    // Optional index for fast ordering inside a task
                    entity.HasIndex(s => new { s.TaskId, s.OrderIndex });
                });

                // ProjectMilestone
                modelBuilder.Entity<ProjectMilestone>(entity =>
                {
                    entity.HasKey(m => m.MilestoneId);

                    // FK to UserProject (one project -> many milestones)
                    entity.HasOne(m => m.Project)
                          .WithMany(p => p.Milestones)
                          .HasForeignKey(m => m.ProjectId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.Property(m => m.Title).HasMaxLength(120).IsRequired();
                    entity.Property(m => m.Description).HasMaxLength(1000);
                });

                // ProjectTaskReference
                modelBuilder.Entity<ProjectTaskReference>(entity =>
                {
                    entity.HasKey(r => r.ReferenceId);

                    // FK to ProjectTask (one task -> many references)
                    entity.HasOne(r => r.Task)
                          .WithMany(t => t.References)
                          .HasForeignKey(r => r.TaskId)
                          .OnDelete(DeleteBehavior.Cascade);

                    // Helpful index for quick filtering
                    entity.HasIndex(r => new { r.TaskId, r.Type });

                    entity.Property(r => r.Url).HasMaxLength(2048).IsRequired();
                    entity.Property(r => r.Title).HasMaxLength(200);
                });

                // TaskDependency (self-relation on ProjectTask)
                modelBuilder.Entity<TaskDependency>(entity =>
                {
                    // Composite PK ensures a pair (TaskId, DependsOnTaskId) is unique
                    entity.HasKey(d => new { d.TaskId, d.DependsOnTaskId });

                    // The "dependent" task (has many dependencies)
                    entity.HasOne(d => d.Task)
                          .WithMany(t => t.Dependencies)
                          .HasForeignKey(d => d.TaskId)
                          .OnDelete(DeleteBehavior.Cascade);

                    // The task it depends on (no back-collection)
                    entity.HasOne(d => d.DependsOn)
                          .WithMany()
                          .HasForeignKey(d => d.DependsOnTaskId)
                          // Restrict: prevent deleting a task that others depend on by accident
                          .OnDelete(DeleteBehavior.Restrict);

                    // Optional: fast lookups by "who unlocks whom"
                    entity.HasIndex(d => d.DependsOnTaskId);
                });

                #endregion


                #region Expertise Configuration
                // Configure TechExpertise - One-to-One with User
                modelBuilder.Entity<TechExpertise>(entity =>
                {
                    entity.HasOne(e => e.User)
                        .WithOne()
                        .HasForeignKey<TechExpertise>("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure NonprofitExpertise - One-to-One with User
                modelBuilder.Entity<NonprofitExpertise>(entity =>
                {
                    entity.HasOne(e => e.User)
                        .WithOne()
                        .HasForeignKey<NonprofitExpertise>("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

                // Prevent shadow FK creation on Gainer/Mentor for TechExpertise
                modelBuilder.Entity<Gainer>()
                    .Ignore(g => g.TechExpertise);

                modelBuilder.Entity<Mentor>()
                    .Ignore(m => m.TechExpertise);
                #endregion

                #region Achievement Configuration
                // Configure UserAchievement
                modelBuilder.Entity<UserAchievement>(entity =>
                {
                    // Ensure a user can't earn the same achievement twice
                    entity.HasIndex(e => new { e.UserId, e.AchievementTemplateId })
                        .IsUnique();

                    // Configure relationships
                    entity.HasOne(e => e.User)
                        .WithMany(u => u.Achievements)
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(e => e.AchievementTemplate)
                        .WithMany()
                        .HasForeignKey(e => e.AchievementTemplateId)
                        .OnDelete(DeleteBehavior.Restrict);
                });

                // Configure AchievementTemplate
                modelBuilder.Entity<AchievementTemplate>(entity =>
                {
                    entity.Property(e => e.Title)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(e => e.Description)
                        .IsRequired()
                        .HasMaxLength(500);

                    entity.Property(e => e.IconUrl)
                        .HasMaxLength(200);

                    entity.Property(e => e.UnlockCriteria)
                        .IsRequired()
                        .HasMaxLength(500);
                });
                #endregion


                #region Project Member Configuration
                // Configure ProjectMember entity
                modelBuilder.Entity<ProjectMember>(entity =>
                {
                    // Configure composite key
                    entity.HasKey(e => new { e.ProjectId, e.UserId });

                    // Configure relationships
                    entity.HasOne(e => e.Project)
                        .WithMany(p => p.ProjectMembers) // does it makes sense project members its a list
                        .HasForeignKey(e => e.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(e => e.User)
                        .WithMany()
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // Configure required fields
                    entity.Property(e => e.UserRole)
                        .IsRequired();

                    entity.Property(e => e.JoinedAtUtc)
                        .IsRequired();
                });
                #endregion

                #region GitHub Configuration
                // Configure GitHubAnalytics entity
                modelBuilder.Entity<GitHubAnalytics>(entity =>
                {
                    // Configure dictionary properties to be stored as JSON
                    entity.Property(e => e.LanguageStats)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    entity.Property(e => e.WeeklyCommits)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    entity.Property(e => e.WeeklyIssues)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    entity.Property(e => e.WeeklyPullRequests)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    entity.Property(e => e.MonthlyCommits)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    entity.Property(e => e.MonthlyIssues)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    entity.Property(e => e.MonthlyPullRequests)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    // Configure relationships
                    entity.HasOne(e => e.Repository)
                        .WithOne(r => r.Analytics)
                        .HasForeignKey<GitHubAnalytics>(e => e.RepositoryId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
                #endregion

                #region GitHub Models Configuration
                // Configure GitHubContribution entity
                modelBuilder.Entity<GitHubContribution>(entity =>
                {
                    // Configure Dictionary<string, int> properties to be stored as JSON
                    entity.Property(e => e.CommitsByDayOfWeek)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    entity.Property(e => e.CommitsByHour)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    entity.Property(e => e.ActivityByMonth)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
                        )
                        .Metadata.SetValueComparer(stringIntDictComparer);

                    // Configure List<string> property to be stored as JSON
                    entity.Property(e => e.LanguagesContributed)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                        )
                        .Metadata.SetValueComparer(stringListComparer);

                    // Configure relationships
                    entity.HasOne(e => e.Repository)
                        .WithMany(r => r.Contributions)
                        .HasForeignKey(e => e.RepositoryId)
                        .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(e => e.User)
                        .WithMany()
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure GitHubRepository entity
                modelBuilder.Entity<GitHubRepository>(entity =>
                {
                    // Configure List<string> properties to be stored as JSON
                    entity.Property(e => e.Languages)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                        )
                        .Metadata.SetValueComparer(stringListComparer);

                    entity.Property(e => e.Branches)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                        )
                        .Metadata.SetValueComparer(stringListComparer);

                    // Configure relationships
                    entity.HasOne(e => e.Project)
                        .WithMany()
                        .HasForeignKey(e => e.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure GitHubSyncLog entity
                modelBuilder.Entity<GitHubSyncLog>(entity =>
                {
                    // Configure relationships
                    entity.HasOne(e => e.Repository)
                        .WithMany(r => r.SyncLogs)
                        .HasForeignKey(e => e.RepositoryId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
                #endregion

                #region Forum Configuration
                // Configure ForumPost entity
                modelBuilder.Entity<ForumPost>(entity =>
                {
                    entity.HasKey(e => e.PostId);

                    // FK to UserProject (one project -> many posts)
                    entity.HasOne(e => e.Project)
                        .WithMany()
                        .HasForeignKey(e => e.ProjectId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // FK to User (one user -> many posts)
                    entity.HasOne(e => e.Author)
                        .WithMany()
                        .HasForeignKey(e => e.AuthorId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // One post -> many replies
                    entity.HasMany(e => e.Replies)
                        .WithOne(r => r.Post)
                        .HasForeignKey(r => r.PostId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // One post -> many likes
                    entity.HasMany(e => e.Likes)
                        .WithOne(l => l.Post)
                        .HasForeignKey(l => l.PostId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // Properties
                    entity.Property(e => e.Content)
                        .IsRequired()
                        .HasMaxLength(2000);

                    entity.Property(e => e.CreatedAtUtc)
                        .IsRequired();

                    // Indexes for performance
                    entity.HasIndex(e => e.ProjectId);
                    entity.HasIndex(e => e.AuthorId);
                    entity.HasIndex(e => e.CreatedAtUtc);
                });

                // Configure ForumReply entity
                modelBuilder.Entity<ForumReply>(entity =>
                {
                    entity.HasKey(e => e.ReplyId);

                    // FK to ForumPost (one post -> many replies)
                    entity.HasOne(e => e.Post)
                        .WithMany(p => p.Replies)
                        .HasForeignKey(e => e.PostId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // FK to User (one user -> many replies)
                    entity.HasOne(e => e.Author)
                        .WithMany()
                        .HasForeignKey(e => e.AuthorId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // One reply -> many likes
                    entity.HasMany(e => e.Likes)
                        .WithOne(l => l.Reply)
                        .HasForeignKey(l => l.ReplyId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // Properties
                    entity.Property(e => e.Content)
                        .IsRequired()
                        .HasMaxLength(1000);

                    entity.Property(e => e.CreatedAtUtc)
                        .IsRequired();

                    // Indexes for performance
                    entity.HasIndex(e => e.PostId);
                    entity.HasIndex(e => e.AuthorId);
                    entity.HasIndex(e => e.CreatedAtUtc);
                });

                // Configure ForumPostLike entity
                modelBuilder.Entity<ForumPostLike>(entity =>
                {
                    // Composite key: PostId + UserId
                    entity.HasKey(e => new { e.PostId, e.UserId });

                    // FK to ForumPost
                    entity.HasOne(e => e.Post)
                        .WithMany(p => p.Likes)
                        .HasForeignKey(e => e.PostId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // FK to User
                    entity.HasOne(e => e.User)
                        .WithMany()
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // Properties
                    entity.Property(e => e.LikedAtUtc)
                        .IsRequired();

                    // Index for performance
                    entity.HasIndex(e => e.PostId);
                    entity.HasIndex(e => e.UserId);
                });

                // Configure ForumReplyLike entity
                modelBuilder.Entity<ForumReplyLike>(entity =>
                {
                    // Composite key: ReplyId + UserId
                    entity.HasKey(e => new { e.ReplyId, e.UserId });

                    // FK to ForumReply
                    entity.HasOne(e => e.Reply)
                        .WithMany(r => r.Likes)
                        .HasForeignKey(e => e.ReplyId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // FK to User
                    entity.HasOne(e => e.User)
                        .WithMany()
                        .HasForeignKey(e => e.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                    // Properties
                    entity.Property(e => e.LikedAtUtc)
                        .IsRequired();

                    // Index for performance
                    entity.HasIndex(e => e.ReplyId);
                    entity.HasIndex(e => e.UserId);
                });
                #endregion

                base.OnModelCreating(modelBuilder);
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error configuring database model");
                throw;
            }
        }
        #endregion
    }
    
    public static class GainItDbContextSeeder
    {
        public static void SeedData(GainItDbContext context, IProjectConfigurationService projectConfigService, ILogger? logger = null)
        {
            logger?.LogInformation("Starting database seeding process");
            
            // Only seed if database is empty
            if (!context.Users.Any())
            {
                #region Seed Users
                // Create a mentor
                var mentor = new Mentor
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Dr. Noa Cohen",
                    EmailAddress = "noa.cohen@techmentor.dev",
                    YearsOfExperience = 15,
                    AreaOfExpertise = "Full Stack Development",
                    Biography = "Senior software architect with expertise in cloud technologies and microservices.",
                    GitHubURL = "https://github.com/noacohen",
                    GitHubUsername = "noacohen",  // Add GitHub username
                    ProfilePictureURL = "https://randomuser.me/api/portraits/women/65.jpg",
                    LinkedInURL = "https://linkedin.com/company/mentors-in-tech",
                    FacebookPageURL = "https://facebook.com/TechCareerMentorship",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var mentor2 = new Mentor
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "David Levy",
                    EmailAddress = "david.levy@mentorspace.io",
                    YearsOfExperience = 10,
                    AreaOfExpertise = "Data Science & AI",
                    Biography = "Experienced data scientist and AI mentor, passionate about machine learning and analytics.",
                    GitHubURL = "https://github.com/davidlevy",
                    GitHubUsername = "davidlevy",  // Add GitHub username
                    ProfilePictureURL = "https://randomuser.me/api/portraits/men/66.jpg",
                    LinkedInURL = "https://linkedin.com/company/tech-career-mentorship",
                    FacebookPageURL = "https://facebook.com/TechCareerMentor",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var mentor3 = new Mentor
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Sarah Chen",
                    EmailAddress = "sarah.chen@techmentor.dev",
                    YearsOfExperience = 12,
                    AreaOfExpertise = "Mobile Development & UI/UX",
                    Biography = "Senior mobile developer and UI/UX expert with experience in React Native, Flutter, and native iOS/Android development.",
                    GitHubURL = "https://github.com/sarahchen",
                    GitHubUsername = "sarahchen",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/women/67.jpg",
                    LinkedInURL = "https://linkedin.com/in/sarahchen",
                    FacebookPageURL = "https://facebook.com/sarah.chen.tech",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var mentor4 = new Mentor
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Michael Rodriguez",
                    EmailAddress = "michael.rodriguez@mentorspace.io",
                    YearsOfExperience = 18,
                    AreaOfExpertise = "DevOps & Cloud Architecture",
                    Biography = "DevOps architect and cloud expert with extensive experience in AWS, Azure, Docker, and Kubernetes. Passionate about automation and infrastructure as code.",
                    GitHubURL = "https://github.com/michaelrodriguez",
                    GitHubUsername = "michaelrodriguez",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/men/68.jpg",
                    LinkedInURL = "https://linkedin.com/in/michaelrodriguez",
                    FacebookPageURL = "https://facebook.com/michael.rodriguez.tech",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                // Create nonprofit organizations
                var nonprofit = new NonprofitOrganization
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "TechForGood Israel",
                    EmailAddress = "contact@techforgood.co.il",
                    WebsiteUrl = "https://techforgood.co.il",
                    Biography = "Empowering Israeli communities through technology education and digital literacy programs.",
                    GitHubURL = "https://github.com/techforgood-israel",
                    GitHubUsername = "techforgood-israel",  // Add GitHub username
                    ProfilePictureURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQHIVTRK4hstoI6IBdXmslzxY96ql1mZQF6wg&s",
                    LinkedInURL = "https://linkedin.com/company/techforgood-israel",
                    FacebookPageURL = "https://facebook.com/TechForGoodIsrael",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var nonprofit2 = new NonprofitOrganization
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "TechForSeniors Foundation",
                    EmailAddress = "contact@techforseniors.org",
                    WebsiteUrl = "https://techforseniors.org",
                    Biography = "Empowering senior citizens with digital skills and technology access to improve their quality of life and social connections.",
                    GitHubURL = "https://github.com/techforseniors",
                    GitHubUsername = "techforseniors",
                    ProfilePictureURL = "https://images.unsplash.com/photo-1515378960530-7c0da6231fb1?q=80&w=400",
                    LinkedInURL = "https://linkedin.com/company/techforseniors",
                    FacebookPageURL = "https://facebook.com/TechForSeniors",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var nonprofit3 = new NonprofitOrganization
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Community Safety Network",
                    EmailAddress = "contact@communitysafetynet.org",
                    WebsiteUrl = "https://communitysafetynet.org",
                    Biography = "Building safer communities through technology-enabled emergency response systems and volunteer coordination.",
                    GitHubURL = "https://github.com/communitysafetynet",
                    GitHubUsername = "communitysafetynet",
                    ProfilePictureURL = "https://images.unsplash.com/photo-1560472354-b33ff0c44a43?q=80&w=400",
                    LinkedInURL = "https://linkedin.com/company/communitysafetynet",
                    FacebookPageURL = "https://facebook.com/CommunitySafetyNet",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                // Create NonprofitExpertise for the nonprofit
                var nonprofitExpertise = new NonprofitExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = nonprofit.UserId,
                    User = nonprofit,
                    FieldOfWork = "Technology Education",
                    MissionStatement = "To bridge the digital divide by providing accessible technology education and resources to underserved communities."
                };

                nonprofit.NonprofitExpertise = nonprofitExpertise;
                context.NonprofitExpertises.Add(nonprofitExpertise);

                var nonprofit2Expertise = new NonprofitExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = nonprofit2.UserId,
                    User = nonprofit2,
                    FieldOfWork = "Digital Literacy & Senior Technology",
                    MissionStatement = "To bridge the digital divide for senior citizens through education, support, and accessible technology solutions."
                };

                var nonprofit3Expertise = new NonprofitExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = nonprofit3.UserId,
                    User = nonprofit3,
                    FieldOfWork = "Community Safety & Emergency Response",
                    MissionStatement = "Building safer communities through technology-enabled emergency response systems and volunteer coordination."
                };

                nonprofit2.NonprofitExpertise = nonprofit2Expertise;
                nonprofit3.NonprofitExpertise = nonprofit3Expertise;
                context.NonprofitExpertises.AddRange(nonprofit2Expertise, nonprofit3Expertise);

                // Create TechExpertise for mentors
                var mentorTechExpertise = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = mentor.UserId,
                    User = mentor,
                    ProgrammingLanguages = new List<string> { "JavaScript", "Python", "Java", "C#" },
                    Technologies = new List<string> { "React", "Node.js", "ASP.NET Core", "Docker" },
                    Tools = new List<string> { "Git", "Azure", "AWS", "Visual Studio" }
                };

                var mentor2TechExpertise = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = mentor2.UserId,
                    User = mentor2,
                    ProgrammingLanguages = new List<string> { "Python", "R", "SQL", "Scala" },
                    Technologies = new List<string> { "TensorFlow", "PyTorch", "Spark", "Hadoop" },
                    Tools = new List<string> { "Jupyter", "Docker", "Kubernetes", "AWS SageMaker" }
                };

                var mentor3TechExpertise = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = mentor3.UserId,
                    User = mentor3,
                    ProgrammingLanguages = new List<string> { "JavaScript", "TypeScript", "Dart", "Swift", "Kotlin" },
                    Technologies = new List<string> { "React Native", "Flutter", "iOS SDK", "Android SDK", "Firebase" },
                    Tools = new List<string> { "Xcode", "Android Studio", "VS Code", "Git", "Figma" }
                };

                var mentor4TechExpertise = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = mentor4.UserId,
                    User = mentor4,
                    ProgrammingLanguages = new List<string> { "Python", "Bash", "YAML", "Terraform", "Go" },
                    Technologies = new List<string> { "Docker", "Kubernetes", "AWS", "Azure", "Jenkins" },
                    Tools = new List<string> { "Terraform", "Ansible", "Prometheus", "Grafana", "ELK Stack" }
                };

                mentor.TechExpertise = mentorTechExpertise;
                mentor2.TechExpertise = mentor2TechExpertise;
                mentor3.TechExpertise = mentor3TechExpertise;
                mentor4.TechExpertise = mentor4TechExpertise;
                context.TechExpertises.AddRange(mentorTechExpertise, mentor2TechExpertise, mentor3TechExpertise, mentor4TechExpertise);

                // Create some gainers
                var gainer1 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Yossi Rosenberg",  // a real top contributer on github
                    EmailAddress = "yossi.rosenberg@techlearner.dev",
                    EducationStatus = "Undergraduate",
                    AreasOfInterest = new List<string> { "Web Development", "UI/UX Design", "Cloud Computing" },
                    GitHubURL = "https://github.com/yossirosenberg",
                    GitHubUsername = "yossirosenberg",  // Add GitHub username
                    LinkedInURL = "https://linkedin.com/in/yossirosenberg",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/men/12.jpg",
                    FacebookPageURL = "https://facebook.com/yossi.rosenberg",
                    Biography = "Aspiring web developer passionate about building user-friendly applications and exploring cloud technologies.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var gainer2 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Maya Goldstein",
                    EmailAddress = "maya.goldstein@innovatelearn.net",
                    EducationStatus = "Graduate",
                    AreasOfInterest = new List<string> { "Machine Learning", "Data Science", "Python" },
                    GitHubURL = "https://github.com/mayagoldstein",
                    GitHubUsername = "mayagoldstein",  // Add GitHub username
                    LinkedInURL = "https://linkedin.com/in/mayagoldstein",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/women/22.jpg",
                    FacebookPageURL = "https://facebook.com/maya.goldstein",
                    Biography = "Graduate student specializing in machine learning and data science, with a love for Python and analytics.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var gainer3 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Amit Ben-David",
                    EmailAddress = "amit.bendavid@codelearner.io",
                    EducationStatus = "Undergraduate",
                    AreasOfInterest = new List<string> { "Mobile Development", "Android", "Kotlin" },
                    GitHubURL = "https://github.com/amitbendavid",
                    GitHubUsername = "amitbendavid",  // Add GitHub username
                    LinkedInURL = "https://linkedin.com/in/amitbendavid",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/men/33.jpg",
                    FacebookPageURL = "https://facebook.com/amit.bendavid",
                    Biography = "Mobile development enthusiast focused on Android and Kotlin, eager to create impactful mobile solutions.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var gainer4 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Tamar Weiss",
                    EmailAddress = "tamar.weiss@securitylearn.dev",
                    EducationStatus = "Graduate",
                    AreasOfInterest = new List<string> { "Cybersecurity", "Networks", "Linux" },
                    GitHubURL = "https://github.com/tamarweiss",
                    GitHubUsername = "tamarweiss",  // Add GitHub username
                    LinkedInURL = "https://linkedin.com/in/tamarweiss",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/women/44.jpg",
                    FacebookPageURL = "https://facebook.com/tamar.weiss",
                    Biography = "Cybersecurity graduate with a strong interest in network security and Linux systems administration.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var gainer5 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Eli Shalom",
                    EmailAddress = "eli.shalom@gamedev.net",
                    EducationStatus = "Undergraduate",
                    AreasOfInterest = new List<string> { "Game Development", "Unity", "C#" },
                    GitHubURL = "https://github.com/elishalom",
                    GitHubUsername = "elishalom",  // Add GitHub username
                    LinkedInURL = "https://linkedin.com/in/elishalom",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/men/55.jpg",
                    FacebookPageURL = "https://facebook.com/eli.shalom",
                    Biography = "Game development student passionate about Unity and C#, aiming to create engaging interactive experiences.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var gainer6 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Liora Barak",
                    EmailAddress = "liora.barak@ailearner.dev",
                    EducationStatus = "Graduate",
                    AreasOfInterest = new List<string> { "AI", "Natural Language Processing", "Python" },
                    GitHubURL = "https://github.com/liorabarak",
                    GitHubUsername = "chahmedejaz",  // Add GitHub username
                    LinkedInURL = "https://linkedin.com/in/liorabarak",
                    FacebookPageURL = "https://facebook.com/liora.barak",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/women/77.jpg",
                    Biography = "AI graduate with a focus on natural language processing and Python, dedicated to advancing intelligent systems.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var gainer7 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Daniel Cohen",
                    EmailAddress = "daniel.cohen@webdev.net",
                    EducationStatus = "Undergraduate",
                    AreasOfInterest = new List<string> { "Web Development", "React", "Node.js" },
                    GitHubURL = "https://github.com/danielcohen",
                    GitHubUsername = "danielcohen",
                    LinkedInURL = "https://linkedin.com/in/danielcohen",
                    FacebookPageURL = "https://facebook.com/daniel.cohen",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/men/88.jpg",
                    Biography = "Passionate web developer focused on modern JavaScript frameworks and full-stack development.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                var gainer8 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Rachel Green",
                    EmailAddress = "rachel.green@datascience.dev",
                    EducationStatus = "Graduate",
                    AreasOfInterest = new List<string> { "Data Science", "Machine Learning", "Python" },
                    GitHubURL = "https://github.com/rachelgreen",
                    GitHubUsername = "rachelgreen",
                    LinkedInURL = "https://linkedin.com/in/rachelgreen",
                    FacebookPageURL = "https://facebook.com/rachel.green",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/women/99.jpg",
                    Biography = "Data science enthusiast with expertise in machine learning algorithms and data visualization.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                // Create TechExpertise entries for each gainer
                var techExpertise1 = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = gainer1.UserId,
                    User = gainer1,
                    ProgrammingLanguages = new List<string> { "JavaScript", "Python", "Java" },
                    Technologies = new List<string> { "React", "Node.js", "Express" },
                    Tools = new List<string> { "Git", "Docker", "AWS" }
                };

                var techExpertise2 = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = gainer2.UserId,
                    User = gainer2,
                    ProgrammingLanguages = new List<string> { "Python", "R", "SQL" },
                    Technologies = new List<string> { "TensorFlow", "PyTorch", "Scikit-learn" },
                    Tools = new List<string> { "Jupyter", "Pandas", "NumPy" }
                };

                var techExpertise3 = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = gainer3.UserId,
                    User = gainer3,
                    ProgrammingLanguages = new List<string> { "Kotlin", "Java", "Swift" },
                    Technologies = new List<string> { "Android SDK", "Jetpack Compose", "Room" },
                    Tools = new List<string> { "Android Studio", "Firebase", "Git" }
                };

                var techExpertise4 = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = gainer4.UserId,
                    User = gainer4,
                    ProgrammingLanguages = new List<string> { "Python", "Bash", "C++" },
                    Technologies = new List<string> { "Django", "Flask", "FastAPI" },
                    Tools = new List<string> { "Wireshark", "Metasploit", "Kali Linux" }
                };

                var techExpertise5 = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = gainer5.UserId,
                    User = gainer5,
                    ProgrammingLanguages = new List<string> { "C#", "JavaScript", "Python" },
                    Technologies = new List<string> { "Unity", "Unreal Engine", "MonoGame" },
                    Tools = new List<string> { "Visual Studio", "Git", "Blender" }
                };

                var techExpertise6 = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = gainer6.UserId,
                    User = gainer6,
                    ProgrammingLanguages = new List<string> { "Python", "Java", "R" },
                    Technologies = new List<string> { "TensorFlow", "PyTorch", "Hugging Face" },
                    Tools = new List<string> { "Jupyter", "Git", "Docker" }
                };

                var techExpertise7 = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = gainer7.UserId,
                    User = gainer7,
                    ProgrammingLanguages = new List<string> { "JavaScript", "TypeScript", "HTML", "CSS" },
                    Technologies = new List<string> { "React", "Node.js", "Express", "Next.js" },
                    Tools = new List<string> { "VS Code", "Git", "npm", "Webpack" }
                };

                var techExpertise8 = new TechExpertise
                {
                    ExpertiseId = Guid.NewGuid(),
                    UserId = gainer8.UserId,
                    User = gainer8,
                    ProgrammingLanguages = new List<string> { "Python", "R", "SQL", "Julia" },
                    Technologies = new List<string> { "Pandas", "NumPy", "Scikit-learn", "Matplotlib" },
                    Tools = new List<string> { "Jupyter", "RStudio", "Git", "Docker" }
                };

                // Add TechExpertise entries to context
                context.TechExpertises.AddRange(techExpertise1, techExpertise2, techExpertise3, techExpertise4, techExpertise5, techExpertise6, techExpertise7, techExpertise8);

                // Link TechExpertise to Gainers
                gainer1.TechExpertise = techExpertise1;
                gainer2.TechExpertise = techExpertise2;
                gainer3.TechExpertise = techExpertise3;
                gainer4.TechExpertise = techExpertise4;
                gainer5.TechExpertise = techExpertise5;
                gainer6.TechExpertise = techExpertise6;
                gainer7.TechExpertise = techExpertise7;
                gainer8.TechExpertise = techExpertise8;

                // Add all users to context
                context.Users.AddRange(mentor, mentor2, mentor3, mentor4, nonprofit, nonprofit2, nonprofit3, gainer1, gainer2, gainer3, gainer4, gainer5, gainer6, gainer7, gainer8);
                context.SaveChanges();
                #endregion

                #region Seed Template Projects
                logger?.LogInformation("Loading template projects from configuration...");
                var templateProjects = projectConfigService.LoadTemplateProjects();
                
                if (templateProjects.Any())
                {
                    // Convert JSON projects to TemplateProject entities
                    var templateProjectEntities = templateProjects.Select(tp => new TemplateProject
                    {
                        ProjectId = Guid.NewGuid(), // Generate new GUID for each template project
                        ProjectName = tp.ProjectName,
                        ProjectDescription = tp.ProjectDescription,
                        DifficultyLevel = ParseDifficultyLevel(tp.DifficultyLevel),
                        ProjectPictureUrl = tp.ProjectPictureUrl,
                        Duration = TimeSpan.FromDays(tp.DurationDays),
                        Goals = tp.Goals,
                        Technologies = tp.Technologies,
                        RequiredRoles = tp.RequiredRoles,
                        RagContext = tp.RagContext != null ? new RagContext
                        {
                            SearchableText = tp.RagContext.SearchableText,
                            Tags = tp.RagContext.Tags,
                            SkillLevels = tp.RagContext.SkillLevels,
                            ProjectType = tp.RagContext.ProjectType,
                            Domain = tp.RagContext.Domain,
                            LearningOutcomes = tp.RagContext.LearningOutcomes,
                            ComplexityFactors = tp.RagContext.ComplexityFactors
                        } : null
                    }).ToList();

                    context.TemplateProjects.AddRange(templateProjectEntities);
                    context.SaveChanges();
                    logger?.LogInformation("Successfully seeded {Count} template projects", templateProjectEntities.Count);
                }
                else
                {
                    logger?.LogWarning("No template projects found in configuration, using fallback projects");
                    
                    // Fallback template projects if JSON loading fails
                    var fallbackTemplateProjects = new List<TemplateProject>
                    {
                        new TemplateProject
                        {
                            ProjectId = Guid.NewGuid(),
                            ProjectName = "Community Food Bank Management System",
                            ProjectDescription = "A web application to help food banks manage inventory, track donations, and coordinate volunteers. Features include donation tracking, volunteer scheduling, and inventory management.",
                            DifficultyLevel = eDifficultyLevel.Intermediate,
                            ProjectPictureUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=1000",
                            Duration = TimeSpan.FromDays(90),
                            Goals = new List<string>
                            {
                                "Create an efficient system for managing food bank operations",
                                "Improve volunteer coordination",
                                "Enhance donation tracking capabilities"
                            },
                            Technologies = new List<string> { "React", "Node.js", "MongoDB", "Express" },
                            RequiredRoles = new List<string> { "Frontend Developer", "Backend Developer", "UI/UX Designer", "Project Manager" },
                            RagContext = new RagContext
                            {
                                SearchableText = "Community Food Bank Management System - A web application for managing food bank operations, volunteer coordination, and donation tracking.",
                                Tags = new List<string> { "food-bank", "volunteer-management", "donation-tracking", "inventory-management", "community-service", "web-application", "react", "nodejs", "mongodb" },
                                SkillLevels = new List<string> { "intermediate" },
                                ProjectType = "web-app",
                                Domain = "social-impact",
                                LearningOutcomes = new List<string> { "Full-stack web development", "Database design", "User management systems", "Volunteer coordination workflows" },
                                ComplexityFactors = new List<string> { "Multi-user roles and permissions", "Real-time inventory tracking", "Volunteer scheduling algorithms", "Reporting and analytics" }
                            }
                        }
                    };

                    context.TemplateProjects.AddRange(fallbackTemplateProjects);
                    context.SaveChanges();
                }



                // Load nonprofit project suggestions for later use
                var nonprofitSuggestions = projectConfigService.LoadNonprofitProjectSuggestions();

                // Create individual projects
                var project1 = new UserProject
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = "TechForGood Learning Platform",
                    ProjectDescription = "An online learning platform for TechForGood Israel to provide free coding courses to underprivileged communities. Features include course management, progress tracking, and interactive coding exercises.",
                    ProjectStatus = eProjectStatus.InProgress,
                    ProjectSource = eProjectSource.NonprofitOrganization,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
                    OwningOrganization = nonprofit,
                    RepositoryLink = "https://github.com/classroomio/classroomio",
                    ProjectPictureUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?q=80&w=1000",
                    Duration = TimeSpan.FromDays(180),
                    Goals = new List<string>
                    {
                        "Help local businesses establish an online presence and connect with their community through a user-friendly platform.",
                        "Enable customers to easily discover and review local businesses.",
                        "Provide business owners with tools to manage their profiles and engage with customers.",
                        "Foster a supportive local business ecosystem."
                    },
                    Technologies = new List<string> { "HTML", "CSS", "JavaScript", "Firebase" },
                    RequiredRoles = new List<string> { "Web Developer", "UI Designer", "Content Writer" },
                    ProgrammingLanguages = new List<string> { "JavaScript", "HTML", "CSS" },
                    RagContext = new RagContext
                    {
                        SearchableText = "TechForGood Learning Platform - An online learning platform providing free coding courses to underprivileged communities with course management and progress tracking.",
                        Tags = new List<string> { "learning-platform", "coding-courses", "education", "tech-for-good", "course-management", "progress-tracking", "interactive-exercises", "web-development", "html", "css", "javascript", "firebase" },
                        SkillLevels = new List<string> { "beginner", "intermediate" },
                        ProjectType = "web-app",
                        Domain = "education",
                        LearningOutcomes = new List<string> { "Web development fundamentals", "Course management systems", "User authentication", "Progress tracking implementation" },
                        ComplexityFactors = new List<string> { "Multi-course content management", "User progress tracking", "Interactive coding exercises", "Responsive design requirements" }
                    }
                };

                var project2 = new UserProject
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = "Community Garden Management System",
                    ProjectDescription = "A system to help community gardens manage plots, track plant growth, and coordinate volunteer schedules. Includes features for weather integration and plant care reminders.",
                    ProjectStatus = eProjectStatus.Pending,
                    ProjectSource = eProjectSource.NonprofitOrganization,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-15),
                    OwningOrganization = nonprofit,
                    RepositoryLink = "https://github.com/MDeLuise/plant-it",
                    Technologies = new List<string> { "Vue.js", "Python", "PostgreSQL", "Docker" },
                    ProjectPictureUrl = "https://images.unsplash.com/photo-1464226184884-fa280b87c399?q=80&w=1000",
                    Duration = TimeSpan.FromDays(90),
                    Goals = new List<string>
                    {
                        "Develop a comprehensive system for managing community gardens.",
                        "Track plant growth and provide care reminders.",
                        "Coordinate volunteer schedules and activities.",
                        "Promote sustainable urban agriculture practices."
                    },
                    RequiredRoles = new List<string> { "Full Stack Developer", "UI/UX Designer", "DevOps Engineer" },
                    RagContext = new RagContext
                    {
                        SearchableText = "Community Garden Management System - A comprehensive system for managing community gardens, tracking plant growth, and coordinating volunteer schedules with weather integration.",
                        Tags = new List<string> { "community-garden", "plant-management", "volunteer-coordination", "weather-integration", "plant-care", "sustainable-agriculture", "vuejs", "python", "postgresql", "docker" },
                        SkillLevels = new List<string> { "intermediate", "advanced" },
                        ProjectType = "web-app",
                        Domain = "environment",
                        LearningOutcomes = new List<string> { "Full-stack development", "Weather API integration", "Plant care algorithms", "Volunteer management systems" },
                        ComplexityFactors = new List<string> { "Weather data integration", "Plant growth tracking algorithms", "Volunteer scheduling optimization", "Multi-garden management" }
                    }
                };

                var project3 = new UserProject
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = "Local Business Directory",
                    ProjectDescription = "A platform for small businesses to create profiles, manage their information, and connect with local customers. Includes features for business owners to update their information and for customers to leave reviews.",
                    ProjectStatus = eProjectStatus.InProgress,
                    ProjectSource = eProjectSource.Template,
                    DifficultyLevel = eDifficultyLevel.Beginner,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                    RepositoryLink = "https://github.com/openfoodfoundation/openfoodnetwork",
                    Technologies = new List<string> { "HTML", "CSS", "JavaScript", "Firebase" },
                    ProjectPictureUrl = "https://images.unsplash.com/photo-1552664730-d307ca884978?q=80&w=1000",
                    Duration = TimeSpan.FromDays(60),
                    Goals = new List<string>
                    {
                        "Support student entrepreneurs in building their businesses.",
                        "Connect students with local business opportunities.",
                        "Provide a platform for student reviews and feedback."
                    },
                    RequiredRoles = new List<string> { "Web Developer", "UI Designer", "Content Writer" },
                    RagContext = new RagContext
                    {
                        SearchableText = "Local Business Directory - A platform for small businesses to create profiles, manage information, and connect with local customers with review and feedback systems.",
                        Tags = new List<string> { "business-directory", "local-business", "customer-reviews", "business-profiles", "local-commerce", "web-development", "html", "css", "javascript", "firebase" },
                        SkillLevels = new List<string> { "beginner" },
                        ProjectType = "web-app",
                        Domain = "business",
                        LearningOutcomes = new List<string> { "Basic web development", "User authentication", "Review systems", "Business profile management" },
                        ComplexityFactors = new List<string> { "User-generated content", "Review moderation", "Business verification", "Search and filtering" }
                    }
                };

                var project4 = new UserProject
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = "Environmental Data Tracker",
                    ProjectDescription = "An application to track and visualize environmental data such as air quality, water quality, and waste management metrics. Includes data visualization and reporting features.",
                    ProjectStatus = eProjectStatus.Pending,
                    ProjectSource = eProjectSource.Template,
                    DifficultyLevel = eDifficultyLevel.Advanced,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                    RepositoryLink = "https://github.com/openfarmcc/OpenFarm",
                    ProjectPictureUrl = "https://images.unsplash.com/photo-1497435334941-8c899ee9e8e9?q=80&w=1000",
                    Duration = TimeSpan.FromDays(120),
                    Goals = new List<string>
                    {
                        "Create a comprehensive environmental monitoring system that helps communities track and improve their environmental impact.",
                        "Enable real-time tracking and visualization of air and water quality metrics.",
                        "Provide actionable insights and reports for community leaders and organizations.",
                        "Promote environmental awareness and data-driven decision making."
                    },
                    Technologies = new List<string> { "Python", "Django", "PostgreSQL", "D3.js" },
                    RequiredRoles = new List<string> { "Full Stack Developer", "Data Scientist", "UI/UX Designer", "DevOps Engineer" },
                    RagContext = new RagContext
                    {
                        SearchableText = "Environmental Data Tracker - An application to track and visualize environmental data including air quality, water quality, and waste management metrics with data visualization and reporting.",
                        Tags = new List<string> { "environmental-data", "data-visualization", "air-quality", "water-quality", "waste-management", "data-science", "python", "django", "postgresql", "d3js", "environmental-monitoring" },
                        SkillLevels = new List<string> { "advanced" },
                        ProjectType = "data-project",
                        Domain = "environment",
                        LearningOutcomes = new List<string> { "Data visualization", "Environmental data analysis", "Real-time monitoring systems", "Reporting and analytics" },
                        ComplexityFactors = new List<string> { "Real-time data processing", "Complex data visualization", "Environmental sensor integration", "Multi-metric analysis" }
                    }
                };

                var project5 = new UserProject
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = "Community Food Bank Management System",
                    ProjectDescription = "A web application to help food banks manage inventory, track donations, and coordinate volunteers. Features include donation tracking, volunteer scheduling, and inventory management.",
                    ProjectStatus = eProjectStatus.InProgress,
                    ProjectSource = eProjectSource.Template,
                    DifficultyLevel = eDifficultyLevel.Intermediate,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                    OwningOrganization = nonprofit,
                    RepositoryLink = "https://github.com/foodbank-solutions/foodbank-manager",
                    ProjectPictureUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=1000",
                    Duration = TimeSpan.FromDays(90),
                    Goals = new List<string>
                    {
                        "Create an efficient system for managing food bank operations",
                        "Improve volunteer coordination",
                        "Enhance donation tracking capabilities",
                        "Implement real-time inventory management"
                    },
                    Technologies = new List<string> { "React", "Node.js", "MongoDB", "Express" },
                    RequiredRoles = new List<string> { "Frontend Developer", "Backend Developer", "UI/UX Designer", "Project Manager" },
                    RagContext = new RagContext
                    {
                        SearchableText = "Community Food Bank Management System - A web application to help food banks manage inventory, track donations, and coordinate volunteers with real-time inventory management.",
                        Tags = new List<string> { "food-bank", "inventory-management", "donation-tracking", "volunteer-coordination", "community-service", "web-application", "react", "nodejs", "mongodb", "express", "real-time" },
                        SkillLevels = new List<string> { "intermediate" },
                        ProjectType = "web-app",
                        Domain = "social-impact",
                        LearningOutcomes = new List<string> { "Full-stack development", "Real-time systems", "Inventory management", "Volunteer coordination" },
                        ComplexityFactors = new List<string> { "Real-time inventory updates", "Multi-user coordination", "Donation tracking workflows", "Reporting and analytics" }
                    }
                };

                // Add ProjectMembers to each project
                project1.ProjectMembers = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        UserId = gainer1.UserId,
                        User = gainer1,
                        UserRole = "Web Developer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-30),
                        Project = project1
                    },
                    new ProjectMember
                    {
                        UserId = gainer2.UserId,
                        User = gainer2,
                        UserRole = "UI Designer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-30),
                        Project = project1
                    },
                    new ProjectMember
                    {
                        UserId = mentor.UserId,
                        User = mentor,
                        UserRole = "Mentor",
                        IsAdmin = true,
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-30),
                        Project = project1
                    }
                };

                gainer1.ParticipatedProjects.Add(project1);
                gainer2.ParticipatedProjects.Add(project1);
                mentor.MentoredProjects.Add(project1);


                project2.ProjectMembers = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        UserId = gainer3.UserId,
                        User = gainer3,
                        UserRole = "Full Stack Developer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-15),
                        Project = project2
                    },
                    new ProjectMember
                    {
                        UserId = gainer4.UserId,
                        User = gainer4,
                        UserRole = "UI/UX Designer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-15),
                        Project = project2
                    },
                    new ProjectMember
                    {
                        UserId = gainer5.UserId,
                        User = gainer5,
                        UserRole = "DevOps Engineer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-15),
                        Project = project2
                    },
                    new ProjectMember
                    {
                        UserId = mentor2.UserId,
                        User = mentor2,
                        UserRole = "Mentor",
                        IsAdmin = true,
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-15),
                        Project = project2
                    }
                };

                gainer3.ParticipatedProjects.Add(project2);
                gainer4.ParticipatedProjects.Add(project2);
                gainer5.ParticipatedProjects.Add(project2);
                mentor2.MentoredProjects.Add(project2);

                project3.ProjectMembers = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        UserId = gainer6.UserId,
                        User = gainer6,
                        UserRole = "Web Developer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-10),
                        Project = project3
                    },
                    new ProjectMember
                    {
                        UserId = gainer1.UserId,
                        User = gainer1,
                        UserRole = "UI Designer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-10),
                        Project = project3
                    }
                };

                gainer6.ParticipatedProjects.Add(project3);
                gainer1.ParticipatedProjects.Add(project3);

                project4.ProjectMembers = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        UserId = gainer1.UserId,
                        User = gainer1,
                        UserRole = "Full Stack Developer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-5),
                        Project = project4
                    },
                    new ProjectMember
                    {
                        UserId = gainer2.UserId,
                        User = gainer2,
                        UserRole = "Data Scientist",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-5),
                        Project = project4
                    },
                    new ProjectMember
                    {
                        UserId = gainer3.UserId,
                        User = gainer3,
                        UserRole = "UI/UX Designer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-5),
                        Project = project4
                    },
                    new ProjectMember
                    {
                        UserId = mentor.UserId,
                        User = mentor,
                        UserRole = "Mentor",
                        IsAdmin = true,
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-5),
                        Project = project4
                    }
                };

                gainer1.ParticipatedProjects.Add(project4);
                gainer2.ParticipatedProjects.Add(project4);
                gainer3.ParticipatedProjects.Add(project4);
                mentor.MentoredProjects.Add(project4);


                project5.ProjectMembers = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        UserId = gainer4.UserId,
                        User = gainer4,
                        UserRole = "Frontend Developer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-2),
                        Project = project5
                    },
                    new ProjectMember
                    {
                        UserId = gainer5.UserId,
                        User = gainer5,
                        UserRole = "Backend Developer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-2),
                        Project = project5
                    },
                    new ProjectMember
                    {
                        UserId = gainer6.UserId,
                        User = gainer6,
                        UserRole = "UI/UX Designer",
                        JoinedAtUtc = DateTime.UtcNow.AddDays(-2),
                        Project = project5
                    }
                };

                gainer4.ParticipatedProjects.Add(project5);
                gainer5.ParticipatedProjects.Add(project5);
                gainer6.ParticipatedProjects.Add(project5);


                // Add all projects to the list
                var seededProjects = new List<UserProject> { project1, project2, project3, project4, project5 };
                
                // Add nonprofit projects if they exist
                if (nonprofitSuggestions.Any())
                {
                                         var nonprofitProjects = nonprofitSuggestions.Select(nps => new UserProject
                     {
                         ProjectId = Guid.NewGuid(), // Generate new GUID for nonprofit projects
                         ProjectName = nps.ProjectName,
                         ProjectDescription = nps.ProjectDescription,
                         ProjectStatus = eProjectStatus.Pending,
                         ProjectSource = eProjectSource.NonprofitOrganization,
                         CreatedAtUtc = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                         RepositoryLink = nps.RepositoryLink,
                         ProjectPictureUrl = nps.ProjectPictureUrl,
                         Duration = TimeSpan.FromDays(nps.DurationDays),
                         Goals = nps.Goals,
                         Technologies = nps.Technologies,
                         RequiredRoles = nps.RequiredRoles,
                         ProgrammingLanguages = nps.ProgrammingLanguages,
                         RagContext = nps.RagContext != null ? new RagContext
                         {
                             SearchableText = nps.RagContext.SearchableText,
                             Tags = nps.RagContext.Tags,
                             SkillLevels = nps.RagContext.SkillLevels,
                             ProjectType = nps.RagContext.ProjectType,
                             Domain = nps.RagContext.Domain,
                             LearningOutcomes = nps.RagContext.LearningOutcomes,
                             ComplexityFactors = nps.RagContext.ComplexityFactors
                         } : null
                     }).ToList();     

                    // Assign nonprofit organizations to projects
                    for (int i = 0; i < nonprofitProjects.Count; i++)
                    {
                        var nonprofitOrg = i == 0 ? nonprofit2 : nonprofit3;
                        nonprofitProjects[i].OwningOrganization = nonprofitOrg;
                    }

                    seededProjects.AddRange(nonprofitProjects);
                }
                
                context.Projects.AddRange(seededProjects);
                context.SaveChanges();
                #endregion

                #region Seed Achievements
                var achievementTemplates = new List<AchievementTemplate>
                {
                    new AchievementTemplate
                    {
                        Id = Guid.NewGuid(),
                        Title = "First Project Complete",
                        Description = "Successfully completed your first project",
                        IconUrl = "/achievements/first-project.png",
                        UnlockCriteria = "Complete a project with status 'Completed'",
                        Category = "Project Completion"
                    },
                    new AchievementTemplate
                    {
                        Id = Guid.NewGuid(),
                        Title = "Team Player",
                        Description = "Participated in 5 different projects",
                        IconUrl = "/achievements/team-player.png",
                        UnlockCriteria = "Be a team member in 5 different projects",
                        Category = "Collaboration"
                    },
                    new AchievementTemplate
                    {
                        Id = Guid.NewGuid(),
                        Title = "Mentor's Choice",
                        Description = "Received positive feedback from a mentor",
                        IconUrl = "/achievements/mentor-choice.png",
                        UnlockCriteria = "Receive positive feedback from a project mentor",
                        Category = "Recognition"
                    },
                    new AchievementTemplate
                    {
                        Id = Guid.NewGuid(),
                        Title = "Expert Mentor",
                        Description = "Successfully mentored 3 different projects",
                        IconUrl = "/achievements/expert-mentor.png",
                        UnlockCriteria = "Mentor 3 different projects to completion",
                        Category = "Leadership"
                    },
                    new AchievementTemplate
                    {
                        Id = Guid.NewGuid(),
                        Title = "Innovation Leader",
                        Description = "Led a project that introduced new technologies or approaches",
                        IconUrl = "/achievements/innovation-leader.png",
                        UnlockCriteria = "Lead a project that successfully implements innovative solutions",
                        Category = "Innovation"
                    },
                    new AchievementTemplate
                    {
                        Id = Guid.NewGuid(),
                        Title = "Community Builder",
                        Description = "Successfully organized and led community-focused projects",
                        IconUrl = "/achievements/community-builder.png",
                        UnlockCriteria = "Lead 2 community-focused projects",
                        Category = "Community"
                    },
                    new AchievementTemplate
                    {
                        Id = Guid.NewGuid(),
                        Title = "Tech Pioneer",
                        Description = "Mastered cutting-edge technologies in project development",
                        IconUrl = "/achievements/tech-pioneer.png",
                        UnlockCriteria = "Use advanced technologies in 3 different projects",
                        Category = "Technology"
                    },
                    new AchievementTemplate
                    {
                        Id = Guid.NewGuid(),
                        Title = "Social Impact Champion",
                        Description = "Contributed to projects with significant social impact",
                        IconUrl = "/achievements/social-impact.png",
                        UnlockCriteria = "Participate in 3 nonprofit or social impact projects",
                        Category = "Social Impact"
                    }
                };

                context.AchievementTemplates.AddRange(achievementTemplates);
                context.SaveChanges();

                // Add achievements to users
                var userAchievements = new List<UserAchievement>
                {
                    // Gainer1 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer1.UserId,
                        User = gainer1,
                        AchievementTemplateId = achievementTemplates[0].Id,
                        AchievementTemplate = achievementTemplates[0],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-20),
                        EarnedDetails = "Successfully completed the TechForGood Learning Platform project with all features implemented"
                    },
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer1.UserId,
                        User = gainer1,
                        AchievementTemplateId = achievementTemplates[1].Id,
                        AchievementTemplate = achievementTemplates[1],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-15),
                        EarnedDetails = "Actively participated in 5 different projects as a team member"
                    },
                    // Gainer2 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer2.UserId,
                        User = gainer2,
                        AchievementTemplateId = achievementTemplates[0].Id,
                        AchievementTemplate = achievementTemplates[0],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-25),
                        EarnedDetails = "Successfully completed the TechForGood Learning Platform project with all features implemented"
                    },
                    // Mentor achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = mentor.UserId,
                        User = mentor,
                        AchievementTemplateId = achievementTemplates[2].Id,
                        AchievementTemplate = achievementTemplates[2],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-10),
                        EarnedDetails = "Received positive feedback from 3 different project teams for excellent mentorship"
                    },
                    // Gainer3 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer3.UserId,
                        User = gainer3,
                        AchievementTemplateId = achievementTemplates[0].Id,
                        AchievementTemplate = achievementTemplates[0],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-5),
                        EarnedDetails = "Successfully completed the Community Garden Management System project with all features implemented"
                    },
                    // Gainer4 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer4.UserId,
                        User = gainer4,
                        AchievementTemplateId = achievementTemplates[1].Id,
                        AchievementTemplate = achievementTemplates[1],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-8),
                        EarnedDetails = "Actively participated in 5 different projects as a team member"
                    },
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer4.UserId,
                        User = gainer4,
                        AchievementTemplateId = achievementTemplates[6].Id,
                        AchievementTemplate = achievementTemplates[6],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-1),
                        EarnedDetails = "Used advanced React and Node.js technologies in 3 different projects"
                    },
                    // Gainer5 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer5.UserId,
                        User = gainer5,
                        AchievementTemplateId = achievementTemplates[0].Id,
                        AchievementTemplate = achievementTemplates[0],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-6),
                        EarnedDetails = "Successfully completed the Mobile App Development project with all features implemented"
                    },
                    // Gainer6 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer6.UserId,
                        User = gainer6,
                        AchievementTemplateId = achievementTemplates[0].Id,
                        AchievementTemplate = achievementTemplates[0],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-4),
                        EarnedDetails = "Successfully completed the Data Visualization Dashboard project with all features implemented"
                    },
                    // Mentor3 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = mentor3.UserId,
                        User = mentor3,
                        AchievementTemplateId = achievementTemplates[2].Id,
                        AchievementTemplate = achievementTemplates[2],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-12),
                        EarnedDetails = "Received positive feedback from 2 different project teams for excellent mentorship"
                    },
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = mentor3.UserId,
                        User = mentor3,
                        AchievementTemplateId = achievementTemplates[3].Id,
                        AchievementTemplate = achievementTemplates[3],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-5),
                        EarnedDetails = "Successfully mentored 3 different projects to completion"
                    },
                    // Mentor4 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = mentor4.UserId,
                        User = mentor4,
                        AchievementTemplateId = achievementTemplates[2].Id,
                        AchievementTemplate = achievementTemplates[2],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-15),
                        EarnedDetails = "Received positive feedback from 3 different project teams for excellent mentorship"
                    },
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = mentor4.UserId,
                        User = mentor4,
                        AchievementTemplateId = achievementTemplates[4].Id,
                        AchievementTemplate = achievementTemplates[4],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-8),
                        EarnedDetails = "Led a project that successfully implemented innovative AI-powered solutions"
                    },
                    // Gainer7 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer7.UserId,
                        User = gainer7,
                        AchievementTemplateId = achievementTemplates[0].Id,
                        AchievementTemplate = achievementTemplates[0],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-10),
                        EarnedDetails = "Successfully completed the AI-Powered Learning Analytics project with all features implemented"
                    },
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer7.UserId,
                        User = gainer7,
                        AchievementTemplateId = achievementTemplates[6].Id,
                        AchievementTemplate = achievementTemplates[6],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-3),
                        EarnedDetails = "Used advanced AI and machine learning technologies in 3 different projects"
                    },
                    // Gainer8 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer8.UserId,
                        User = gainer8,
                        AchievementTemplateId = achievementTemplates[0].Id,
                        AchievementTemplate = achievementTemplates[0],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-7),
                        EarnedDetails = "Successfully completed the Sustainable Energy Monitoring project with all features implemented"
                    },
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = gainer8.UserId,
                        User = gainer8,
                        AchievementTemplateId = achievementTemplates[1].Id,
                        AchievementTemplate = achievementTemplates[1],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-2),
                        EarnedDetails = "Actively participated in 5 different projects as a team member"
                    },
                    // Nonprofit2 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = nonprofit2.UserId,
                        User = nonprofit2,
                        AchievementTemplateId = achievementTemplates[5].Id,
                        AchievementTemplate = achievementTemplates[5],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-20),
                        EarnedDetails = "Successfully organized and led 2 community-focused environmental projects"
                    },
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = nonprofit2.UserId,
                        User = nonprofit2,
                        AchievementTemplateId = achievementTemplates[7].Id,
                        AchievementTemplate = achievementTemplates[7],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-15),
                        EarnedDetails = "Contributed to 3 nonprofit projects with significant environmental impact"
                    },
                    // Nonprofit3 achievements
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = nonprofit3.UserId,
                        User = nonprofit3,
                        AchievementTemplateId = achievementTemplates[5].Id,
                        AchievementTemplate = achievementTemplates[5],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-18),
                        EarnedDetails = "Successfully organized and led 2 community-focused healthcare projects"
                    },
                    new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = nonprofit3.UserId,
                        User = nonprofit3,
                        AchievementTemplateId = achievementTemplates[7].Id,
                        AchievementTemplate = achievementTemplates[7],
                        EarnedAtUtc = DateTime.UtcNow.AddDays(-12),
                        EarnedDetails = "Contributed to 3 nonprofit projects with significant healthcare impact"
                    }
                };

                // Add achievements to users' collections
                gainer1.Achievements = userAchievements.Where(a => a.UserId == gainer1.UserId).ToList();
                gainer2.Achievements = userAchievements.Where(a => a.UserId == gainer2.UserId).ToList();
                gainer3.Achievements = userAchievements.Where(a => a.UserId == gainer3.UserId).ToList();
                gainer4.Achievements = userAchievements.Where(a => a.UserId == gainer4.UserId).ToList();
                mentor.Achievements = userAchievements.Where(a => a.UserId == mentor.UserId).ToList();
                mentor3.Achievements = userAchievements.Where(a => a.UserId == mentor3.UserId).ToList();
                mentor4.Achievements = userAchievements.Where(a => a.UserId == mentor4.UserId).ToList();
                gainer7.Achievements = userAchievements.Where(a => a.UserId == gainer7.UserId).ToList();
                gainer8.Achievements = userAchievements.Where(a => a.UserId == gainer8.UserId).ToList();
                nonprofit2.Achievements = userAchievements.Where(a => a.UserId == nonprofit2.UserId).ToList();
                nonprofit3.Achievements = userAchievements.Where(a => a.UserId == nonprofit3.UserId).ToList();

                // Add achievements to users' collections
                foreach (var achievement in userAchievements)
                {
                    achievement.User.Achievements.Add(achievement);
                }

                context.UserAchievements.AddRange(userAchievements);
                context.SaveChanges();
                #endregion
            }
            else{
                logger?.LogInformation("Database already seeded");
            }
        }

        /// <summary>
        /// Helper method to parse difficulty level string to enum
        /// </summary>
        private static eDifficultyLevel ParseDifficultyLevel(string difficultyLevel)
        {
            return difficultyLevel.ToLower() switch
            {
                "beginner" => eDifficultyLevel.Beginner,
                "intermediate" => eDifficultyLevel.Intermediate,
                "advanced" => eDifficultyLevel.Advanced,
                _ => eDifficultyLevel.Beginner
            };
        }
    }
}

