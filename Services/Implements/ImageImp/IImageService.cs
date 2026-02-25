using Repositories.Entities;

namespace Services.Implements.ImageImp
{
    public interface IImageService
    {
        Task<Image> GetNewestAvatar(int userId);
        Task<Image> GetByIdAsync(int id);
        Task<List<Image>> GetAllMyAvatar(int userId);
        Task<List<Image>> GetAllAvatar();
        Task<int> CreateAvatarImage(int userId, Image image);
        Task<bool> DeteleImage(int imageId);
    }
}
