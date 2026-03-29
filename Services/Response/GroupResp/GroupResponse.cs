namespace Services.Response.GroupResp
{
    public class GroupResponse
    {
        public int GroupId { get; set; }

        public string? Name { get; set; }
        public string? Avatar { get; set; }

        public bool? IsGroup { get; set; } = true; // true cho group, false cho 1-1 chat

        public string? CreateBy { get; set; }
        public string? IsOnline { get; set; }

        public DateTime? CreatedAt { get; set; }
        public string LastMessage { get; set; }

    }
}
