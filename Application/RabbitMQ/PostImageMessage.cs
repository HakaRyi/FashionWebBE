namespace Application.RabbitMQ
{
    public class PostImageMessage
    {
        public int PostId { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
}
