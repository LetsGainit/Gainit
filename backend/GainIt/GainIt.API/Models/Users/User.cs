using GainIt.API.Models.Enums.Users;

namespace GainIt.API.Models.Users
{
    public class User
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        public eUserRole UserRole { get; protected set; } // Will include: "NonprofitOrganization", "Mentor", or "Gainer"
    }
}
