using GainIt.API.Models.Enums.Users;

namespace GainIt.API.Models.Users.Gainers
{
    public class Gainer : IUser
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        public eUserRole UserRole { get; set; } = eUserRole.Gainer;  // Set as "Gainer" by default
        public string EducationStatus { get; set; }
        public List<string> AreasOfInterest { get; set; }
    }
}
