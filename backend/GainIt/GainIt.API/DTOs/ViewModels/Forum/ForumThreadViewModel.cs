namespace GainIt.API.DTOs.ViewModels.Forum
{
    public class ForumThreadViewModel
    {
        public ForumPostViewModel Post { get; set; } = new();
        public int TotalReplies { get; set; }
        public int TotalLikes { get; set; }

        public ForumThreadViewModel() { }

        public ForumThreadViewModel(ForumPostViewModel post)
        {
            Post = post;
            TotalReplies = post.ReplyCount;
            TotalLikes = post.LikeCount;
        }
    }
}
