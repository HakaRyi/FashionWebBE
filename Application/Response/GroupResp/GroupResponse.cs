namespace Application.Response.GroupResp
{
    public class GroupResponse
    {
        public int GroupId { get; set; }

        public string? Name { get; set; }
        public string? Avatar { get; set; }

        public bool? IsGroup { get; set; } = true; // true cho group, false cho 1-1 chat

        public string? CreateBy { get; set; }
        public string? IsOnline { get; set; }
        public int? OtherUserId { get; set; }    

        public DateTime? CreatedAt { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }

    }
    public class PhotoInGroupResponse
    {
        public int PhotoId { get; set; }
        public string Url { get; set; }
        public int? GroupId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string AccountAvatar { get; set; }
        public DateTime? CreatedAt { get; set; }

    }
}
