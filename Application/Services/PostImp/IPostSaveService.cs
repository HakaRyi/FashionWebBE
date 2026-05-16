using Domain.Contracts.Common;
using Domain.Dto.Social.SavedPost;

namespace Application.Services.PostImp
{
    public interface IPostSaveService
    {
        Task<bool> SavePostAsync(int postId, int accountId);
        Task<bool> UnsavePostAsync(int postId, int accountId);
        Task<PagedResultDto<SavedPostDto>> GetSavedPostsAsync(int accountId, int page, int pageSize);
    }
}