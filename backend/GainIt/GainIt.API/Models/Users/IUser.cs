using GainIt.API.Models.Enums.Users;

namespace GainIt.API.Models.Users
{
    public interface IUser
    {
        int UserId { get; set; }
        string FullName { get; set; }
        string EmailAddress { get; set; }
        eUserRole UserRole { get; set; } // Will include: "Nonprofit", "Mentor", or "Gainer"
    }
}
