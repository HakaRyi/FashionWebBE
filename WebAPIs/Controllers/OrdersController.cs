using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.OrderImp;
using Services.Request.OrderReq;
using Services.Utils;
using System.Security.Claims;

namespace WebAPIs.Controllers
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

        [HttpPost("{id:int}/pay")]
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
    }
}