using Repositories.Dto.Social;
using Repositories.Entities;

namespace Services.Implements.ImageImp
{
    public interface IImageService
    {
        Task<ImageResponse> CreateAvatarImageAsync(int userId, string imageUrl);
        Task<List<ImageResponse>> CreatePostImagesAsync(int postId, List<string> imageUrls);
        Task<List<ImageResponse>> CreateItemImagesAsync(int itemId, List<string> imageUrls);
        Task DeleteImageAsync(int imageId);
        Task DeleteImagesAsync(List<int> imageIds);
        //Task<List<Image>> GetAllAvatarAsync();
        Task<List<ImageResponse>> GetAllMyAvatarAsync(int userId);
        Task<ImageResponse?> GetNewestAvatarAsync(int userId);
        Task<ImageResponse?> GetByIdAsync(int id);
    }
}