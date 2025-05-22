using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.Users
{
    public abstract class UserExpertise
    {
        [Key]
        public Guid ExpertiseId { get; set; }

        [Required]
        public Guid UserId { get; set; }
    }
}