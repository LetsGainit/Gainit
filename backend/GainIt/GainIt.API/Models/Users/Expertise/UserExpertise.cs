using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Users.Expertise
{
    public abstract class UserExpertise
    {
        [Key]
        public Guid ExpertiseId { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("UserId")]
        public Guid UserId { get; set; }

        // navigation back to its owner
        [Required]
        [JsonIgnore]
        public User User { get; set; } = null!; // tells EF to not ignore the User property
    }
}
