using Repositories.Entities;
using Repositories.Repos.ImageRepos;
using Repositories.UnitOfWork;

namespace Services.Implements.ImageImp
{
    public class ImageService : IImageService
    {
        private readonly IImageRepository _imageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ImageService(
            IImageRepository imageRepository,
            IUnitOfWork unitOfWork)
        {
            _imageRepository = imageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Image> CreateAvatarImageAsync(int userId, string imageUrl)
        {
            var newImage = new Image
            {
                ImageUrl = imageUrl,
                AccountAvatarId = userId,
                OwnerType = "AVATAR",
                CreatedAt = DateTime.UtcNow
            };

            await _imageRepository.AddAsync(newImage);
            await _unitOfWork.SaveChangesAsync();

            return newImage;
        }

        public async Task DeleteImageAsync(int imageId)
        {
            var image = await _imageRepository.GetByIdAsync(imageId)
                ?? throw new Exception("Image not found");

            _imageRepository.Delete(image);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<Image>> GetAllAvatarAsync()
        {
            return await _imageRepository.GetAllAvatarAsync();
        }

        public async Task<List<Image>> GetAllMyAvatarAsync(int userId)
        {
            return await _imageRepository.GetAllMyAvatarAsync(userId);
        }

        public async Task<Image?> GetByIdAsync(int id)
        {
            return await _imageRepository.GetByIdAsync(id);
        }

        public async Task<Image?> GetNewestAvatarAsync(int userId)
        {
            return await _imageRepository.GetNewestAvatarAsync(userId);
        }
    }
}