using GainIt.API.Models.Enums.Users;
using GainIt.API.Models.Projects;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.Models.Users
{
    [Index(nameof(ExternalId), IsUnique = true)]
    [Index(nameof(EmailAddress), IsUnique = true)]
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Required, StringLength(200)]
        public string ExternalId { get; set; } = default!;

        [StringLength(100)]
        public string? Country { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200)]
        public string EmailAddress { get; set; } = string.Empty;

        public eUserType? UserRole { get; protected set; }

        [StringLength(1000)]
        public string? Biography { get; set; }

        [Url, StringLength(200)] public string? FacebookPageURL { get; set; }
        [Url, StringLength(200)] public string? LinkedInURL { get; set; }
        [Url, StringLength(200)] public string? GitHubURL { get; set; }
        [Url, StringLength(200)] public string? ProfilePictureURL { get; set; }

        public List<UserAchievement> Achievements { get; set; } = new();

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastLoginAt { get; set; }
    }
}
