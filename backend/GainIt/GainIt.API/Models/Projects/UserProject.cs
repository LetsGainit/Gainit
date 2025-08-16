using GainIt.API.Models.Enums.Projects;
using System.ComponentModel.DataAnnotations;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Projects
{
    public class UserProject : TemplateProject
    {
        [Required]
        public eProjectStatus ProjectStatus { get; set; }

        [Required]
        public eProjectSource ProjectSource { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Url(ErrorMessage = "Invalid Repository URL")]
        public string? RepositoryLink { get; set; }

        public Guid? OwningOrganizationUserId { get; set; }
        public NonprofitOrganization? OwningOrganization { get; set; }

        [Required(ErrorMessage = "Programming Languages are required")]
        public List<string> ProgrammingLanguages { get; set; } = new();


        // Add this property
        public List<ProjectMember> ProjectMembers { get; set; } = new();

        // Helper property to work with dictionary
        [NotMapped]
        public Dictionary<string, Guid> RoleToIdMap
        {
            get => ProjectMembers.ToDictionary(ur => ur.UserRole, ur => ur.UserId);
            set => ProjectMembers = value.Select(userRolePair =>
                new ProjectMember
                {
                    UserRole = userRolePair.Key,
                    UserId = userRolePair.Value,
                    User = GetTeamMemberById(userRolePair.Value),
                    Project = this
                })
                .ToList();
        }
        public Gainer? GetTeamMemberById(Guid userId)
        {
            return ProjectMembers
                .Where(pm => pm.UserId == userId && pm.LeftAtUtc == null)
                .Select(pm => pm.User as Gainer)
                .FirstOrDefault();
        }
    }
}