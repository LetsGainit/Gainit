using GainIt.API.Models.Projects;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Nonprofits;
using Microsoft.EntityFrameworkCore;
using GainIt.API.Models.Enums.Projects;

namespace GainIt.API.Data
{
    public class GainItDbContext : DbContext
    {
        public GainItDbContext(DbContextOptions<GainItDbContext> i_Options) : base(i_Options)
        {
        }
        // DbSet properties for your entities go here
        // public DbSet<Project> Projects { get; set; }
        // public DbSet<Mentor> Mentors { get; set; }
        // Add other DbSets as needed

        public DbSet<User> Users { get; set; }
        public DbSet<Gainer> Gainers { get; set; }
        public DbSet<Mentor> Mentors { get; set; }
        public DbSet<NonprofitOrganization> Nonprofits { get; set; }
        public DbSet<UserProject> Projects { get; set; }
        public DbSet<TemplateProject> TemplateProjects { get; set; }

        protected override void OnModelCreating(ModelBuilder i_ModelBuilder)
        {
            // Use TPT for inheritance
            i_ModelBuilder.Entity<User>().UseTptMappingStrategy();

            // Many-to-many: Projects ↔ TeamMembers (Gainers)
            i_ModelBuilder.Entity<UserProject>()
                .HasMany(p => p.TeamMembers)
                .WithMany(g => g.ParticipatedProjects)
                .UsingEntity(j => j.ToTable("ProjectTeamMembers"));

            // One mentor → many projects
            i_ModelBuilder.Entity<UserProject>()
                .HasOne<Mentor>(p => p.AssignedMentor)
                .WithMany(m => m.MentoredProjects) 
                .HasForeignKey("AssignedMentorUserId")
                .OnDelete(DeleteBehavior.SetNull);

            // One nonprofit → many projects
            i_ModelBuilder.Entity<UserProject>()
                .HasOne<NonprofitOrganization>(p => p.OwningOrganization)
                .WithMany(n => n.OwnedProjects) 
                .HasForeignKey("OwningOrganizationUserId")
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(i_ModelBuilder);
        }


    }
    public static class GainItDbContextSeeder
    {
        public static void SeedData(GainItDbContext context)
        {
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new Gainer
                    {
                        UserId = Guid.NewGuid(),
                        FullName = "John Doe",
                        EmailAddress = "john.doe@example.com",
                        EducationStatus = "Undergraduate",
                        AreasOfInterest = new List<string> { "Technology", "Education" }
                    },
                    new Mentor
                    {
                        UserId = Guid.NewGuid(),
                        FullName = "Jane Smith",
                        EmailAddress = "jane.smith@example.com",
                        YearsOfExperience = 10,
                        AreaOfExpertise = "Software Development"
                    },
                    new NonprofitOrganization
                    {
                        UserId = Guid.NewGuid(),
                        FullName = "Helping Hands",
                        EmailAddress = "contact@helpinghands.org",
                        WebsiteUrl = "https://helpinghands.org"
                    }
                );
                context.SaveChanges();
            }

            if (!context.TemplateProjects.Any())
            {
                context.TemplateProjects.Add(new TemplateProject
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = "Starter Template",
                    ProjectDescription = "A base template for beginner projects.",
                    DifficultyLevel = eDifficultyLevel.Beginner
                });

                context.SaveChanges();
            }

            if (!context.Projects.Any())
            {
                // Seed a template project
                context.Projects.Add(new UserProject
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = "Nonprofit Project",
                    ProjectDescription = "This is a nonprofit project for testing purposes.",
                    ProjectStatus = eProjectStatus.Pending,
                    ProjectSource = eProjectSource.NonprofitOrganization,
                    CreatedAtUtc = DateTime.UtcNow,
                    TeamMembers = new List<Gainer>(),
                    OwningOrganization = context.Users.OfType<NonprofitOrganization>().FirstOrDefault(),
                    OwningOrganizationUserId = context.Users.OfType<NonprofitOrganization>().FirstOrDefault()?.UserId,
                    RepositoryLink = "https://github.com/example/template-repo"
                });

                context.SaveChanges();
            }
        }

        
    }

}
