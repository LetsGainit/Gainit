using GainIt.API.Models.Enums.Users;

namespace GainIt.API.Models.Users.Mentors
{
    public class Mentor : User
    {
        public Mentor()
        {
            this.UserRole = eUserRole.Mentor; // Set as "Mentor" by default
        }
        public int YearsOfExperience { get; set; }
        public string AreaOfExpertise { get; set; }
    }
}
