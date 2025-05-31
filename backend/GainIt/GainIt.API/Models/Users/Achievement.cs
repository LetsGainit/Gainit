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
        public required string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public required string Description { get; set; }

        public bool IsUnlocked { get; set; }

        [Url(ErrorMessage = "Invalid Picture URL")]
        [StringLength(500, ErrorMessage = "Picture URL cannot exceed 500 characters")]
        public required string IconUrl { get; set; }

        // foreign‐key back to User
        [ForeignKey("UserId")]
        public Guid UserId { get; set; }

        // navigation back to its owner
        [ForeignKey("User")]
        public required User User { get; set; }
    }
}