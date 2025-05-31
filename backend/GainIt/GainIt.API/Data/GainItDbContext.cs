using GainIt.API.Models.Projects;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Nonprofits;
using Microsoft.EntityFrameworkCore;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Users.Expertise;

namespace GainIt.API.Data
{
    public class GainItDbContext : DbContext
    {
        public GainItDbContext(DbContextOptions<GainItDbContext> i_Options) : base(i_Options)
        {
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
        /// Uses TPC (Table Per Concrete Type) inheritance strategy
        /// </summary>
        public DbSet<UserProject> Projects { get; set; }

        /// <summary>
        /// Template projects table - project templates that can be used as a base
        /// Uses TPC (Table Per Concrete Type) inheritance strategy
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
        #endregion

        protected override void OnModelCreating(ModelBuilder i_ModelBuilder)
        {

            #region Inheritance Configuration
            // Configure TPT inheritance for User hierarchy
            i_ModelBuilder.Entity<User>().UseTptMappingStrategy();

            // Configure TPC inheritance for Project hierarchy
            i_ModelBuilder.Entity<TemplateProject>().UseTpcMappingStrategy();

            // Configure TPT inheritance for UserExpertise hierarchy
            i_ModelBuilder.Entity<UserExpertise>().UseTptMappingStrategy();
            #endregion

            #region User Configuration
            // Configure User entity
            i_ModelBuilder.Entity<User>(entity =>
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
            // Configure UserProject
            i_ModelBuilder.Entity<UserProject>(entity =>
            {
                // Many-to-many: Projects ↔ TeamMembers (Gainers)
                entity.HasMany(p => p.TeamMembers)
                    .WithMany(g => g.ParticipatedProjects)
                    .UsingEntity(j => j.ToTable("ProjectTeamMembers"));

                // One mentor → many projects
                entity.HasOne<Mentor>(p => p.AssignedMentor)
                    .WithMany(m => m.MentoredProjects)
                    .HasForeignKey("AssignedMentorUserId")
                    .OnDelete(DeleteBehavior.SetNull);

                // One nonprofit → many projects
                entity.HasOne<NonprofitOrganization>(p => p.OwningOrganization)
                    .WithMany(n => n.OwnedProjects)
                    .HasForeignKey("OwningOrganizationUserId")
                    .OnDelete(DeleteBehavior.SetNull);

                // Configure required fields
                entity.Property(e => e.ProjectName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ProjectDescription)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            // Configure TemplateProject
            i_ModelBuilder.Entity<TemplateProject>(entity =>
            {
                entity.Property(e => e.ProjectName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ProjectDescription)
                    .IsRequired()
                    .HasMaxLength(1000);
            });
            #endregion

            #region Expertise Configuration
            // Configure TechExpertise
            i_ModelBuilder.Entity<TechExpertise>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithOne()
                    .HasForeignKey<TechExpertise>("UserId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure NonprofitExpertise
            i_ModelBuilder.Entity<NonprofitExpertise>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithOne()
                    .HasForeignKey<NonprofitExpertise>("UserId")
                    .OnDelete(DeleteBehavior.Cascade);
            });
            #endregion

            #region Achievement Configuration
            // Configure UserAchievement
            i_ModelBuilder.Entity<UserAchievement>(entity =>
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
            i_ModelBuilder.Entity<AchievementTemplate>(entity =>
            {
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.IconUrl)
                    .HasMaxLength(200);

                entity.Property(e => e.PictureUrl)
                    .HasMaxLength(200);

                entity.Property(e => e.UnlockCriteria)
                    .IsRequired()
                    .HasMaxLength(500);
            });
            #endregion


            #region Project Member Configuration
            // Configure ProjectMember entity
            i_ModelBuilder.Entity<ProjectMember>(entity =>
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

            base.OnModelCreating(i_ModelBuilder);
        }
    }


    public static class GainItDbContextSeeder
    {
        public static void SeedData(GainItDbContext context)
        {
            // Only seed if database is empty
            if (!context.Users.Any())
            {
                #region Seed Users
                // Create a mentor
                var mentor = new Mentor
                {
                    UserId = Guid.NewGuid(),
                    FullName = "Dr. Sarah Chen",
                    EmailAddress = "sarah.chen@techmentor.com",
                    YearsOfExperience = 15,
                    AreaOfExpertise = "Full Stack Development",
                    Biography = "Senior software architect with expertise in cloud technologies and microservices."
                };

                // Create a nonprofit organization
                var nonprofit = new NonprofitOrganization
                {
                    UserId = Guid.NewGuid(),
                    FullName = "TechForGood Foundation",
                    EmailAddress = "contact@techforgood.org",
                    WebsiteUrl = "https://techforgood.org",
                    Biography = "Empowering communities through technology education and digital literacy programs."
                };

                // Create some gainers
                var gainer1 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    FullName = "Alex Johnson",
                    EmailAddress = "alex.j@student.edu",
                    EducationStatus = "Undergraduate",
                    AreasOfInterest = new List<string> { "Web Development", "UI/UX Design", "Cloud Computing" }
                };

                var gainer2 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    FullName = "Maria Rodriguez",
                    EmailAddress = "maria.r@student.edu",
                    EducationStatus = "Graduate",
                    AreasOfInterest = new List<string> { "Machine Learning", "Data Science", "Python" }
                };

                context.Users.AddRange(mentor, nonprofit, gainer1, gainer2);
                context.SaveChanges();
                #endregion

                #region Seed Template Projects
                var templateProjects = new List<TemplateProject>
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
    Technologies = new List<string>
  {
 "React",
"Node.js",
  "MongoDB",
     "Express"
 },





                        Technologies = new List<string> { "React", "Node.js", "MongoDB", "Express" },
                        RequiredRoles = new List<string> { "Frontend Developer", "Backend Developer", "UI/UX Designer", "Project Manager" }
                    },
                    new TemplateProject
                    {
                        ProjectId = Guid.NewGuid(),
                        ProjectName = "Local Business Directory",
                        ProjectDescription = "A platform for small businesses to create profiles, manage their information, and connect with local customers. Includes features for business owners to update their information and for customers to leave reviews.",
                        DifficultyLevel = eDifficultyLevel.Beginner,
                        ProjectPictureUrl = "https://images.unsplash.com/photo-1552664730-d307ca884978?q=80&w=1000",
                        Duration = TimeSpan.FromDays(60),
                        Goals = "Help local businesses establish an online presence and connect with their community through a user-friendly platform.",
                        Technologies = new List<string> { "HTML", "CSS", "JavaScript", "Firebase" },
                        RequiredRoles = new List<string> { "Web Developer", "UI Designer", "Content Writer" }
                    },
                    new TemplateProject
                    {
                        ProjectId = Guid.NewGuid(),
                        ProjectName = "Environmental Data Tracker",
                        ProjectDescription = "An application to track and visualize environmental data such as air quality, water quality, and waste management metrics. Includes data visualization and reporting features.",
                        DifficultyLevel = eDifficultyLevel.Advanced,
                        ProjectPictureUrl = "https://images.unsplash.com/photo-1497435334941-8c899ee9e8e9?q=80&w=1000",
                        Duration = TimeSpan.FromDays(120),
                        Goals = "Create a comprehensive environmental monitoring system that helps communities track and improve their environmental impact.",
                        Technologies = new List<string> { "Python", "Django", "PostgreSQL", "D3.js" },
                        RequiredRoles = new List<string> { "Full Stack Developer", "Data Scientist", "UI/UX Designer", "DevOps Engineer" }
                    }
                };

                context.TemplateProjects.AddRange(templateProjects);
                context.SaveChanges();
                #endregion

                #region Seed User Projects
                var userProjects = new List<UserProject>
                {
                    new UserProject
                    {
                        ProjectId = Guid.NewGuid(),
                        ProjectName = "TechForGood Learning Platform",
                        ProjectDescription = "An online learning platform for TechForGood Foundation to provide free coding courses to underprivileged communities. Features include course management, progress tracking, and interactive coding exercises.",
                        ProjectStatus = eProjectStatus.InProgress,
                        ProjectSource = eProjectSource.NonprofitOrganization,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
                        TeamMembers = new List<Gainer> { gainer1, gainer2 },
                        AssignedMentor = mentor,
                        OwningOrganization = nonprofit,
                        RepositoryLink = "https://github.com/techforgood/learning-platform",
                        Technologies = new List<string> { "React", "TypeScript", "Node.js", "MongoDB" },
                        ProjectPictureUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?q=80&w=1000",
                        Duration = TimeSpan.FromDays(180),
                        Goals = "Create an accessible learning platform that empowers underprivileged communities with coding skills and technology education.",
                        RequiredRoles = new List<string> { "Frontend Developer", "Backend Developer", "UI/UX Designer", "Content Creator" }
                    },
                    new UserProject
                    {
                        ProjectId = Guid.NewGuid(),
                        ProjectName = "Community Garden Management System",
                        ProjectDescription = "A system to help community gardens manage plots, track plant growth, and coordinate volunteer schedules. Includes features for weather integration and plant care reminders.",
                        ProjectStatus = eProjectStatus.Pending,
                        ProjectSource = eProjectSource.NonprofitOrganization,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-15),
                        TeamMembers = new List<Gainer> { gainer1 },
                        AssignedMentor = mentor,
                        OwningOrganization = nonprofit,
                        RepositoryLink = "https://github.com/techforgood/garden-management",
                        Technologies = new List<string> { "Vue.js", "Python", "PostgreSQL", "Docker" },
                        ProjectPictureUrl = "https://images.unsplash.com/photo-1464226184884-fa280b87c399?q=80&w=1000",
                        Duration = TimeSpan.FromDays(90),
                        Goals = "Develop a comprehensive system for managing community gardens and promoting sustainable urban agriculture.",
                        RequiredRoles = new List<string> { "Full Stack Developer", "UI/UX Designer", "DevOps Engineer" }
                    }
                };

                context.Projects.AddRange(userProjects);
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
                }
            };

                context.AchievementTemplates.AddRange(achievementTemplates);
                context.SaveChanges();
                #endregion
            }
        }
    }
}

    