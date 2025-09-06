using GainIt.API.Data;
using GainIt.API.DTOs.ViewModels.Forum;
using GainIt.API.Realtime;
using GainIt.API.Services.Email.Interfaces;
using GainIt.API.Services.Forum.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.Forum.Implementations
{
    public class ForumNotificationService : IForumNotificationService
    {
        private readonly GainItDbContext r_Db;
        private readonly IEmailSender r_Email;
        private readonly IHubContext<NotificationsHub> r_Hub;
        private readonly ILogger<ForumNotificationService> r_Log;

        public ForumNotificationService(
            GainItDbContext i_Db,
            IEmailSender i_Email,
            IHubContext<NotificationsHub> i_Hub,
            ILogger<ForumNotificationService> i_Log)
        {
            r_Db = i_Db;
            r_Email = i_Email;
            r_Hub = i_Hub;
            r_Log = i_Log;
        }

        public async Task PostRepliedAsync(Guid i_PostId, ForumReplyViewModel i_Reply)
        {
            try
            {
                // Get the post with author and project information
                var post = await r_Db.ForumPosts
                    .Include(p => p.Author)
                    .Include(p => p.Project)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PostId == i_PostId);

                if (post == null)
                {
                    r_Log.LogWarning("Post not found for reply notification: PostId={PostId}", i_PostId);
                    return;
                }

                // Don't notify if the reply author is the same as the post author
                if (post.AuthorId == i_Reply.AuthorId)
                {
                    r_Log.LogDebug("Skipping notification - reply author is same as post author: PostId={PostId}, AuthorId={AuthorId}", 
                        i_PostId, i_Reply.AuthorId);
                    return;
                }

                // Get the reply author information
                var replyAuthor = await r_Db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == i_Reply.AuthorId);

                if (replyAuthor == null)
                {
                    r_Log.LogWarning("Reply author not found: AuthorId={AuthorId}", i_Reply.AuthorId);
                    return;
                }

                // Send SignalR notification to post author
                if (!string.IsNullOrEmpty(post.Author.ExternalId))
                {
                    try
                    {
                        await r_Hub.Clients.User(post.Author.ExternalId)
                            .SendAsync(RealtimeEvents.Forum.PostReplied, new PostRepliedNotificationViewModel
                            {
                                PostId = post.PostId,
                                ProjectId = post.ProjectId,
                                ProjectName = post.Project.ProjectName ?? "Unknown Project",
                                ReplyId = i_Reply.ReplyId,
                                ReplyContent = i_Reply.Content ?? "",
                                ReplyAuthorName = i_Reply.AuthorName ?? "Unknown User",
                                ReplyAuthorId = i_Reply.AuthorId,
                                RepliedAtUtc = i_Reply.CreatedAtUtc
                            });
                    }
                    catch (Exception signalrEx)
                    {
                        r_Log.LogWarning(signalrEx, "Failed to send SignalR post replied notification: PostAuthorId={PostAuthorId}, ExternalId={ExternalId}, PostId={PostId}", 
                            post.AuthorId, post.Author.ExternalId, post.PostId);
                    }
                }
                else
                {
                    r_Log.LogWarning("Post author has no ExternalId for SignalR notification: PostAuthorId={PostAuthorId}, Email={Email}, PostId={PostId}", 
                        post.AuthorId, post.Author.EmailAddress, post.PostId);
                }

                r_Log.LogInformation("SignalR notification sent for post reply: PostId={PostId}, ReplyId={ReplyId}, PostAuthorId={PostAuthorId}", 
                    i_PostId, i_Reply.ReplyId, post.AuthorId);

                // Send email notification to post author
                await r_Email.SendAsync(
                    post.Author.EmailAddress,
                    $"GainIt Notifications: New reply to your post in {post.Project.ProjectName}",
                    $"Hi {post.Author.FullName},\n\n{replyAuthor.FullName} replied to your post in project '{post.Project.ProjectName}'.\n\nReply: {i_Reply.Content}\n\nYou can view the full discussion in your project forum.",
                    null
                );

                r_Log.LogInformation("Email notification sent for post reply: PostId={PostId}, ReplyId={ReplyId}, PostAuthorEmail={PostAuthorEmail}", 
                    i_PostId, i_Reply.ReplyId, post.Author.EmailAddress);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error sending post reply notification: PostId={PostId}, ReplyId={ReplyId}", 
                    i_PostId, i_Reply.ReplyId);
            }
        }

        public async Task PostLikedAsync(Guid i_PostId, Guid i_LikedByUserId)
        {
            try
            {
                // Get the post with author and project information
                var post = await r_Db.ForumPosts
                    .Include(p => p.Author)
                    .Include(p => p.Project)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PostId == i_PostId);

                if (post?.Project == null)
                {
                    r_Log.LogWarning("Post or project not found for like notification: PostId={PostId}", i_PostId);
                    return;
                }

                // Don't notify if the like author is the same as the post author
                if (post.AuthorId == i_LikedByUserId)
                {
                    r_Log.LogDebug("Skipping notification - like author is same as post author: PostId={PostId}, AuthorId={AuthorId}", 
                        i_PostId, i_LikedByUserId);
                    return;
                }

                // Get the user who liked the post
                var likedByUser = await r_Db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == i_LikedByUserId);

                if (likedByUser == null)
                {
                    r_Log.LogWarning("User who liked post not found: UserId={UserId}", i_LikedByUserId);
                    return;
                }

                // Send SignalR notification only to post author
                if (!string.IsNullOrEmpty(post.Author.ExternalId))
                {
                    try
                    {
                        await r_Hub.Clients.User(post.Author.ExternalId)
                            .SendAsync(RealtimeEvents.Forum.PostLiked, new PostLikedNotificationViewModel
                            {
                                PostId = post.PostId,
                                ProjectId = post.ProjectId,
                                ProjectName = post.Project.ProjectName ?? "Unknown Project",
                                LikedByUserName = likedByUser.FullName ?? "Unknown User",
                                LikedByUserId = i_LikedByUserId,
                                LikedAtUtc = DateTime.UtcNow
                            });
                    }
                    catch (Exception signalrEx)
                    {
                        r_Log.LogWarning(signalrEx, "Failed to send SignalR post liked notification: PostAuthorId={PostAuthorId}, ExternalId={ExternalId}, PostId={PostId}", 
                            post.AuthorId, post.Author.ExternalId, post.PostId);
                    }
                }
                else
                {
                    r_Log.LogWarning("Post author has no ExternalId for SignalR notification: PostAuthorId={PostAuthorId}, Email={Email}, PostId={PostId}", 
                        post.AuthorId, post.Author.EmailAddress, post.PostId);
                }

                r_Log.LogInformation("SignalR notification sent for post like: PostId={PostId}, LikedByUserId={LikedByUserId}, PostAuthorId={PostAuthorId}", 
                    i_PostId, i_LikedByUserId, post.AuthorId);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error sending post like notification: PostId={PostId}, LikedByUserId={LikedByUserId}", 
                    i_PostId, i_LikedByUserId);
            }
        }

        public async Task ReplyLikedAsync(Guid i_ReplyId, Guid i_LikedByUserId)
        {
            try
            {
                // Get the reply with author and post information
                var reply = await r_Db.ForumReplies
                    .Include(r => r.Author)
                    .Include(r => r.Post)
                    .ThenInclude(p => p.Project)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.ReplyId == i_ReplyId);

                if (reply?.Post?.Project == null)
                {
                    r_Log.LogWarning("Reply, post, or project not found for like notification: ReplyId={ReplyId}", i_ReplyId);
                    return;
                }

                // Don't notify if the like author is the same as the reply author
                if (reply.AuthorId == i_LikedByUserId)
                {
                    r_Log.LogDebug("Skipping notification - like author is same as reply author: ReplyId={ReplyId}, AuthorId={AuthorId}", 
                        i_ReplyId, i_LikedByUserId);
                    return;
                }

                // Get the user who liked the reply
                var likedByUser = await r_Db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == i_LikedByUserId);

                if (likedByUser == null)
                {
                    r_Log.LogWarning("User who liked reply not found: UserId={UserId}", i_LikedByUserId);
                    return;
                }

                // Send SignalR notification only to reply author
                if (!string.IsNullOrEmpty(reply.Author.ExternalId))
                {
                    try
                    {
                        await r_Hub.Clients.User(reply.Author.ExternalId)
                            .SendAsync(RealtimeEvents.Forum.ReplyLiked, new ReplyLikedNotificationViewModel
                            {
                                ReplyId = reply.ReplyId,
                                PostId = reply.PostId,
                                ProjectName = reply.Post.Project.ProjectName ?? "Unknown Project",
                                LikedByUserName = likedByUser.FullName ?? "Unknown User",
                                LikedByUserId = i_LikedByUserId,
                                LikedAtUtc = DateTime.UtcNow
                            });
                    }
                    catch (Exception signalrEx)
                    {
                        r_Log.LogWarning(signalrEx, "Failed to send SignalR reply liked notification: ReplyAuthorId={ReplyAuthorId}, ExternalId={ExternalId}, ReplyId={ReplyId}", 
                            reply.AuthorId, reply.Author.ExternalId, reply.ReplyId);
                    }
                }
                else
                {
                    r_Log.LogWarning("Reply author has no ExternalId for SignalR notification: ReplyAuthorId={ReplyAuthorId}, Email={Email}, ReplyId={ReplyId}", 
                        reply.AuthorId, reply.Author.EmailAddress, reply.ReplyId);
                }

                r_Log.LogInformation("SignalR notification sent for reply like: ReplyId={ReplyId}, LikedByUserId={LikedByUserId}, ReplyAuthorId={ReplyAuthorId}", 
                    i_ReplyId, i_LikedByUserId, reply.AuthorId);
            }
            catch (Exception ex)
            {
                r_Log.LogError(ex, "Error sending reply like notification: ReplyId={ReplyId}, LikedByUserId={LikedByUserId}", 
                    i_ReplyId, i_LikedByUserId);
            }
        }
    }
}
