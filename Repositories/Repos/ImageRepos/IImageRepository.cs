using Repositories.Entities;

namespace Repositories.Repos.ImageRepos
{
    public interface IImageRepository
    {
        Task<Image?> GetByIdAsync(int id);
        Task<Image?> GetNewestAvatarAsync(int userId);
        Task<List<Image>> GetAllMyAvatarAsync(int userId);
        Task<List<Image>> GetPostImagesAsync(int postId);
        Task<List<Image>> GetItemImagesAsync(int itemId);
        Task AddAsync(Image image);
        Task AddRangeAsync(List<Image> images);
        void Delete(Image image);
        void DeleteRange(List<Image> images);
    }
}