using System.ComponentModel.DataAnnotations;

namespace Services.Request.PaymentReq
{
    public class CreateOrderRequest
    {
        public int AccountId { get; set; }

        [Range(10000, double.MaxValue, ErrorMessage = "Số tiền tối thiểu là 10,000đ.")]
        public decimal Amount { get; set; }
    }
}