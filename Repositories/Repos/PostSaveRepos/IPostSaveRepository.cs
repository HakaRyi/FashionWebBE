using Repositories.Dto.Common;
using Repositories.Dto.Social.SavedPost;
using Repositories.Entities;

namespace Repositories.Repos.PostSaveRepos
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