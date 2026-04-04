using Application.Services.PaymentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Request.PaymentReq;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/payment")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private const string MobileDeepLinkSuccess = "fashionmobile://payment-result?status=00";
        private const string MobileDeepLinkFailed = "fashionmobile://payment-result?status=99";

        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("topup/vnpay")]
        public async Task<IActionResult> CreateTopUpVnPay([FromBody] CreateTopUpRequest request)
        {
            try
            {
                string ipAddress = GetClientIpAddress();

                var result = await _paymentService.CreateTopUpVnPayAsync(request, ipAddress);

                return Ok(new
                {
                    message = "Khởi tạo giao dịch nạp ví qua VNPAY thành công.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("topup/zalopay")]
        public async Task<IActionResult> CreateTopUpZaloPay([FromBody] CreateTopUpRequest request)
        {
            try
            {
                var result = await _paymentService.CreateTopUpZaloPayAsync(request);

                return Ok(new
                {
                    message = "Khởi tạo giao dịch nạp ví qua ZaloPay thành công.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("vnpay/return")]
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                bool isSuccess = await _paymentService.HandleVnPayReturnAsync(Request.Query);
                return BuildMobileRedirectPage(isSuccess ? MobileDeepLinkSuccess : MobileDeepLinkFailed);
            }
            catch
            {
                return BuildMobileRedirectPage(MobileDeepLinkFailed);
            }
        }

        [AllowAnonymous]
        [HttpGet("vnpay/callback")]
        public async Task<IActionResult> VnPayCallback()
        {
            try
            {
                string orderCode = Request.Query["vnp_TxnRef"];
                string responseCode = Request.Query["vnp_ResponseCode"];
                bool isSuccess = responseCode == "00";

                bool result = await _paymentService.ProcessTopUpCallbackAsync(orderCode, isSuccess);

                return Ok(new
                {
                    success = result,
                    orderCode = orderCode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("zalopay/callback")]
        public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloCallbackRequest request)
        {
            try
            {
                await _paymentService.HandleZaloPayCallbackAsync(request);
                return Ok(new { return_code = 1 });
            }
            catch
            {
                return Ok(new { return_code = 0 });
            }
        }

        private string GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

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
                    setTimeout(function() {{
                        window.location.href = '{deepLink}';
                    }}, 800);
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