namespace GainIt.API.DTOs.ViewModels.Forum
{
    public class PostRepliedNotificationViewModel
    {
        public Guid PostId { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public Guid ReplyId { get; set; }
        public string ReplyContent { get; set; } = string.Empty;
        public string ReplyAuthorName { get; set; } = string.Empty;
        public Guid ReplyAuthorId { get; set; }
        public DateTime RepliedAtUtc { get; set; }
    }
}
