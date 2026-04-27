using Domain.Dto.Admin;
using Domain.Dto.Social.Post;
using Application.Request.PostReq;
using Application.Response.PostResp;
using Domain.Contracts.Social.Post;
using Domain.Contracts.Common;

namespace Application.Services.PostImp
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
        Task<int> SharePostAsync(int postId);
        Task<List<PostResponse>> GetPostsByEventIdAsync(int eventId);
        Task<List<PostResponse>> GetPostsForExpertReviewAsync(int eventId);
        Task<PostResponse> UpdatePostAsync(int postId, int accountId, UpdatePostRequest request);
        Task DeletePostAsync(int postId);
        Task SetPostDeleteStatus(int postId);
        Task SetPostBannedStatus(int postId);
        Task<PostResponse> JoinEventByPostAsync(int accountId, CreatePostDto dto);
        Task<GlobalSearchResultDto> SearchEverythingAsync(string keyword, int? viewerId);
    }
}