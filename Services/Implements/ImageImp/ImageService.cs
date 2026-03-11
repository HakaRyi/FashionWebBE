using Repositories.Dto.Response;
using Repositories.Entities;
using Repositories.Repos.ImageRepos;
using Repositories.UnitOfWork;
using Services.Utils.Mapper;

namespace Services.Implements.ImageImp
{
    public class ImageService : IImageService
    {
        private readonly IImageRepository _imageRepository;
        private readonly IUnitOfWork _unitOfWork;

        private const int MAX_AVATAR_HISTORY = 10;

        public ImageService(
            IImageRepository imageRepository,
            IUnitOfWork unitOfWork)
        {
            _imageRepository = imageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ImageResponse> CreateAvatarImageAsync(int userId, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("ImageUrl cannot be empty.");

            var newImage = new Image
            {
                ImageUrl = imageUrl,
                AccountAvatarId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _imageRepository.AddAsync(newImage);

            var avatars = await _imageRepository.GetAllMyAvatarAsync(userId);

            if (avatars.Count >= MAX_AVATAR_HISTORY)
            {
                var remove = avatars
                    .OrderBy(a => a.CreatedAt)
                    .Take(avatars.Count - MAX_AVATAR_HISTORY + 1)
                    .ToList();

                _imageRepository.DeleteRange(remove);
            }

            await _unitOfWork.SaveChangesAsync();

            return newImage.ToResponse();
        }


        public async Task<List<ImageResponse>> CreatePostImagesAsync(
            int postId,
            List<string> imageUrls)
        {
            if (imageUrls == null || !imageUrls.Any())
                return new List<ImageResponse>();

            var images = imageUrls.Select(url => new Image
            {
                ImageUrl = url,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _imageRepository.AddRangeAsync(images);

            await _unitOfWork.SaveChangesAsync();

            return images.ToResponseList();
        }

        public async Task<List<ImageResponse>> CreateItemImagesAsync(
            int itemId,
            List<string> imageUrls)
        {
            if (imageUrls == null || !imageUrls.Any())
                return new List<ImageResponse>();

            var images = imageUrls.Select(url => new Image
            {
                ImageUrl = url,
                ItemId = itemId,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _imageRepository.AddRangeAsync(images);

            await _unitOfWork.SaveChangesAsync();

            return images.ToResponseList();
        }

        public async Task DeleteImageAsync(int imageId)
        {
            var image = await _imageRepository.GetByIdAsync(imageId)
                ?? throw new KeyNotFoundException("Image not found.");

            _imageRepository.Delete(image);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteImagesAsync(List<int> imageIds)
        {
            if (imageIds == null || !imageIds.Any())
                return;

            var images = new List<Image>();

            foreach (var id in imageIds)
            {
                var img = await _imageRepository.GetByIdAsync(id);
                if (img != null)
                    images.Add(img);
            }

            if (images.Any())
            {
                _imageRepository.DeleteRange(images);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        //public async Task<List<Image>> GetAllAvatarAsync()
        //{
        //    return await _imageRepository.GetAllAvatarAsync();
        //}

        public async Task<List<ImageResponse>> GetAllMyAvatarAsync(int userId)
        {
            var myAvatars = await _imageRepository.GetAllMyAvatarAsync(userId);

            return myAvatars.ToResponseList();
        }

        public async Task<ImageResponse?> GetNewestAvatarAsync(int userId)
        {
            var newestAvatar = await _imageRepository.GetNewestAvatarAsync(userId);

            return newestAvatar?.ToResponse();
        }

        public async Task<ImageResponse?> GetByIdAsync(int id)
        {
            var avatar = await _imageRepository.GetByIdAsync(id);

            return avatar?.ToResponse();
        }
    }
}