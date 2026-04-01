using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.Auth;
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
        private const string MobileDeepLinkSuccess = "fashionmobile://payment-result?status=00";
        private const string MobileDeepLinkFailed = "fashionmobile://payment-result?status=99";

        private readonly IPaymentService _paymentService;
        private readonly ICurrentUserService _currentUserService;

        public PaymentController(
            IPaymentService paymentService,
            ICurrentUserService currentUserService)
        {
            _paymentService = paymentService;
            _currentUserService = currentUserService;
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
                string ipAddress = GetClientIpAddress();

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
                bool isSuccess = await _paymentService.ProcessPaymentReturn(Request.Query);
                return BuildMobileRedirectPage(isSuccess ? MobileDeepLinkSuccess : MobileDeepLinkFailed);
            }
            catch
            {
                return BuildMobileRedirectPage(MobileDeepLinkFailed);
            }
        }

        private bool TryGetCurrentUserId(out int accountId)
        {
            accountId = 0;

            try
            {
                accountId = _currentUserService.GetRequiredUserId();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        private ContentResult BuildMobileRedirectPage(string deepLink)
        {
            var html = $@"
            <!DOCTYPE html>
            <html lang=""vi"">
            <head>
                <meta charset=""utf-8"" />
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                <title>Đang quay lại ứng dụng...</title>
                <script>
                    window.location.href = '{deepLink}';
                </script>
            </head>
            <body>
                <h3>Đang quay lại ứng dụng...</h3>
            </body>
            </html>";
            return Content(html, "text/html");
        }
    }
}