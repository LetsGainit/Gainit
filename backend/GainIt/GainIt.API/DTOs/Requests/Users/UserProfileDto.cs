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

        public string? GitHubUsername { get; set; }  // Add GitHub username to profile

        /// <summary>
        /// True if user is new (never existed before) OR if they exist but haven't completed their profile form.
        /// False only if they exist AND have completed their profile (exist in Gainers, Mentors, or Nonprofits table).
        /// </summary>
        public bool IsNewUser { get; set; }

        public override string ToString()
        {
            return $"UserProfileDto{{UserId={UserId}, ExternalId='{ExternalId}', EmailAddress='{EmailAddress}', FullName='{FullName}', Country='{Country}', GitHubUsername='{GitHubUsername}', IsNewUser={IsNewUser}}}";
        }
    }
}
