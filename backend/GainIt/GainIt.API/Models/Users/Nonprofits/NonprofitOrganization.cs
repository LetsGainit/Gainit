using GainIt.API.Models.Enums.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users.Expertise;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Users.Nonprofits
{
    public class NonprofitOrganization : User
    {
        public NonprofitOrganization()
        {
            this.UserRole = eUserType.NonprofitOrganization;
        }

        [Required(ErrorMessage = "Website URL is required")]
        [Url(ErrorMessage = "Invalid Website URL")]
        public string WebsiteUrl { get; set; }

        [JsonIgnore]
        public NonprofitExpertise NonprofitExpertise { get; set; }

        [JsonIgnore]
        public List<UserProject> OwnedProjects { get; set; } = new();

    }
}
