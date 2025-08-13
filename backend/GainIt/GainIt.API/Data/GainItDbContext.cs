using GainIt.API.Models.Projects;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Nonprofits;
using Microsoft.EntityFrameworkCore;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Users.Expertise;
using Microsoft.Extensions.Logging;

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
                #region Inheritance Configuration
                // Configure TPT inheritance for User hierarchy
                modelBuilder.Entity<User>().UseTptMappingStrategy();

                // Configure TPC inheritance for Project hierarchy
                modelBuilder.Entity<TemplateProject>().UseTpcMappingStrategy();

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
                // Configure UserProject
                modelBuilder.Entity<UserProject>(entity =>
                {
                  
                    

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
                modelBuilder.Entity<TemplateProject>(entity =>
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
                modelBuilder.Entity<TechExpertise>(entity =>
                {
                    entity.HasOne(e => e.User)
                        .WithOne()
                        .HasForeignKey<TechExpertise>("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure NonprofitExpertise
                modelBuilder.Entity<NonprofitExpertise>(entity =>
                {
                    entity.HasOne(e => e.User)
                        .WithOne()
                        .HasForeignKey<NonprofitExpertise>("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
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
        public static void SeedData(GainItDbContext context, ILogger? logger = null)
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
                    ProfilePictureURL = "https://randomuser.me/api/portraits/men/66.jpg",
                    LinkedInURL = "https://linkedin.com/company/tech-career-mentorship",
                    FacebookPageURL = "https://facebook.com/TechCareerMentor",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow,
                    Country = "Israel",
                    Achievements = new List<UserAchievement>()
                };

                // Create a nonprofit organization
                var nonprofit = new NonprofitOrganization
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "TechForGood Israel",
                    EmailAddress = "contact@techforgood.co.il",
                    WebsiteUrl = "https://techforgood.co.il",
                    Biography = "Empowering Israeli communities through technology education and digital literacy programs.",
                    GitHubURL = "https://github.com/techforgood-israel",
                    ProfilePictureURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQHIVTRK4hstoI6IBdXmslzxY96ql1mZQF6wg&s",
                    LinkedInURL = "https://linkedin.com/company/techforgood-israel",
                    FacebookPageURL = "https://facebook.com/TechForGoodIsrael",
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

                mentor.TechExpertise = mentorTechExpertise;
                mentor2.TechExpertise = mentor2TechExpertise;
                context.TechExpertises.AddRange(mentorTechExpertise, mentor2TechExpertise);

                // Create some gainers
                var gainer1 = new Gainer
                {
                    UserId = Guid.NewGuid(),
                    ExternalId = Guid.NewGuid().ToString(),
                    FullName = "Yossi Rosenberg",
                    EmailAddress = "yossi.rosenberg@techlearner.dev",
                    EducationStatus = "Undergraduate",
                    AreasOfInterest = new List<string> { "Web Development", "UI/UX Design", "Cloud Computing" },
                    GitHubURL = "https://github.com/yossirosenberg",
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
                    LinkedInURL = "https://linkedin.com/in/liorabarak",
                    FacebookPageURL = "https://facebook.com/liora.barak",
                    ProfilePictureURL = "https://randomuser.me/api/portraits/women/77.jpg",
                    Biography = "AI graduate with a focus on natural language processing and Python, dedicated to advancing intelligent systems.",
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

                // Add TechExpertise entries to context
                context.TechExpertises.AddRange(techExpertise1, techExpertise2, techExpertise3, techExpertise4, techExpertise5, techExpertise6);

                // Link TechExpertise to Gainers
                gainer1.TechExpertise = techExpertise1;
                gainer2.TechExpertise = techExpertise2;
                gainer3.TechExpertise = techExpertise3;
                gainer4.TechExpertise = techExpertise4;
                gainer5.TechExpertise = techExpertise5;
                gainer6.TechExpertise = techExpertise6;

                // Add all users to context
                context.Users.AddRange(mentor, mentor2, nonprofit, gainer1, gainer2, gainer3, gainer4, gainer5, gainer6);
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
                        Goals = new List<string>
                        {
                            "Help local businesses establish an online presence and connect with their community through a user-friendly platform.",
                            "Enable customers to easily discover and review local businesses.",
                            "Provide business owners with tools to manage their profiles and engage with customers.",
                            "Foster a supportive local business ecosystem."
                        },
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
                        Goals = new List<string>
                        {
                            "Create a comprehensive environmental monitoring system that helps communities track and improve their environmental impact.",
                            "Enable real-time tracking and visualization of air and water quality metrics.",
                            "Provide actionable insights and reports for community leaders and organizations.",
                            "Promote environmental awareness and data-driven decision making."
                        },
                        Technologies = new List<string> { "Python", "Django", "PostgreSQL", "D3.js" },
                        RequiredRoles = new List<string> { "Full Stack Developer", "Data Scientist", "UI/UX Designer", "DevOps Engineer" }
                    }
                };

                context.TemplateProjects.AddRange(templateProjects);
                context.SaveChanges();

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
                    ProgrammingLanguages = new List<string> { "JavaScript", "HTML", "CSS" }
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
                    RequiredRoles = new List<string> { "Full Stack Developer", "UI/UX Designer", "DevOps Engineer" }
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
                    RepositoryLink = "https://github.com/learnhouse/learnhouse",
                    Technologies = new List<string> { "HTML", "CSS", "JavaScript", "Firebase" },
                    ProjectPictureUrl = "https://images.unsplash.com/photo-1552664730-d307ca884978?q=80&w=1000",
                    Duration = TimeSpan.FromDays(60),
                    Goals = new List<string>
                    {
                        "Support student entrepreneurs in building their businesses.",
                        "Connect students with local business opportunities.",
                        "Provide a platform for student reviews and feedback."
                    },
                    RequiredRoles = new List<string> { "Web Developer", "UI Designer", "Content Writer" }
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
                    RequiredRoles = new List<string> { "Full Stack Developer", "Data Scientist", "UI/UX Designer", "DevOps Engineer" }
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
                    RequiredRoles = new List<string> { "Frontend Developer", "Backend Developer", "UI/UX Designer", "Project Manager" }
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
                    }
                };

                // Add achievements to users' collections
                gainer1.Achievements = userAchievements.Where(a => a.UserId == gainer1.UserId).ToList();
                gainer2.Achievements = userAchievements.Where(a => a.UserId == gainer2.UserId).ToList();
                gainer3.Achievements = userAchievements.Where(a => a.UserId == gainer3.UserId).ToList();
                gainer4.Achievements = userAchievements.Where(a => a.UserId == gainer4.UserId).ToList();
                mentor.Achievements = userAchievements.Where(a => a.UserId == mentor.UserId).ToList();

                // Add achievements to users' collections
                foreach (var achievement in userAchievements)
                {
                    achievement.User.Achievements.Add(achievement);
                }

                context.UserAchievements.AddRange(userAchievements);
                context.SaveChanges();
                #endregion
            }
        }
    }
}

