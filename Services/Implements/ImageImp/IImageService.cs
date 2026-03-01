using Repositories.Entities;

namespace Services.Implements.ImageImp
{
    public interface IImageService
    {
        Task<Image> CreateAvatarImageAsync(int userId, string imageUrl);
        Task DeleteImageAsync(int imageId);
        Task<List<Image>> GetAllAvatarAsync();
        Task<List<Image>> GetAllMyAvatarAsync(int userId);
        Task<Image?> GetByIdAsync(int id);
        Task<Image?> GetNewestAvatarAsync(int userId);
    }
}