using Microsoft.AspNetCore.Http;
using Services.Request.PrizeReq;



namespace Services.Request.EventReq
{
    public class EventRequest
    {
    }

    public class CreateEventRequest
    {
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime SubmissionDeadline { get; set; }

        public DateTime EndTime { get; set; }

        public double ExpertWeight { get; set; }

        public double UserWeight { get; set; }

        public double PointPerLike { get; set; }

        public double PointPerShare { get; set; }

        //[Range(2, 20, ErrorMessage = "Số lượng Expert tối thiểu để bắt đầu phải từ 2 trở lên.")]
        public int MinExpertsRequired { get; set; }

        public bool IsAutoStart { get; set; }

        public List<PrizeRequest> Prizes { get; set; } = new();

        public List<int>? InvitedExpertIds { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
