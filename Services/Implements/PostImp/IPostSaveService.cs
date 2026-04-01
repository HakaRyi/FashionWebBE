using Repositories.Dto.Common;
using Repositories.Dto.Social.SavedPost;

namespace Services.Implements.PostImp
{
    public interface IPostSaveService
    {
        Task<bool> SavePostAsync(int postId, int accountId);
        Task<bool> UnsavePostAsync(int postId, int accountId);
        Task<PagedResultDto<SavedPostDto>> GetSavedPostsAsync(int accountId, int page, int pageSize);
    }
}