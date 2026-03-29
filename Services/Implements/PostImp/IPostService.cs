using Repositories.Dto.Common;
using Repositories.Dto.Social.Post;
using Services.Request.PostReq;
using Services.Response.PostResp;

namespace Services.Implements.PostImp
{
    public interface IPostService
    {
        Task<PostResponse> CreatePostAsync(int accountId, CreatePostDto dto);
        Task UpdatePostAsync(int postId, int accountId, UpdatePostDto dto);
        Task DeletePostAsync(int postId, int accountId);
        Task<string> AdminCheckTheStatusPost(CheckPostRequest request, int id);
        Task<List<PostResponse>> GetAllPostAsync();
        Task<List<PostResponse>> GetAllMyPostAsync(int userId);
        Task<PostResponse?> GetPostByIdAsync(int postId);
        Task<List<PostFeedDto>> GetFeedAsync(int userId, DateTime? cursor, int pageSize);
        Task<PostDetailDto?> GetPostDetailAsync(int postId, int userId);
        Task<PagedResultDto<MyPostDto>> GetMyPostsAsync(int ownerId, int page, int pageSize);
        Task<PagedResultDto<PostFeedDto>> GetUserPostsAsync(int ownerId, int? viewerId, int page, int pageSize);
        Task<List<PostFeedDto>> GetTrendingPostsAsync(int userId, int limit);
        Task<PostVisibilityResponseDto> HidePostAsync(int postId, int accountId);
        Task<PostVisibilityResponseDto> UnhidePostAsync(int postId, int accountId);
        Task<PagedResultDto<AdminReviewPostDto>> GetPendingAdminPostsAsync(int page, int pageSize);
        Task<PagedResultDto<AdminReviewPostDto>> GetRejectedPostsAsync(int page, int pageSize);



        Task<List<PostResponse>> GetPostsByEventIdAsync(int eventId);
        Task<PostResponse> UpdatePostAsync(int postId, int accountId, UpdatePostRequest request);

        Task DeletePostAsync(int postId);
        Task SetPostDeleteStatus(int postId);
        Task SetPostBannedStatus(int postId);

    }
}