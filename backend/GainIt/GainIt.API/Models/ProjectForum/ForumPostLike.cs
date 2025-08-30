using GainIt.API.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.ProjectForum
{
    public class ForumPostLike
    {
        [Key]
        [Column(Order = 0)]
        public Guid PostId { get; set; }

        [Key]
        [Column(Order = 1)]
        public Guid UserId { get; set; }

        [Required]
        public DateTime LikedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(PostId))]
        public ForumPost Post { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
