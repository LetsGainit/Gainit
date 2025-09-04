using GainIt.API.DTOs.Requests.Forum;
using GainIt.API.DTOs.ViewModels.Forum;
using GainIt.API.Services.Forum.Interfaces;
using GainIt.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace GainIt.API.Controllers.Forum
{
    [ApiController]
    [Route("api/forum")]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class ForumController : ControllerBase
    {
        private readonly IForumService r_ForumService;
        private readonly ILogger<ForumController> r_Logger;
        private readonly GainItDbContext r_DbContext;

        public ForumController(IForumService i_ForumService, ILogger<ForumController> i_Logger, GainItDbContext i_DbContext)
        {
            r_ForumService = i_ForumService;
            r_Logger = i_Logger;
            r_DbContext = i_DbContext;
        }

        #region Post Operations

        /// <summary>
        /// Creates a new forum post in a project.
        /// </summary>
        /// <param name="i_CreateDto">The post creation data.</param>
        /// <returns>The created post.</returns>
        [HttpPost("posts")]
        public async Task<ActionResult<ForumPostViewModel>> CreatePost([FromBody] CreateForumPostDto i_CreateDto)
        {
            r_Logger.LogInformation("Creating forum post: ProjectId={ProjectId}", i_CreateDto.ProjectId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var post = await r_ForumService.CreatePostAsync(i_CreateDto, currentUserId);
                r_Logger.LogInformation("Successfully created forum post: PostId={PostId}", post.PostId);
                return CreatedAtAction(nameof(GetPost), new { postId = post.PostId }, post);
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error creating forum post");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a forum post by its ID.
        /// </summary>
        /// <param name="i_PostId">The ID of the post to retrieve.</param>
        /// <returns>The forum post with replies.</returns>
        [HttpGet("posts/{postId}")]
        public async Task<ActionResult<ForumPostViewModel>> GetPost(Guid i_PostId)
        {
            r_Logger.LogInformation("Getting forum post: PostId={PostId}", i_PostId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var post = await r_ForumService.GetPostByIdAsync(i_PostId, currentUserId);
                r_Logger.LogInformation("Successfully retrieved forum post: PostId={PostId}", i_PostId);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum post not found: PostId={PostId}", i_PostId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting forum post: PostId={PostId}", i_PostId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all forum posts for a project with pagination.
        /// </summary>
        /// <param name="i_ProjectId">The ID of the project.</param>
        /// <param name="page">The page number (default: 1).</param>
        /// <param name="pageSize">The number of posts per page (default: 10).</param>
        /// <returns>A list of forum posts.</returns>
        [HttpGet("projects/{projectId}/posts")]
        public async Task<ActionResult<List<ForumPostViewModel>>> GetProjectPosts(Guid i_ProjectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            r_Logger.LogInformation("Getting project forum posts: ProjectId={ProjectId}, Page={Page}, PageSize={PageSize}", i_ProjectId, page, pageSize);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var posts = await r_ForumService.GetProjectPostsAsync(i_ProjectId, currentUserId, page, pageSize);
                r_Logger.LogInformation("Successfully retrieved project forum posts: ProjectId={ProjectId}, Count={Count}", i_ProjectId, posts.Count);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting project forum posts: ProjectId={ProjectId}", i_ProjectId);
                throw;
            }
        }

        /// <summary>
        /// Updates a forum post.
        /// </summary>
        /// <param name="i_PostId">The ID of the post to update.</param>
        /// <param name="i_UpdateDto">The update data.</param>
        /// <returns>The updated post.</returns>
        [HttpPut("posts/{postId}")]
        public async Task<ActionResult<ForumPostViewModel>> UpdatePost(Guid i_PostId, [FromBody] UpdateForumPostDto i_UpdateDto)
        {
            r_Logger.LogInformation("Updating forum post: PostId={PostId}", i_PostId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var post = await r_ForumService.UpdatePostAsync(i_PostId, i_UpdateDto, currentUserId);
                r_Logger.LogInformation("Successfully updated forum post: PostId={PostId}", i_PostId);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum post not found: PostId={PostId}", i_PostId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating forum post: PostId={PostId}", i_PostId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a forum post.
        /// </summary>
        /// <param name="i_PostId">The ID of the post to delete.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("posts/{postId}")]
        public async Task<ActionResult> DeletePost(Guid i_PostId)
        {
            r_Logger.LogInformation("Deleting forum post: PostId={PostId}", i_PostId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                await r_ForumService.DeletePostAsync(i_PostId, currentUserId);
                r_Logger.LogInformation("Successfully deleted forum post: PostId={PostId}", i_PostId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum post not found: PostId={PostId}", i_PostId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting forum post: PostId={PostId}", i_PostId);
                throw;
            }
        }

        #endregion

        #region Reply Operations

        /// <summary>
        /// Creates a new reply to a forum post.
        /// </summary>
        /// <param name="i_CreateDto">The reply creation data.</param>
        /// <returns>The created reply.</returns>
        [HttpPost("replies")]
        public async Task<ActionResult<ForumReplyViewModel>> CreateReply([FromBody] CreateForumReplyDto i_CreateDto)
        {
            r_Logger.LogInformation("Creating forum reply: PostId={PostId}", i_CreateDto.PostId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var reply = await r_ForumService.CreateReplyAsync(i_CreateDto, currentUserId);
                r_Logger.LogInformation("Successfully created forum reply: ReplyId={ReplyId}", reply.ReplyId);
                return CreatedAtAction(nameof(GetPost), new { postId = i_CreateDto.PostId }, reply);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum post not found: PostId={PostId}", i_CreateDto.PostId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error creating forum reply");
                throw;
            }
        }

        /// <summary>
        /// Updates a forum reply.
        /// </summary>
        /// <param name="i_ReplyId">The ID of the reply to update.</param>
        /// <param name="i_UpdateDto">The update data.</param>
        /// <returns>The updated reply.</returns>
        [HttpPut("replies/{replyId}")]
        public async Task<ActionResult<ForumReplyViewModel>> UpdateReply(Guid i_ReplyId, [FromBody] UpdateForumReplyDto i_UpdateDto)
        {
            r_Logger.LogInformation("Updating forum reply: ReplyId={ReplyId}", i_ReplyId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var reply = await r_ForumService.UpdateReplyAsync(i_ReplyId, i_UpdateDto, currentUserId);
                r_Logger.LogInformation("Successfully updated forum reply: ReplyId={ReplyId}", i_ReplyId);
                return Ok(reply);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum reply not found: ReplyId={ReplyId}", i_ReplyId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating forum reply: ReplyId={ReplyId}", i_ReplyId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a forum reply.
        /// </summary>
        /// <param name="i_ReplyId">The ID of the reply to delete.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("replies/{replyId}")]
        public async Task<ActionResult> DeleteReply(Guid i_ReplyId)
        {
            r_Logger.LogInformation("Deleting forum reply: ReplyId={ReplyId}", i_ReplyId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                await r_ForumService.DeleteReplyAsync(i_ReplyId, currentUserId);
                r_Logger.LogInformation("Successfully deleted forum reply: ReplyId={ReplyId}", i_ReplyId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum reply not found: ReplyId={ReplyId}", i_ReplyId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting forum reply: ReplyId={ReplyId}", i_ReplyId);
                throw;
            }
        }

        #endregion

        #region Like Operations

        /// <summary>
        /// Toggles the like status of a forum post.
        /// </summary>
        /// <param name="i_PostId">The ID of the post to like/unlike.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("posts/{postId}/like")]
        public async Task<ActionResult> TogglePostLike(Guid i_PostId)
        {
            r_Logger.LogInformation("Toggling post like: PostId={PostId}", i_PostId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                await r_ForumService.TogglePostLikeAsync(i_PostId, currentUserId);
                r_Logger.LogInformation("Successfully toggled post like: PostId={PostId}", i_PostId);
                return NoContent();
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error toggling post like: PostId={PostId}", i_PostId);
                throw;
            }
        }

        /// <summary>
        /// Toggles the like status of a forum reply.
        /// </summary>
        /// <param name="i_ReplyId">The ID of the reply to like/unlike.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("replies/{replyId}/like")]
        public async Task<ActionResult> ToggleReplyLike(Guid i_ReplyId)
        {
            r_Logger.LogInformation("Toggling reply like: ReplyId={ReplyId}", i_ReplyId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                await r_ForumService.ToggleReplyLikeAsync(i_ReplyId, currentUserId);
                r_Logger.LogInformation("Successfully toggled reply like: ReplyId={ReplyId}", i_ReplyId);
                return NoContent();
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error toggling reply like: ReplyId={ReplyId}", i_ReplyId);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the current user ID from the authentication context.
        /// Extracts the external ID from JWT claims and maps it to the database User ID.
        /// </summary>
        /// <returns>The current user ID from the database.</returns>
        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var externalId = User.FindFirst("oid")?.Value
                  ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(externalId))
                throw new UnauthorizedAccessException("User ID not found in token.");

            // Find the user in the database by external ID
            var user = await r_DbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ExternalId == externalId);

            if (user == null)
                throw new UnauthorizedAccessException("User not found in database.");

            return user.UserId;
        }

        #endregion
    }
}

