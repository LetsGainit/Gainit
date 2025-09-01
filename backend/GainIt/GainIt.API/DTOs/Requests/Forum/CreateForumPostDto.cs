using System.ComponentModel.DataAnnotations;

namespace GainIt.API.DTOs.Requests.Forum
{
    public class CreateForumPostDto
    {
        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        [StringLength(2000, ErrorMessage = "Post content cannot exceed 2000 characters")]
        public string Content { get; set; } = string.Empty;
    }
}
