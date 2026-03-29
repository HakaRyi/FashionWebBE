namespace Services.Response.WardrobeResp
{
    public class WardrobeResponse
    {
        public int WardrobeId { get; set; }

        public int AccountId { get; set; }

        public string? Name { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
