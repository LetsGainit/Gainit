using GainIt.API.Models.Enums.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users.Expertise;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Users.Gainers
{
    public class Gainer : User
    {
        public Gainer()
        {
            this.UserRole = eUserType.Gainer;  // Set as "Gainer" by default
        }

        [Required(ErrorMessage = "Education Status is required")]
        [StringLength(100, ErrorMessage = "Education Status cannot exceed 100 characters")]
        public string EducationStatus { get; set; }
        
        [JsonIgnore]
        public List<string> AreasOfInterest { get; set; }

        [JsonIgnore]
        public TechExpertise? TechExpertise { get; set; }

        [JsonIgnore]
        public List<UserProject> ParticipatedProjects { get; set; } = new();

        // Note: Achievements are inherited from User base class
        // No need to redeclare them here
    }
}
