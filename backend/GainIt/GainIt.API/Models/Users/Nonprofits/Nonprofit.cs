using GainIt.API.Models.Enums.Users;

namespace GainIt.API.Models.Users.Nonprofits
{
    public class Nonprofit : IUser
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        public eUserRole UserRole { get; set; } = eUserRole.Nonprofit;  // Set as "Nonprofit" by default
        public string WebsiteUrl { get; set; }

    }
}
