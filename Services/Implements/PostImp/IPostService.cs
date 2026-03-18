using Services.Request.PostReq;
using Services.Response.PostResp;

namespace Services.Implements.PostImp
{
    public interface IPostService
    {
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