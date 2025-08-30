namespace GainIt.API.DTOs.ViewModels.Forum
{
    public class ReplyLikedNotificationDto
    {
        public Guid ReplyId { get; set; }
        public Guid PostId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string LikedByUserName { get; set; } = string.Empty;
        public Guid LikedByUserId { get; set; }
        public DateTime LikedAtUtc { get; set; }
    }
}
