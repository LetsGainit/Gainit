using GainIt.API.Models.Enums.Users;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.Models.Users.Nonprofits
{
    public class NonprofitOrganization : User
    {
        public NonprofitOrganization()
        {
            this.UserRole = eUserRole.NonprofitOrganization;
        }

        [Required(ErrorMessage = "Website URL is required")]
        [Url(ErrorMessage = "Invalid Website URL")]
        public string WebsiteUrl { get; set; }

    }
}
