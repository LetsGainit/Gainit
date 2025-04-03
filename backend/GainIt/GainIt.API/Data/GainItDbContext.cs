using GainIt.API.Models.Projects;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Nonprofits;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder i_ModelBuilder)
        {
            // Use TPT strategy: each type has its own table (clean, scalable)
            i_ModelBuilder.Entity<User>().UseTptMappingStrategy();

            base.OnModelCreating(i_ModelBuilder); // Keep EF Core default behavior
        }

    }
    
}
