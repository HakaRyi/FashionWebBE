namespace Services.Response.WalletResp
{
    public class WalletResponse
    {
        public int WalletId { get; set; }
        public decimal Balance { get; set; }
        public string? Currency { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
