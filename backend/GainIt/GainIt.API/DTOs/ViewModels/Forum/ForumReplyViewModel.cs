using GainIt.API.Models.ProjectForum;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Models.Users.Gainers;

namespace GainIt.API.DTOs.ViewModels.Forum
{
    public class ForumReplyViewModel
    {
        public Guid ReplyId { get; set; }
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool CanEdit { get; set; }

        public ForumReplyViewModel() { }

        public ForumReplyViewModel(ForumReply reply, bool isLikedByCurrentUser = false, string? projectRole = null, bool canEdit = false)
        {
            ReplyId = reply.ReplyId;
            PostId = reply.PostId;
            AuthorId = reply.AuthorId;
            AuthorName = reply.Author.FullName;
            AuthorRole = GetUserRole(reply.Author, projectRole);
            Content = reply.Content;
            CreatedAtUtc = reply.CreatedAtUtc;
            UpdatedAtUtc = reply.UpdatedAtUtc;
            LikeCount = reply.LikeCount;
            IsLikedByCurrentUser = isLikedByCurrentUser;
            CanEdit = canEdit;
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
