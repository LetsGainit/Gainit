using GainIt.API.DTOs.ViewModels.Forum;

namespace GainIt.API.Services.Forum.Interfaces
{
    public interface IForumNotificationService
    {
        /// <summary>
        /// Sends notifications when someone replies to a forum post
        /// Email + SignalR notification to the post author
        /// </summary>
        Task PostRepliedAsync(Guid i_PostId, ForumReplyViewModel i_Reply);

        /// <summary>
        /// Sends SignalR notification when someone likes a forum post
        /// SignalR only notification to the post author
        /// </summary>
        Task PostLikedAsync(Guid i_PostId, Guid i_LikedByUserId);

        /// <summary>
        /// Sends SignalR notification when someone likes a forum reply
        /// SignalR only notification to the reply author
        /// </summary>
        Task ReplyLikedAsync(Guid i_ReplyId, Guid i_LikedByUserId);
    }
}
