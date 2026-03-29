using Repositories.Dto.Social.SavedPost;

namespace Services.Implements.PostImp
{
    public interface IPostSaveService
    {
        Task SavePostAsync(int postId, int accountId);

        Task UnsavePostAsync(int postId, int accountId);

        Task<List<SavedPostDto>> GetSavedPostsAsync(
            int accountId,
            int page,
            int pageSize);
    }
}