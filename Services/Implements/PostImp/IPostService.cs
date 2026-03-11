using Repositories.Dto.Response;
using Services.Request.PostReq;

namespace Services.Implements.PostImp
{
    public interface IPostService
    {
        Task<PostResponse> CreatePostAsync(int accountId, CreatePostRequest request);
        Task<List<PostResponse>> GetAllPostAsync();
        Task<PostResponse?> GetPostByIdAsync(int postId);
        Task<string> AdminCheckTheStatusPost(CheckPostRequest request, int id);
        Task<List<PostResponse>> GetAllMyPostAsync(int userId);
        Task<List<PostResponse>> GetFeedAsync(int userId, DateTime? cursor, int pageSize);
        Task<List<PostResponse>> GetPostsByUserAsync(int userId, int pageSize);
        Task<List<PostResponse>> GetTrendingPostsAsync(int limit);
        Task<PostResponse> UpdatePostAsync(int postId, int accountId, UpdatePostRequest request);
        Task DeletePostAsync(int postId);
    }
}