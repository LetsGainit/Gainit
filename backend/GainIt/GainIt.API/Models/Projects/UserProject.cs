using GainIt.API.Models.Enums.Projects;
using System.ComponentModel.DataAnnotations;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.Projects
{
    public class UserProject : TemplateProject
    {
        [Required]
        public eProjectStatus ProjectStatus { get; set; }

        [Required]
        public eProjectSource ProjectSource { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public List<Gainer> TeamMembers { get; set; } = new();

        [Url(ErrorMessage = "Invalid Repository URL")]
        public string? RepositoryLink { get; set; }

        public Guid? AssignedMentorUserId { get; set; }
        public Mentor? AssignedMentor { get; set; }

        public Guid? OwningOrganizationUserId { get; set; }
        public NonprofitOrganization? OwningOrganization { get; set; }

        [Required]
        public bool IsPublic { get; set; } = true; // Default to public

        [Required(ErrorMessage = "Programming Languages are required")]
        public List<string> ProgrammingLanguages { get; set; } = new();


        // Add this property
        public List<ProjectUserRole> UserRoles { get; set; } = new();

        // Helper property to work with dictionary
        [NotMapped]
        public Dictionary<Guid, string> UserIdToRoleMap
        {
            get => UserRoles.ToDictionary(ur => ur.UserId, ur => ur.UserRole);
            set => UserRoles = value.Select(userRolePair =>
                new ProjectUserRole { UserId = userRolePair.Key, UserRole = userRolePair.Value })
                .ToList();
        }
    }
}   