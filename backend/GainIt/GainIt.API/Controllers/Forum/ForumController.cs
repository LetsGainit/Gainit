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
        /// <param name="createDto">The post creation data.</param>
        /// <returns>The created post.</returns>
        [HttpPost("posts")]
        public async Task<ActionResult<ForumPostViewModel>> CreatePost([FromBody] CreateForumPostDto createDto)
        {
            r_Logger.LogInformation("Creating forum post: ProjectId={ProjectId}", createDto.ProjectId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var post = await r_ForumService.CreatePostAsync(createDto, currentUserId);
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
        /// <param name="postId">The ID of the post to retrieve.</param>
        /// <returns>The forum post with replies.</returns>
        [HttpGet("posts/{postId}")]
        public async Task<ActionResult<ForumPostViewModel>> GetPost(Guid postId)
        {
            r_Logger.LogInformation("Getting forum post: PostId={PostId}", postId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var post = await r_ForumService.GetPostByIdAsync(postId, currentUserId);
                r_Logger.LogInformation("Successfully retrieved forum post: PostId={PostId}", postId);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum post not found: PostId={PostId}", postId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting forum post: PostId={PostId}", postId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all forum posts for a project with pagination.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="page">The page number (default: 1).</param>
        /// <param name="pageSize">The number of posts per page (default: 10).</param>
        /// <returns>A list of forum posts.</returns>
        [HttpGet("projects/{projectId}/posts")]
        public async Task<ActionResult<List<ForumPostViewModel>>> GetProjectPosts(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            r_Logger.LogInformation("Getting project forum posts: ProjectId={ProjectId}, Page={Page}, PageSize={PageSize}", projectId, page, pageSize);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var posts = await r_ForumService.GetProjectPostsAsync(projectId, currentUserId, page, pageSize);
                r_Logger.LogInformation("Successfully retrieved project forum posts: ProjectId={ProjectId}, Count={Count}", projectId, posts.Count);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error getting project forum posts: ProjectId={ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// Updates a forum post.
        /// </summary>
        /// <param name="postId">The ID of the post to update.</param>
        /// <param name="updateDto">The update data.</param>
        /// <returns>The updated post.</returns>
        [HttpPut("posts/{postId}")]
        public async Task<ActionResult<ForumPostViewModel>> UpdatePost(Guid postId, [FromBody] UpdateForumPostDto updateDto)
        {
            r_Logger.LogInformation("Updating forum post: PostId={PostId}", postId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var post = await r_ForumService.UpdatePostAsync(postId, updateDto, currentUserId);
                r_Logger.LogInformation("Successfully updated forum post: PostId={PostId}", postId);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum post not found: PostId={PostId}", postId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating forum post: PostId={PostId}", postId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a forum post.
        /// </summary>
        /// <param name="postId">The ID of the post to delete.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("posts/{postId}")]
        public async Task<ActionResult> DeletePost(Guid postId)
        {
            r_Logger.LogInformation("Deleting forum post: PostId={PostId}", postId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                await r_ForumService.DeletePostAsync(postId, currentUserId);
                r_Logger.LogInformation("Successfully deleted forum post: PostId={PostId}", postId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum post not found: PostId={PostId}", postId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting forum post: PostId={PostId}", postId);
                throw;
            }
        }

        #endregion

        #region Reply Operations

        /// <summary>
        /// Creates a new reply to a forum post.
        /// </summary>
        /// <param name="createDto">The reply creation data.</param>
        /// <returns>The created reply.</returns>
        [HttpPost("replies")]
        public async Task<ActionResult<ForumReplyViewModel>> CreateReply([FromBody] CreateForumReplyDto createDto)
        {
            r_Logger.LogInformation("Creating forum reply: PostId={PostId}", createDto.PostId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var reply = await r_ForumService.CreateReplyAsync(createDto, currentUserId);
                r_Logger.LogInformation("Successfully created forum reply: ReplyId={ReplyId}", reply.ReplyId);
                return CreatedAtAction(nameof(GetPost), new { postId = createDto.PostId }, reply);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum post not found: PostId={PostId}", createDto.PostId);
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
        /// <param name="replyId">The ID of the reply to update.</param>
        /// <param name="updateDto">The update data.</param>
        /// <returns>The updated reply.</returns>
        [HttpPut("replies/{replyId}")]
        public async Task<ActionResult<ForumReplyViewModel>> UpdateReply(Guid replyId, [FromBody] UpdateForumReplyDto updateDto)
        {
            r_Logger.LogInformation("Updating forum reply: ReplyId={ReplyId}", replyId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                var reply = await r_ForumService.UpdateReplyAsync(replyId, updateDto, currentUserId);
                r_Logger.LogInformation("Successfully updated forum reply: ReplyId={ReplyId}", replyId);
                return Ok(reply);
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum reply not found: ReplyId={ReplyId}", replyId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error updating forum reply: ReplyId={ReplyId}", replyId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a forum reply.
        /// </summary>
        /// <param name="replyId">The ID of the reply to delete.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("replies/{replyId}")]
        public async Task<ActionResult> DeleteReply(Guid replyId)
        {
            r_Logger.LogInformation("Deleting forum reply: ReplyId={ReplyId}", replyId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                await r_ForumService.DeleteReplyAsync(replyId, currentUserId);
                r_Logger.LogInformation("Successfully deleted forum reply: ReplyId={ReplyId}", replyId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                r_Logger.LogWarning("Forum reply not found: ReplyId={ReplyId}", replyId);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                r_Logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error deleting forum reply: ReplyId={ReplyId}", replyId);
                throw;
            }
        }

        #endregion

        #region Like Operations

        /// <summary>
        /// Toggles the like status of a forum post.
        /// </summary>
        /// <param name="postId">The ID of the post to like/unlike.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("posts/{postId}/like")]
        public async Task<ActionResult> TogglePostLike(Guid postId)
        {
            r_Logger.LogInformation("Toggling post like: PostId={PostId}", postId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                await r_ForumService.TogglePostLikeAsync(postId, currentUserId);
                r_Logger.LogInformation("Successfully toggled post like: PostId={PostId}", postId);
                return NoContent();
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error toggling post like: PostId={PostId}", postId);
                throw;
            }
        }

        /// <summary>
        /// Toggles the like status of a forum reply.
        /// </summary>
        /// <param name="replyId">The ID of the reply to like/unlike.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("replies/{replyId}/like")]
        public async Task<ActionResult> ToggleReplyLike(Guid replyId)
        {
            r_Logger.LogInformation("Toggling reply like: ReplyId={ReplyId}", replyId);

            try
            {
                var currentUserId = await GetCurrentUserIdAsync();

                await r_ForumService.ToggleReplyLikeAsync(replyId, currentUserId);
                r_Logger.LogInformation("Successfully toggled reply like: ReplyId={ReplyId}", replyId);
                return NoContent();
            }
            catch (Exception ex)
            {
                r_Logger.LogError(ex, "Error toggling reply like: ReplyId={ReplyId}", replyId);
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
            var externalId = tryGetClaim(User, "oid", ClaimTypes.NameIdentifier)
                ?? tryGetClaim(User, "sub")
                ?? tryGetClaim(User, ClaimTypes.NameIdentifier)
                ?? tryGetClaim(User, "http://schemas.microsoft.com/identity/claims/objectidentifier")
                ?? tryGetClaim(User, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

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

        /// <summary>
        /// Helper method to try getting a claim value from multiple claim types.
        /// </summary>
        /// <param name="user">The user claims principal.</param>
        /// <param name="types">The claim types to try.</param>
        /// <returns>The claim value if found, null otherwise.</returns>
        private static string? tryGetClaim(ClaimsPrincipal user, params string[] types)
        {
            foreach (var t in types)
            {
                var v = user.FindFirstValue(t);
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            return null;
        }

        #endregion
    }
}

