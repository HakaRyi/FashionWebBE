namespace Application.Request.UserReportReq
{
    public class CreateUserReportRequestDto
    {
        public int PostId { get; set; }
        public int ReportTypeId { get; set; }
        public string? Reason { get; set; }
    }
}