using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.OrderImp;
using Services.Request.OrderReq;
using Services.Utils;
using System.Security.Claims;

namespace WebAPIs.Controllers
{
    [Route("api/orders")]
    [ApiController]
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
            var sellerIdClaim = User.GetUserId().ToString();

            if (!int.TryParse(sellerIdClaim, out int sellerId))
            {
                return Unauthorized();
            }

            var result = await _orderService.CreateOrderAsync(sellerId, request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                 ?? User.FindFirst("Id")?.Value;

            if (!int.TryParse(accountIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _orderService.GetOrderByIdAsync(id, currentUserId);

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
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                 ?? User.FindFirst("Id")?.Value;

            if (!int.TryParse(accountIdClaim, out int sellerId))
            {
                return Unauthorized();
            }

            var result = await _orderService.GetSalesOrdersAsync(sellerId);
            return Ok(result);
        }

        [HttpGet("purchases")]
        public async Task<IActionResult> GetPurchasesOrders()
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                 ?? User.FindFirst("Id")?.Value;

            if (!int.TryParse(accountIdClaim, out int buyerId))
            {
                return Unauthorized();
            }

            var result = await _orderService.GetPurchasesOrdersAsync(buyerId);
            return Ok(result);
        }

        /* api này tui dùng để đôi trạng thái đơn hàng theo các nút bấm trên giao diện */
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (!int.TryParse(accountIdClaim, out int currentUserId)) return Unauthorized();

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
        /* Tui có 1 màn hình dùng ví để thanh toán hóa đơn, api này dùng ở màn hình đó */
        [HttpPost("{id}/pay")]
        public async Task<IActionResult> PayOrder(int id)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("Id")?.Value;
            if (!int.TryParse(accountIdClaim, out int buyerId)) return Unauthorized();

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
    }
}
