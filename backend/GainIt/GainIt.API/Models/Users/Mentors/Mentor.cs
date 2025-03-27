using GainIt.API.Models.Enums.Users;

namespace GainIt.API.Models.Users.Mentors
{
    public class Mentor : IUser
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        public eUserRole UserRole { get; set; } = eUserRole.Mentor;  // Set as "Mentor" by default
        public int YearsOfExperience { get; set; }
        public string AreaOfExpertise { get; set; }
    }
}
