using Microsoft.AspNetCore.Http;
using Repositories.Dto.Common;
using Repositories.Dto.Social.Post;
using Services.Request.PostReq;
using Services.Response.PostResp;

namespace Services.Implements.PostImp
{
    public interface IPostService
    {
        Task<int> CreatePostAsync(int accountId, CreatePostDto dto, List<IFormFile>? files);

        Task UpdatePostAsync(int postId, int accountId, UpdatePostDto dto);

        Task DeletePostAsync(int postId, int accountId);

        Task<List<PostFeedDto>> GetFeedAsync(int userId, DateTime? cursor, int pageSize);

        Task<PostDetailDto?> GetPostDetailAsync(int postId, int userId);

        Task<PagedResultDto<MyPostDto>> GetMyPostsAsync(int ownerId, int page, int pageSize);

        Task<PagedResultDto<PostFeedDto>> GetUserPostsAsync(int ownerId, int? viewerId, int page, int pageSize);

        Task<List<PostFeedDto>> GetTrendingPostsAsync(int userId, int limit);

        Task<PostVisibilityResponseDto> HidePostAsync(int postId, int accountId);

        Task<PostVisibilityResponseDto> UnhidePostAsync(int postId, int accountId);

        Task<PagedResultDto<AdminReviewPostDto>> GetAIRejectedPostsAsync(int page, int pageSize);

        Task ReviewAIRejectedPostAsync(int postId, bool approve);
    }
}