using GainIt.API.Models.Enums.Users;

namespace GainIt.API.Models.Users.Nonprofits
{
    public class NonprofitOrganization : User
    {
        public NonprofitOrganization()
        {
            this.UserRole = eUserRole.NonprofitOrganization;
        }
        public string WebsiteUrl { get; set; }

    }
}
