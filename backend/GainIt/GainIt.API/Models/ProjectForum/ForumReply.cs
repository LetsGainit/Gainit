using GainIt.API.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.ProjectForum
{
    public class ForumReply
    {
        [Key]
        public Guid ReplyId { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid AuthorId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Reply content cannot exceed 1000 characters")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public int LikeCount { get; set; } = 0;

        // Navigation properties
        [ForeignKey(nameof(PostId))]
        public ForumPost Post { get; set; } = null!;

        [ForeignKey(nameof(AuthorId))]
        public User Author { get; set; } = null!;

        public List<ForumReplyLike> Likes { get; set; } = new();
    }
}
