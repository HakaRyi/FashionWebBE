using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;
using Repositories.Repos.ImageRepos;

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
