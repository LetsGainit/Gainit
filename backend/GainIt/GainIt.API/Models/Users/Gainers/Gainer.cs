using GainIt.API.Models.Enums.Users;

namespace GainIt.API.Models.Users.Gainers
{
    public class Gainer : User
    {
        public Gainer()
        {
            this.UserRole = eUserRole.Gainer;  // Set as "Gainer" by default
        }
        public string EducationStatus { get; set; }
        public List<string> AreasOfInterest { get; set; }
    }
}
