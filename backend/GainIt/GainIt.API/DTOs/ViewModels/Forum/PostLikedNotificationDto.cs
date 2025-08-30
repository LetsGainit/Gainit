namespace GainIt.API.DTOs.ViewModels.Forum
{
    public class PostLikedNotificationDto
    {
        public Guid PostId { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string LikedByUserName { get; set; } = string.Empty;
        public Guid LikedByUserId { get; set; }
        public DateTime LikedAtUtc { get; set; }
    }
}
