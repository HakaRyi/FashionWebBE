namespace Repositories.Dto.Response
{
    public class ImageResponse
    {
        public int ImageId { get; set; }
        public string Url { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
