namespace Application.RabbitMQ
{
    public class ChatMessageQueueDto
    {
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; }
        public List<string> ImageUrls { get; set; }
        public int? ReplyToId { get; set; }
    }
}
