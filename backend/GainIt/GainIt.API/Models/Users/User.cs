using System.ComponentModel.DataAnnotations;
using GainIt.API.Models.Enums.Users;
using GainIt.API.Models.Projects;

namespace GainIt.API.Models.Users
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(200, ErrorMessage = "Email Address cannot exceed 200 characters")]
        public string EmailAddress { get; set; }

        [Required]
        public eUserRole UserRole { get; protected set; } // Will include: "NonprofitOrganization", "Mentor", or "Gainer"

    }
}
