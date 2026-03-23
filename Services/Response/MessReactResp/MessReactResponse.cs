namespace Services.Response.MessReactResp
{
    public class MessReactResponse
    {
        public int ReactId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string ReactType { get; set; } = string.Empty;
        public int MessageId { get; set; }
    }
}
