namespace Services.Response.DashboardResp
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
        public string Views { get; set; } = "";
        public int Progress { get; set; }
    }

    public class ChartDataDto
    {
        public string Name { get; set; } = "";
        public double Value { get; set; }
    }
}
