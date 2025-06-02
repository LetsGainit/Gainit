using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.Users.Expertise
{
    public abstract class UserExpertise
    {
        [Key]
        public Guid ExpertiseId { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public Guid UserId { get; set; }

        // navigation back to its owner
        [Required]
        public required User User { get; set; }
    }
}
