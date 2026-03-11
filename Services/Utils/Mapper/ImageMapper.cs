using Repositories.Dto.Response;
using Repositories.Entities;

namespace Services.Utils.Mapper
{
    public static class ImageMapper
    {
        public static ImageResponse ToResponse(this Image image)
        {
            return new ImageResponse
            {
                ImageId = image.ImageId,
                Url = image.ImageUrl,
                CreatedAt = image.CreatedAt ?? DateTime.UtcNow
            };
        }

        public static List<ImageResponse> ToResponseList(this List<Image> images)
        {
            return images.Select(i => i.ToResponse()).ToList();
        }
    }
}