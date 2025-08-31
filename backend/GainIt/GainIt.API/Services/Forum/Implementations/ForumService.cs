using GainIt.API.Data;
using GainIt.API.DTOs.Requests.Forum;
using GainIt.API.DTOs.ViewModels.Forum;
using GainIt.API.Models.ProjectForum;
using GainIt.API.Services.Forum.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.Forum.Implementations
{
    public class ForumService : IForumService
    {
        private readonly GainItDbContext r_DbContext;
        private readonly ILogger<ForumService> r_Logger;
        private readonly IForumNotificationService r_NotificationService;

        public ForumService(
            GainItDbContext i_DbContext, 
            ILogger<ForumService> i_Logger,
            IForumNotificationService i_NotificationService)
        {
            r_DbContext = i_DbContext;
            r_Logger = i_Logger;
            r_NotificationService = i_NotificationService;
        }

        public async Task<ForumPostViewModel> CreatePostAsync(CreateForumPostDto i_CreateDto, Guid i_AuthorId)
        {
            r_Logger.LogInformation("Creating forum post: ProjectId={ProjectId}, AuthorId={AuthorId}", i_CreateDto.ProjectId, i_AuthorId);

            try
            {
                // Verify user is a project member
                if (!await isUserProjectMemberAsync(i_CreateDto.ProjectId, i_AuthorId))
                {
                    r_Logger.LogWarning("User is not a project member: ProjectId={ProjectId}, UserId={UserId}", i_CreateDto.ProjectId, i_AuthorId);
                    throw new UnauthorizedAccessException("User is not a member of this project");
                }

                var post = new ForumPost
                {
                    PostId = Guid.NewGuid(),
                    ProjectId = i_CreateDto.ProjectId,
                    AuthorId = i_AuthorId,
                    Content = i_CreateDto.Content,
                    CreatedAtUtc = DateTime.UtcNow
                };

                r_DbContext.ForumPosts.Add(post);
                await r_DbContext.SaveChangesAsync();

                // Load the post with author information
                var createdPost = await r_DbContext.ForumPosts
                    .Include(p => p.Author)
                    .FirstOrDefaultAsync(p => p.PostId == post.PostId);

                // Get project role for the post author
                var authorProjectRole = await getUserProjectRoleAsync(i_CreateDto.ProjectId, i_AuthorId);

                r_Logger.LogInformation("Successfully created forum post: PostId={PostId}", post.PostId);
                return new ForumPostViewModel(createdPost!, false, authorProjectRole);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error creating forum post: ProjectId={ProjectId}, AuthorId={AuthorId}", i_CreateDto.ProjectId, i_AuthorId);
                throw;
            }
        }

        public async Task<ForumPostViewModel> GetPostByIdAsync(Guid i_PostId, Guid i_CurrentUserId)
        {
            r_Logger.LogInformation("Getting forum post: PostId={PostId}, CurrentUserId={CurrentUserId}", i_PostId, i_CurrentUserId);

            try
            {
                var post = await r_DbContext.ForumPosts
                    .Include(p => p.Author)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies.OrderBy(r => r.CreatedAtUtc))
                        .ThenInclude(r => r.Author)
                    .Include(p => p.Replies)
                        .ThenInclude(r => r.Likes)
                    .FirstOrDefaultAsync(p => p.PostId == i_PostId);

                if (post == null)
                {
                    r_Logger.LogWarning("Forum post not found: PostId={PostId}", i_PostId);
                    throw new KeyNotFoundException($"Forum post with ID {i_PostId} not found");
                }

                // Verify user is a project member
                if (!await isUserProjectMemberAsync(post.ProjectId, i_CurrentUserId))
                {
                    r_Logger.LogWarning("User is not a project member: ProjectId={ProjectId}, UserId={UserId}", post.ProjectId, i_CurrentUserId);
                    throw new UnauthorizedAccessException("User is not a member of this project");
                }

                // Get project role for the post author
                var authorProjectRole = await getUserProjectRoleAsync(post.ProjectId, post.AuthorId);

                // Check if current user liked the post
                var isLikedByCurrentUser = post.Likes.Any(l => l.UserId == i_CurrentUserId);

                // Create a custom ForumPostViewModel with complete reply data
                var postViewModel = new ForumPostViewModel(post, isLikedByCurrentUser, authorProjectRole);
                
                // Update replies with proper like information
                postViewModel.Replies = post.Replies.Select(reply =>
                {
                    var replyIsLikedByCurrentUser = reply.Likes.Any(l => l.UserId == i_CurrentUserId);
                    return new ForumReplyViewModel(reply, replyIsLikedByCurrentUser, authorProjectRole);
                }).ToList();

                r_Logger.LogInformation("Successfully retrieved forum post: PostId={PostId}", i_PostId);
                return postViewModel;
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting forum post: PostId={PostId}", i_PostId);
                throw;
            }
        }

        public async Task<List<ForumPostViewModel>> GetProjectPostsAsync(Guid i_ProjectId, Guid i_CurrentUserId, int i_Page = 1, int i_PageSize = 10)
        {
            r_Logger.LogInformation("Getting project forum posts: ProjectId={ProjectId}, Page={Page}, PageSize={PageSize}", i_ProjectId, i_Page, i_PageSize);

            try
            {
                // Verify user is a project member
                if (!await isUserProjectMemberAsync(i_ProjectId, i_CurrentUserId))
                {
                    r_Logger.LogWarning("User is not a project member: ProjectId={ProjectId}, UserId={UserId}", i_ProjectId, i_CurrentUserId);
                    throw new UnauthorizedAccessException("User is not a member of this project");
                }

                var posts = await r_DbContext.ForumPosts
                    .Include(p => p.Author)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies)
                        .ThenInclude(r => r.Author)
                    .Include(p => p.Replies)
                        .ThenInclude(r => r.Likes)
                    .Where(p => p.ProjectId == i_ProjectId)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .ToListAsync();

                var postViewModels = new List<ForumPostViewModel>();
                foreach (var post in posts)
                {
                    var isLikedByCurrentUser = post.Likes.Any(l => l.UserId == i_CurrentUserId);
                    var authorProjectRole = await getUserProjectRoleAsync(i_ProjectId, post.AuthorId);
                    
                    // Create a custom ForumPostViewModel with complete reply data
                    var postViewModel = new ForumPostViewModel(post, isLikedByCurrentUser, authorProjectRole);
                    
                    // Update replies with proper like information
                    postViewModel.Replies = post.Replies.Select(reply =>
                    {
                        var replyIsLikedByCurrentUser = reply.Likes.Any(l => l.UserId == i_CurrentUserId);
                        return new ForumReplyViewModel(reply, replyIsLikedByCurrentUser, authorProjectRole);
                    }).ToList();
                    
                    postViewModels.Add(postViewModel);
                }

                r_Logger.LogInformation("Successfully retrieved project forum posts: ProjectId={ProjectId}, Count={Count}", i_ProjectId, postViewModels.Count);
                return postViewModels;
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting project forum posts: ProjectId={ProjectId}", i_ProjectId);
                throw;
            }
        }

        public async Task<ForumPostViewModel> UpdatePostAsync(Guid i_PostId, UpdateForumPostDto i_UpdateDto, Guid i_AuthorId)
        {
            r_Logger.LogInformation("Updating forum post: PostId={PostId}, AuthorId={AuthorId}", i_PostId, i_AuthorId);

            try
            {
                var post = await r_DbContext.ForumPosts
                    .Include(p => p.Author)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies.OrderBy(r => r.CreatedAtUtc))
                        .ThenInclude(r => r.Author)
                    .Include(p => p.Replies)
                        .ThenInclude(r => r.Likes)
                    .FirstOrDefaultAsync(p => p.PostId == i_PostId);

                if (post == null)
                {
                    r_Logger.LogWarning("Forum post not found: PostId={PostId}", i_PostId);
                    throw new KeyNotFoundException($"Forum post with ID {i_PostId} not found");
                }

                if (!await isPostAuthorAsync(i_PostId, i_AuthorId))
                {
                    r_Logger.LogWarning("User is not the author of the post: PostId={PostId}, AuthorId={AuthorId}, RequestedBy={RequestedBy}", i_PostId, post.AuthorId, i_AuthorId);
                    throw new UnauthorizedAccessException("Only the author can update this post");
                }

                post.Content = i_UpdateDto.Content;
                post.UpdatedAtUtc = DateTime.UtcNow;

                await r_DbContext.SaveChangesAsync();

                // Get project role for the post author
                var authorProjectRole = await getUserProjectRoleAsync(post.ProjectId, post.AuthorId);

                // Create a custom ForumPostViewModel with complete reply data
                var postViewModel = new ForumPostViewModel(post, false, authorProjectRole);
                
                // Update replies with proper like information (no current user context for updates)
                postViewModel.Replies = post.Replies.Select(reply =>
                {
                    return new ForumReplyViewModel(reply, false, authorProjectRole);
                }).ToList();

                r_Logger.LogInformation("Successfully updated forum post: PostId={PostId}", i_PostId);
                return postViewModel;
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating forum post: PostId={PostId}", i_PostId);
                throw;
            }
        }

        public async Task DeletePostAsync(Guid i_PostId, Guid i_AuthorId)
        {
            r_Logger.LogInformation("Deleting forum post: PostId={PostId}, AuthorId={AuthorId}", i_PostId, i_AuthorId);

            try
            {
                var post = await r_DbContext.ForumPosts
                    .Include(p => p.Replies)
                    .Include(p => p.Likes)
                    .FirstOrDefaultAsync(p => p.PostId == i_PostId);

                if (post == null)
                {
                    r_Logger.LogWarning("Forum post not found: PostId={PostId}", i_PostId);
                    throw new KeyNotFoundException($"Forum post with ID {i_PostId} not found");
                }

                if (!await isPostAuthorAsync(i_PostId, i_AuthorId))
                {
                    r_Logger.LogWarning("User is not the author of the post: PostId={PostId}, AuthorId={AuthorId}, RequestedBy={RequestedBy}", i_PostId, post.AuthorId, i_AuthorId);
                    throw new UnauthorizedAccessException("Only the author can delete this post");
                }

                // Delete related entities
                r_DbContext.ForumReplyLikes.RemoveRange(post.Replies.SelectMany(r => r.Likes));
                r_DbContext.ForumReplies.RemoveRange(post.Replies);
                r_DbContext.ForumPostLikes.RemoveRange(post.Likes);
                r_DbContext.ForumPosts.Remove(post);

                await r_DbContext.SaveChangesAsync();

                r_Logger.LogInformation("Successfully deleted forum post: PostId={PostId}", i_PostId);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting forum post: PostId={PostId}", i_PostId);
                throw;
            }
        }

        public async Task<ForumReplyViewModel> CreateReplyAsync(CreateForumReplyDto i_CreateDto, Guid i_AuthorId)
        {
            r_Logger.LogInformation("Creating forum reply: PostId={PostId}, AuthorId={AuthorId}", i_CreateDto.PostId, i_AuthorId);

            try
            {
                // Verify the post exists and user is a project member
                var post = await r_DbContext.ForumPosts
                    .Include(p => p.Project)
                    .FirstOrDefaultAsync(p => p.PostId == i_CreateDto.PostId);

                if (post == null)
                {
                    r_Logger.LogWarning("Forum post not found: PostId={PostId}", i_CreateDto.PostId);
                    throw new KeyNotFoundException($"Forum post with ID {i_CreateDto.PostId} not found");
                }

                if (!await isUserProjectMemberAsync(post.ProjectId, i_AuthorId))
                {
                    r_Logger.LogWarning("User is not a project member: ProjectId={ProjectId}, UserId={UserId}", post.ProjectId, i_AuthorId);
                    throw new UnauthorizedAccessException("User is not a member of this project");
                }

                var reply = new ForumReply
                {
                    ReplyId = Guid.NewGuid(),
                    PostId = i_CreateDto.PostId,
                    AuthorId = i_AuthorId,
                    Content = i_CreateDto.Content,
                    CreatedAtUtc = DateTime.UtcNow
                };

                r_DbContext.ForumReplies.Add(reply);

                // Update post reply count
                post.ReplyCount++;

                await r_DbContext.SaveChangesAsync();

                // Load the reply with author information
                var createdReply = await r_DbContext.ForumReplies
                    .Include(r => r.Author)
                    .FirstOrDefaultAsync(r => r.ReplyId == reply.ReplyId);

                // Get project role for the reply author
                var authorProjectRole = await getUserProjectRoleAsync(post.ProjectId, i_AuthorId);

                var replyViewModel = new ForumReplyViewModel(createdReply!, false, authorProjectRole);

                // Send notification to post author
                await r_NotificationService.PostRepliedAsync(i_CreateDto.PostId, replyViewModel);

                r_Logger.LogInformation("Successfully created forum reply: ReplyId={ReplyId}", reply.ReplyId);
                return replyViewModel;
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error creating forum reply: PostId={PostId}, AuthorId={AuthorId}", i_CreateDto.PostId, i_AuthorId);
                throw;
            }
        }

        public async Task<ForumReplyViewModel> UpdateReplyAsync(Guid i_ReplyId, UpdateForumReplyDto i_UpdateDto, Guid i_AuthorId)
        {
            r_Logger.LogInformation("Updating forum reply: ReplyId={ReplyId}, AuthorId={AuthorId}", i_ReplyId, i_AuthorId);

            try
            {
                var reply = await r_DbContext.ForumReplies
                    .Include(r => r.Author)
                    .Include(r => r.Post)
                    .FirstOrDefaultAsync(r => r.ReplyId == i_ReplyId);

                if (reply == null)
                {
                    r_Logger.LogWarning("Forum reply not found: ReplyId={ReplyId}", i_ReplyId);
                    throw new KeyNotFoundException($"Forum reply with ID {i_ReplyId} not found");
                }

                if (!await isReplyAuthorAsync(i_ReplyId, i_AuthorId))
                {
                    r_Logger.LogWarning("User is not the author of the reply: ReplyId={ReplyId}, AuthorId={AuthorId}, RequestedBy={RequestedBy}", i_ReplyId, reply.AuthorId, i_AuthorId);
                    throw new UnauthorizedAccessException("Only the author can update this reply");
                }

                reply.Content = i_UpdateDto.Content;
                reply.UpdatedAtUtc = DateTime.UtcNow;

                await r_DbContext.SaveChangesAsync();

                // Get project role for the reply author
                var authorProjectRole = await getUserProjectRoleAsync(reply.Post.ProjectId, reply.AuthorId);

                r_Logger.LogInformation("Successfully updated forum reply: ReplyId={ReplyId}", i_ReplyId);
                return new ForumReplyViewModel(reply, false, authorProjectRole);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating forum reply: ReplyId={ReplyId}", i_ReplyId);
                throw;
            }
        }

        public async Task DeleteReplyAsync(Guid i_ReplyId, Guid i_AuthorId)
        {
            r_Logger.LogInformation("Deleting forum reply: ReplyId={ReplyId}, AuthorId={AuthorId}", i_ReplyId, i_AuthorId);

            try
            {
                var reply = await r_DbContext.ForumReplies
                    .Include(r => r.Likes)
                    .Include(r => r.Post)
                    .FirstOrDefaultAsync(r => r.ReplyId == i_ReplyId);

                if (reply == null)
                {
                    r_Logger.LogWarning("Forum reply not found: ReplyId={ReplyId}", i_ReplyId);
                    throw new KeyNotFoundException($"Forum reply with ID {i_ReplyId} not found");
                }

                if (!await isReplyAuthorAsync(i_ReplyId, i_AuthorId))
                {
                    r_Logger.LogWarning("User is not the author of the reply: ReplyId={ReplyId}, AuthorId={AuthorId}, RequestedBy={RequestedBy}", i_ReplyId, reply.AuthorId, i_AuthorId);
                    throw new UnauthorizedAccessException("Only the author can delete this reply");
                }

                // Delete related likes
                r_DbContext.ForumReplyLikes.RemoveRange(reply.Likes);
                r_DbContext.ForumReplies.Remove(reply);

                // Update post reply count
                reply.Post.ReplyCount--;

                await r_DbContext.SaveChangesAsync();

                r_Logger.LogInformation("Successfully deleted forum reply: ReplyId={ReplyId}", i_ReplyId);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting forum reply: ReplyId={ReplyId}", i_ReplyId);
                throw;
            }
        }

        public async Task TogglePostLikeAsync(Guid i_PostId, Guid i_UserId)
        {
            r_Logger.LogInformation("Toggling post like: PostId={PostId}, UserId={UserId}", i_PostId, i_UserId);

            try
            {
                var existingLike = await r_DbContext.ForumPostLikes
                    .FirstOrDefaultAsync(l => l.PostId == i_PostId && l.UserId == i_UserId);

                if (existingLike != null)
                {
                    // Unlike
                    r_DbContext.ForumPostLikes.Remove(existingLike);
                    var post = await r_DbContext.ForumPosts.FindAsync(i_PostId);
                    if (post != null)
                    {
                        post.LikeCount--;
                    }
                    r_Logger.LogInformation("Post unliked: PostId={PostId}, UserId={UserId}", i_PostId, i_UserId);
                }
                else
                {
                    // Like
                    var like = new ForumPostLike
                    {
                        PostId = i_PostId,
                        UserId = i_UserId,
                        LikedAtUtc = DateTime.UtcNow
                    };
                    r_DbContext.ForumPostLikes.Add(like);
                    var post = await r_DbContext.ForumPosts.FindAsync(i_PostId);
                    if (post != null)
                    {
                        post.LikeCount++;
                    }
                    r_Logger.LogInformation("Post liked: PostId={PostId}, UserId={UserId}", i_PostId, i_UserId);

                    // Send notification to post author
                    await r_NotificationService.PostLikedAsync(i_PostId, i_UserId);
                }

                await r_DbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error toggling post like: PostId={PostId}, UserId={UserId}", i_PostId, i_UserId);
                throw;
            }
        }

        public async Task ToggleReplyLikeAsync(Guid i_ReplyId, Guid i_UserId)
        {
            r_Logger.LogInformation("Toggling reply like: ReplyId={ReplyId}, UserId={UserId}", i_ReplyId, i_UserId);

            try
            {
                var existingLike = await r_DbContext.ForumReplyLikes
                    .FirstOrDefaultAsync(l => l.ReplyId == i_ReplyId && l.UserId == i_UserId);

                if (existingLike != null)
                {
                    // Unlike
                    r_DbContext.ForumReplyLikes.Remove(existingLike);
                    var reply = await r_DbContext.ForumReplies.FindAsync(i_ReplyId);
                    if (reply != null)
                    {
                        reply.LikeCount--;
                    }
                    r_Logger.LogInformation("Reply unliked: ReplyId={ReplyId}, UserId={UserId}", i_ReplyId, i_UserId);
                }
                else
                {
                    // Like
                    var like = new ForumReplyLike
                    {
                        ReplyId = i_ReplyId,
                        UserId = i_UserId,
                        LikedAtUtc = DateTime.UtcNow
                    };
                    r_DbContext.ForumReplyLikes.Add(like);
                    var reply = await r_DbContext.ForumReplies.FindAsync(i_ReplyId);
                    if (reply != null)
                    {
                        reply.LikeCount++;
                    }
                    r_Logger.LogInformation("Reply liked: ReplyId={ReplyId}, UserId={UserId}", i_ReplyId, i_UserId);

                    // Send notification to reply author
                    await r_NotificationService.ReplyLikedAsync(i_ReplyId, i_UserId);
                }

                await r_DbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error toggling reply like: ReplyId={ReplyId}, UserId={UserId}", i_ReplyId, i_UserId);
                throw;
            }
        }

        private async Task<bool> isUserProjectMemberAsync(Guid i_ProjectId, Guid i_UserId)
        {
            return await r_DbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_UserId && pm.LeftAtUtc == null);
        }

        private async Task<bool> isPostAuthorAsync(Guid i_PostId, Guid i_UserId)
        {
            return await r_DbContext.ForumPosts
                .AnyAsync(p => p.PostId == i_PostId && p.AuthorId == i_UserId);
        }

        private async Task<bool> isReplyAuthorAsync(Guid i_ReplyId, Guid i_UserId)
        {
            return await r_DbContext.ForumReplies
                .AnyAsync(r => r.ReplyId == i_ReplyId && r.AuthorId == i_UserId);
        }

        private async Task<string?> getUserProjectRoleAsync(Guid i_ProjectId, Guid i_UserId)
        {
            var projectMember = await r_DbContext.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == i_ProjectId && pm.UserId == i_UserId && pm.LeftAtUtc == null);
            
            return projectMember?.UserRole;
        }
    }
}
