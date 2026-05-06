using Domain.Contracts.Common;
using Domain.Dto.Social.SavedPost;
using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IPostSaveRepository
    {
        Task<bool> ExistsAsync(int postId, int accountId);
        Task AddAsync(PostSave postSave);
        Task<PostSave?> GetByPostAndUserAsync(int postId, int accountId);
        void Delete(PostSave postSave);
        Task<PagedResultDto<SavedPostDto>> GetSavedPostsAsync(int accountId, int page, int pageSize);
    }
}