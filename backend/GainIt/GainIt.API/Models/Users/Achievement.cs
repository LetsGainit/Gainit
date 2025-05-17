using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.Users
{
    public class Achievement
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        public bool IsUnlocked { get; set; }

        [Required]
        [StringLength(500)]
        public string IconUrl { get; set; }

        // foreign‐key back to User
        [ForeignKey("UserId")]
        public Guid UserId { get; set; }

        // navigation back to its owner
        [ForeignKey("User")]
        public User User { get; set; }
    }
}