namespace GainIt.API.DTOs.Requests.Users
{
    public class ExternalUserDto
    {
        public string ExternalId { get; init; } = default!;

        public string? Email { get; init; }

        public string? FullName { get; init; }

        public string? IdentityProvider { get; init; }

        public string? Country { get; init; }
    }
}
