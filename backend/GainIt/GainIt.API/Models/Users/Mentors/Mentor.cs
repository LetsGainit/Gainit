using GainIt.API.Models.Enums.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users.Expertise;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Users.Mentors
{
    public class Mentor : User
    {
        public Mentor()
        {
            this.UserRole = eUserType.Mentor; // Set as "Mentor" by default
        }

        [Range(1, 50, ErrorMessage = "Years of Experience must be between 1 and 50")]
        public int YearsOfExperience { get; set; }

        [Required(ErrorMessage = "Area of Expertise is required")]
        [StringLength(200, ErrorMessage = "Area of Expertise cannot exceed 200 characters")]
        public string AreaOfExpertise { get; set; }

        [JsonIgnore]
        public TechExpertise TechExpertise { get; set; }

        [JsonIgnore]
        public List<UserProject> MentoredProjects { get; set; } = new();

        [JsonIgnore]
        public List<UserAchievement> Achievements { get; set; } = new();
    }
}
