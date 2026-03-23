namespace Repositories.Dto.Wallet
{
    public class TransactionFilterRequestDto
    {
        public string? Type { get; set; }
        public string? ReferenceType { get; set; }
        public string? Status { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        public string? Keyword { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}