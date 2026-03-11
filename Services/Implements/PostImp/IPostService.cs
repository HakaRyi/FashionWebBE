using Repositories.Dto.Response;
using Services.Request.PostReq;
using Services.Response.PostResp;

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
        Task UpdatePostAsync(int postId, int accountId, UpdatePostRequest request);
        Task DeletePostAsync(int postId);
    }
}