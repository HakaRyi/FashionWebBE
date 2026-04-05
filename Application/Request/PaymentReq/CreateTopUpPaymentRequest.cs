using System.ComponentModel.DataAnnotations;

namespace Application.Request.PaymentReq
{
    public class CreateTopUpPaymentRequest
    {
        [Range(10000, double.MaxValue, ErrorMessage = "Số tiền nạp tối thiểu là 10,000đ.")]
        public decimal Amount { get; set; }

        [Required]
        public string Provider { get; set; } = null!;
    }
}