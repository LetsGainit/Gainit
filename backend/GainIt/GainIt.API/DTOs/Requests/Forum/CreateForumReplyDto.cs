using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Forum
{
    public class CreateForumReplyDto
    {
        [Required]
        public Guid PostId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Reply content cannot exceed 1000 characters")]
        public string Content { get; set; } = string.Empty;
    }
}
