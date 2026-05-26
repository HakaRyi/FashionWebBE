namespace Domain.Contracts.Admin
{
    public class StatusCountDto
    {
        public string Status { get; set; } = null!;

        public int Count { get; set; }
    }

    public class TypeCountDto
    {
        public string Type { get; set; } = null!;

        public int Count { get; set; }
    }

    public class DateCountDto
    {
        public DateTime Date { get; set; }

        public int Count { get; set; }
    }
}