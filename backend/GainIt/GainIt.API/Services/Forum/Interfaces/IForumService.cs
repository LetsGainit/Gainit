using GainIt.API.DTOs.Requests.Forum;
using GainIt.API.DTOs.ViewModels.Forum;
using GainIt.API.Models.ProjectForum;

namespace GainIt.API.Services.Forum.Interfaces
{
    public interface IForumService
    {
        // Post operations
        Task<ForumPostViewModel> CreatePostAsync(CreateForumPostDto i_CreateDto, Guid i_AuthorId);
        Task<ForumPostViewModel> GetPostByIdAsync(Guid i_PostId, Guid i_CurrentUserId);
        Task<List<ForumPostViewModel>> GetProjectPostsAsync(Guid i_ProjectId, Guid i_CurrentUserId, int i_Page = 1, int i_PageSize = 10);
        Task<ForumPostViewModel> UpdatePostAsync(Guid i_PostId, UpdateForumPostDto i_UpdateDto, Guid i_AuthorId);
        Task DeletePostAsync(Guid i_PostId, Guid i_AuthorId);

        // Reply operations
        Task<ForumReplyViewModel> CreateReplyAsync(CreateForumReplyDto i_CreateDto, Guid i_AuthorId);
        Task<ForumReplyViewModel> UpdateReplyAsync(Guid i_ReplyId, UpdateForumReplyDto i_UpdateDto, Guid i_AuthorId);
        Task DeleteReplyAsync(Guid i_ReplyId, Guid i_AuthorId);

        // Like operations
        Task TogglePostLikeAsync(Guid i_PostId, Guid i_UserId);
        Task ToggleReplyLikeAsync(Guid i_ReplyId, Guid i_UserId);


    }
}
