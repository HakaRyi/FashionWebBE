using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.AI;
using Services.Implements.PaymentService;
using Services.Request.PaymentReq;
using Services.Utils;

namespace WebAPI.Controllers
{
    [Route("api/payment")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Tạo yêu cầu thanh toán mua Gói Hội Viên (Package)
        /// </summary>
        [HttpPost("create-package-payment")]
        public async Task<IActionResult> CreatePackagePayment([FromBody] PaymentRequest request)
        {
            try
            {
                var response = await _paymentService.CreatePackagePaymentAsync(request);
                if (response == null) return BadRequest("Không thể tạo giao dịch.");

                // Lưu ý: Ở đây bạn sẽ cần tích hợp thêm logic của VnPayLibrary để tạo Link 
                // chuyển hướng sang cổng VnPay dựa trên response.OrderCode và response.Amount.
                return Ok(new
                {
                    Message = "Đã khởi tạo giao dịch mua gói.",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Tạo yêu cầu nạp tiền vào ví (Top-up)
        /// </summary>
        [HttpPost("create-topup-payment")]
        public async Task<IActionResult> CreateTopUpPayment([FromQuery] decimal amount)
        {
            try
            {
                var response = await _paymentService.CreateTopUpPaymentAsync(amount);
                if (response == null) return BadRequest("Không thể tạo giao dịch.");

                return Ok(new
                {
                    Message = "Đã khởi tạo giao dịch nạp tiền.",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cổng tiếp nhận kết quả trả về từ VnPay (IPN/Callback)
        /// URL này cần được cấu hình trong VnPay Dashboard
        /// </summary>
        [HttpGet("vnpay-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayCallback()
        {
            var queryData = Request.Query;
            string orderCode = queryData["vnp_TxnRef"]; // Mã đơn hàng mình gửi đi
            string responseCode = queryData["vnp_ResponseCode"]; // 00 là thành công

            bool isSuccess = responseCode == "00";

            var result = await _paymentService.ProcessPaymentCallbackAsync(orderCode, isSuccess);

            if (result)
            {
                return Redirect($"https://your-frontend-domain.com/payment-success?order={orderCode}");
            }

            return Redirect($"https://your-frontend-domain.com/payment-failed?order={orderCode}");
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = User.GetUserId();
            request.AccountId = userId;
            var result = await _paymentService.CreateOrderAsync(request);
            return Ok(result);
        }

        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] ZaloCallbackRequest request)
        {
            await _paymentService.HandleCallbackAsync(request);
            return Ok(new { return_code = 1 });
        }

        [HttpPost("create-order-vnpay")]
        [Authorize]
        public async Task<IActionResult> CreateOrderVnPay([FromBody] CreateOrderRequest request)
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("Id")?.Value
                              ?? User.FindFirst("AccountId")?.Value;

            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            request.AccountId = accountId;

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _paymentService.CreateVnPayOrderAsync(request, ipAddress);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            var isSuccess = await _paymentService.ProcessPaymentReturn(Request.Query);
            string status = isSuccess ? "00" : "99";

            return Content($@"
            <html><head><script>
                window.location.href = 'fashionmobile://payment-result?status={status}';
            </script></head>
            <body><h3>Đang quay lại ứng dụng...</h3></body></html>", "text/html");
        }
    }
}