using GainIt.API.Models.Enums.Users;

namespace GainIt.API.DTOs.Requests.Users
{
    public class UserProfileDto
    {

        public Guid UserId { get; set; }

        public string ExternalId { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Country { get; set; }

    }
}
