using Microsoft.AspNetCore.Http;
using Repositories.Dto.Common;
using Repositories.Dto.Social.Post;
using Services.Request.PostReq;
using Services.Response.PostResp;

namespace Services.Implements.PostImp
{
    public interface IPostService
    {
        Task<int> CreatePostAsync(
            int accountId,
            CreatePostDto dto,
            List<IFormFile>? files);

        Task UpdatePostAsync(
            int postId,
            int accountId,
            UpdatePostDto dto);

        Task DeletePostAsync(int postId, int accountId);

        Task<List<PostFeedDto>> GetFeedAsync(
            int userId,
            DateTime? cursor,
            int pageSize);

        Task<PostDetailDto?> GetPostDetailAsync(
            int postId,
            int userId);

        Task<PagedResultDto<MyPostDto>> GetMyPostsAsync(
            int ownerId,
            int page,
            int pageSize);

        Task<PagedResultDto<PostFeedDto>> GetUserPostsAsync(
            int ownerId,
            int? viewerId,
            int page,
            int pageSize);

        Task<List<PostFeedDto>> GetTrendingPostsAsync(
            int userId,
            int limit);

        Task<PostVisibilityResponseDto> HidePostAsync(
            int postId,
            int accountId);

        Task<PostVisibilityResponseDto> UnhidePostAsync(
            int postId,
            int accountId);

        Task<PostResponse> CreatePostAsync(int accountId, CreatePostRequest request);
        Task<List<PostResponse>> GetAllPostAsync();
        Task<List<PostResponse>> GetAllPendingAdminAsync();
        Task<int> UpdatePostStatus(int postId, string status);
        Task<PostResponse?> GetPostByIdAsync(int postId);
        Task<string> AdminCheckTheStatusPost(CheckPostRequest request, int id);
        Task<List<PostResponse>> GetAllMyPostAsync(int userId);
        Task<PostResponse> UpdatePostAsync(int postId, int accountId, UpdatePostRequest request);
        Task DeletePostAsync(int postId);
        Task SetPostDeleteStatus(int postId);
        Task SetPostBannedStatus(int postId);
    }
}