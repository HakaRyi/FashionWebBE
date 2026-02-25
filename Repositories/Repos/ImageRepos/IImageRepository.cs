using Repositories.Entities;

namespace Repositories.Repos.ImageRepos
{
    public interface IImageRepository
    {
        Task<Image> GetNewestAvatar(int userId);
        Task<Image> GetById(int id);
        Task<List<Image>> GetAllMyAvatar(int userId);
        Task<List<Image>> GetAllAvatar();
        Task<int> CreateImage(Image image);
        Task<bool> DeteleImage(Image image);
    }
}
