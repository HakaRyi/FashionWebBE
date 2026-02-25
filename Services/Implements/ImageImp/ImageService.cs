using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;
using Repositories.Repos.ImageRepos;

namespace Services.Implements.ImageImp
{
    public class ImageService : IImageService
    {
        private readonly IImageRepository _imageRepository;
        public ImageService(IImageRepository imageRepository)
        {
            _imageRepository = imageRepository;
        }

        public async Task<int> CreateAvatarImage(int userId, Image image)
        {
            var newImage = new Image
            {
                ImageUrl = image.ImageUrl,
                AccountAvatarId = userId,
                PostId = null,
                ItemId = null,
                OwnerType = "AVATAR",
                CreatedAt = DateTime.UtcNow
            };
            return await _imageRepository.CreateImage(newImage);
        }

        public async Task<bool> DeteleImage(int imageId)
        {
            var image = await _imageRepository.GetById(imageId);
            var result = await _imageRepository.DeteleImage(image);
            return true;
        }

        public async Task<List<Image>> GetAllAvatar()
        {
            return await _imageRepository.GetAllAvatar();
        }

        public async Task<List<Image>> GetAllMyAvatar(int userId)
        {
            return await _imageRepository.GetAllMyAvatar(userId);
        }

        public async Task<Image> GetByIdAsync(int id)
        {
            return await _imageRepository.GetById(id);
        }

        public async Task<Image> GetNewestAvatar(int userId)
        {
            return await _imageRepository.GetNewestAvatar(userId);
        }
    }
}
