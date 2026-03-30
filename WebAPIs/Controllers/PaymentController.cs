using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.PaymentService;
using Services.Request.PaymentReq;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/payment")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("create-package-payment")]
        public async Task<IActionResult> CreatePackagePayment([FromBody] PaymentRequest request)
        {
            try
            {
                var response = await _paymentService.CreatePackagePaymentAsync(request);
                if (response == null)
                    return BadRequest(new { message = "Không thể tạo giao dịch." });

                return Ok(new
                {
                    message = "Đã khởi tạo giao dịch mua gói.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create-topup-payment")]
        public async Task<IActionResult> CreateTopUpPayment([FromQuery] decimal amount)
        {
            try
            {
                var response = await _paymentService.CreateTopUpPaymentAsync(amount);
                if (response == null)
                    return BadRequest(new { message = "Không thể tạo giao dịch." });

                return Ok(new
                {
                    message = "Đã khởi tạo giao dịch nạp tiền.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("vnpay-callback")]
        public async Task<IActionResult> VnPayCallback()
        {
            try
            {
                string orderCode = Request.Query["vnp_TxnRef"];
                string responseCode = Request.Query["vnp_ResponseCode"];
                bool isSuccess = responseCode == "00";

                var result = await _paymentService.ProcessPaymentCallbackAsync(orderCode, isSuccess);

                if (result)
                    return Redirect($"https://your-frontend-domain.com/payment-success?order={orderCode}");

                return Redirect($"https://your-frontend-domain.com/payment-failed?order={orderCode}");
            }
            catch
            {
                return Redirect("https://your-frontend-domain.com/payment-failed");
            }
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                request.AccountId = User.GetUserId();
                var result = await _paymentService.CreateOrderAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] ZaloCallbackRequest request)
        {
            try
            {
                await _paymentService.HandleCallbackAsync(request);
                return Ok(new { return_code = 1 });
            }
            catch
            {
                return Ok(new { return_code = 0 });
            }
        }

        [HttpPost("create-order-vnpay")]
        public async Task<IActionResult> CreateOrderVnPay([FromBody] CreateOrderRequest request)
        {
            if (!TryGetCurrentUserId(out int accountId))
                return Unauthorized(new { message = "Unauthorized" });

            try
            {
                request.AccountId = accountId;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var result = await _paymentService.CreateVnPayOrderAsync(request, ipAddress);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                var isSuccess = await _paymentService.ProcessPaymentReturn(Request.Query);
                string status = isSuccess ? "00" : "99";

                return Content($@"
                <html>
                <head>
                    <script>
                        window.location.href = 'fashionmobile://payment-result?status={status}';
                    </script>
                </head>
                <body>
                    <h3>Đang quay lại ứng dụng...</h3>
                </body>
                </html>", "text/html");
            }
            catch
            {
                return Content(@"
                <html>
                <head>
                    <script>
                        window.location.href = 'fashionmobile://payment-result?status=99';
                    </script>
                </head>
                <body>
                    <h3>Đang quay lại ứng dụng...</h3>
                </body>
                </html>", "text/html");
            }
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var claimValue =
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("Id")?.Value ??
                User.FindFirst("AccountId")?.Value;

            return int.TryParse(claimValue, out userId);
        }
    }
}