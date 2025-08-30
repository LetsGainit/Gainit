using GainIt.API.Models.ProjectForum;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Models.Users.Gainers;

namespace GainIt.API.DTOs.ViewModels.Forum
{
    public class ForumPostViewModel
    {
        public Guid PostId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<ForumReplyViewModel> Replies { get; set; } = new();

        public ForumPostViewModel() { }

        public ForumPostViewModel(ForumPost post, bool isLikedByCurrentUser = false, string? projectRole = null)
        {
            PostId = post.PostId;
            ProjectId = post.ProjectId;
            AuthorId = post.AuthorId;
            AuthorName = post.Author.FullName;
            AuthorRole = GetUserRole(post.Author, projectRole);
            Content = post.Content;
            CreatedAtUtc = post.CreatedAtUtc;
            UpdatedAtUtc = post.UpdatedAtUtc;
            LikeCount = post.LikeCount;
            ReplyCount = post.ReplyCount;
            IsLikedByCurrentUser = isLikedByCurrentUser;
            Replies = post.Replies.Select(r => new ForumReplyViewModel(r, false, projectRole)).ToList();
        }

        private string GetUserRole(User user, string? projectRole = null)
        {
            // Check user type first
            if (user is Mentor)
                return "Mentor";
            else if (user is NonprofitOrganization)
                return "Nonprofit/Owner";
            else if (user is Gainer)
            {
                // For gainers, use their project-specific role if available
                if (!string.IsNullOrEmpty(projectRole))
                    return $"Gainer - {projectRole}";
                else
                    return "Gainer";
            }
            else
                return "Member";
        }
    }
}
