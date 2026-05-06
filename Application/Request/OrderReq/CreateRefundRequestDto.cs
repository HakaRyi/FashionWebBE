using Microsoft.AspNetCore.Http;

namespace Application.Request.OrderReq
{
    public class CreateRefundRequestDto
    {
        public string Reason { get; set; } = string.Empty;

        public IFormFile ProofImage1 { get; set; } = null!;

        public IFormFile? ProofImage2 { get; set; }
    }
}