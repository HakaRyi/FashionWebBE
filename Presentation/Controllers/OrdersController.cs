using Application.Request.OrderReq;
using Application.Request.RefundReq;
using Application.Services.OrderImp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!TryGetCurrentUserId(out int sellerId))
                return Unauthorized();

            try
            {
                var result = await _orderService.CreateOrderAsync(sellerId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized();

            try
            {
                var result = await _orderService.GetOrderByIdAsync(id, currentUserId);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetOrderDetailById(int id)
        {
            try
            {
                var result = await _orderService.GetOrderDetailByIdAsync(id);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /*Tui Chia Phần Đơn hàng ra làm 2 tab
         1 Tab sài api sales để lấy đơn hàng đã bán
         1 Tab sài api purchares để lấy đơn hàng đã mua
         */
        [HttpGet("sales")]
        public async Task<IActionResult> GetSalesOrders()
        {
            if (!TryGetCurrentUserId(out int sellerId))
                return Unauthorized();

            try
            {
                var result = await _orderService.GetSalesOrdersAsync(sellerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("purchases")]
        public async Task<IActionResult> GetPurchasesOrders()
        {
            if (!TryGetCurrentUserId(out int buyerId))
                return Unauthorized();

            try
            {
                var result = await _orderService.GetPurchasesOrdersAsync(buyerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized();

            try
            {
                var result = await _orderService.UpdateOrderStatusAsync(id, request.Status, currentUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPut("shipper/{id}/status")]
        public async Task<IActionResult> UpdateOrderByShipperStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var result = await _orderService.UpdateOrderStatusByShipperAsync(id, request.Status);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        /* Tui có 1 màn hình dùng ví để thanh toán hóa đơn, api này dùng ở màn hình đó */
        [HttpPost("{id}/pay")]
        public async Task<IActionResult> PayOrder(int id)
        {
            if (!TryGetCurrentUserId(out int buyerId))
                return Unauthorized();

            try
            {
                var result = await _orderService.PayOrderWithWalletAsync(id, buyerId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var claimValue =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("Id")?.Value ??
                User.FindFirst("AccountId")?.Value;

            return int.TryParse(claimValue, out userId);
        }

        [AllowAnonymous]
        [HttpGet("paid")]
        public async Task<IActionResult> GetPaidOrders()
        {
            try
            {
                var result = await _orderService.GetPaidOrdersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("completed")]
        public async Task<IActionResult> GetCompletedOrders()
        {
            try
            {
                var result = await _orderService.GetCompletedOrdersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("cancelled")]
        public async Task<IActionResult> GetCancelledOrders()
        {
            try
            {
                var result = await _orderService.GetCancelledOrdersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("shipping")]
        public async Task<IActionResult> GetShippingOrders()
        {
            try
            {
                var result = await _orderService.GetShippingOrdersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/refund-request")]
        public async Task<IActionResult> CreateRefundRequest(int id, [FromBody] CreateRefundRequest dto)
        {
            if (!TryGetCurrentUserId(out int buyerId))
                return Unauthorized();

            try
            {
                var result = await _orderService.CreateRefundRequestAsync(
                    id, buyerId, dto.Reason, dto.ProofImage1, dto.ProofImage2);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/process-refund")]
        public async Task<IActionResult> ProcessRefund(int id)
        {
            try
            {
                var result = await _orderService.UpdateRefundStatus(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("refund-requests")]
        public async Task<IActionResult> GetAllRefundRequests()
        {
            try
            {
                var result = await _orderService.GetAllRefundRequestsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/reject-refund")]
        public async Task<IActionResult> RejectRefund(int id, [FromBody] RejectRefundRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AdminNote))
                    return BadRequest(new { message = "Vui lòng cung cấp lý do từ chối." });

                var result = await _orderService.RejectRefundAsync(id, request.AdminNote);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-refunds")]
        public async Task<IActionResult> GetMyRefundRequests()
        {
            if (!TryGetCurrentUserId(out int buyerId))
                return Unauthorized();

            try
            {
                var result = await _orderService.GetMyRefundRequestsAsync(buyerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}