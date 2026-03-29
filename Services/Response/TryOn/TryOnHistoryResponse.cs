namespace Services.Response.TryOn
{
    public class TryOnHistoryResponse
    {
        public int TryOnId { get; set; }
        public string? ImageUrl { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
