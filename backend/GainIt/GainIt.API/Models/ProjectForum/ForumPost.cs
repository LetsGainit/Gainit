using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GainIt.API.Models.ProjectForum
{
    public class ForumPost
    {
        [Key]
        public Guid PostId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public Guid AuthorId { get; set; }

        [Required]
        [StringLength(2000, ErrorMessage = "Post content cannot exceed 2000 characters")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public List<ForumReply> Replies { get; set; } = new();

        public List<ForumPostLike> Likes { get; set; } = new();

        public int LikeCount { get; set; } = 0;

        public int ReplyCount { get; set; } = 0;

        // Navigation properties
        [ForeignKey(nameof(ProjectId))]
        public UserProject Project { get; set; } = null!;

        [ForeignKey(nameof(AuthorId))]
        public User Author { get; set; } = null!;

        
    }
}
