namespace Application.Response.DashboardResp
{
    public class AnalyticsDashboardResponse
    {
        public List<StatCardDto> Stats { get; set; } = new();
        public List<TopEventDto> TopEvents { get; set; } = new();
        public List<ChartDataDto> ChartData { get; set; } = new();
    }

    public class StatCardDto
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Change { get; set; } = "";
        public bool IsUp { get; set; }
    }

    public class TopEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public int Posts { get; set; }
        public int Rated { get; set; }
        public string Engagements { get; set; } = "";
    }

    public class ChartDataDto
    {
        public string Date { get; set; } = "";
        public int Submissions { get; set; }
        public int Engagements { get; set; }
    }
}
