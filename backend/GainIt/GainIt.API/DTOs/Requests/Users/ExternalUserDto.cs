namespace GainIt.API.DTOs.Requests.Users
{
    public class ExternalUserDto
    {
        public string ExternalId { get; init; } = default!;

        public string? Email { get; init; }

        public string? FullName { get; init; }

        public string? IdentityProvider { get; init; }

        public string? Country { get; init; }

        public string? GitHubUsername { get; init; }  // Add GitHub username for registration

        public override string ToString()
        {
            return $"ExternalUserDto{{ExternalId='{ExternalId}', Email='{Email}', FullName='{FullName}', IdentityProvider='{IdentityProvider}', Country='{Country}', GitHubUsername='{GitHubUsername}'}}";
        }
    }
}
