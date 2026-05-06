using Application.Interfaces;
using Application.Request.OrderReq;
using Application.Services.OrderImp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICurrentUserService _currentUser;

        public OrderController(
            IOrderService orderService,
            ICurrentUserService currentUser)
        {
            _orderService = orderService;
            _currentUser = currentUser;
        }

        [HttpPost("{sellerId:int}")]
        public async Task<IActionResult> CreateOrder(
            int sellerId,
            [FromBody] CreateOrderRequest request)
        {
            var buyerId = _currentUser.GetRequiredUserId();
            var result = await _orderService.CreateOrderAsync(sellerId, buyerId, request);
            return Ok(result);
        }

        [HttpPost("{orderId:int}/pay")]
        public async Task<IActionResult> PayOrder(int orderId)
        {
            var buyerId = _currentUser.GetRequiredUserId();
            var result = await _orderService.PayOrderWithWalletAsync(orderId, buyerId);
            return Ok(result);
        }

        [HttpGet("paid")]
        public async Task<IActionResult> GetPaidOrders()
        {
            var result = await _orderService.GetPaidOrdersAsync();
            return Ok(result);
        }

        [HttpGet("shipping")]
        public async Task<IActionResult> GetShippingOrders()
        {
            var result = await _orderService.GetShippingOrdersAsync();
            return Ok(result);
        }

        [HttpGet("completed")]
        public async Task<IActionResult> GetCompletedOrders()
        {
            var result = await _orderService.GetCompletedOrdersAsync();
            return Ok(result);
        }

        [HttpGet("cancelled")]
        public async Task<IActionResult> GetCancelledOrders()
        {
            var result = await _orderService.GetCancelledOrdersAsync();
            return Ok(result);
        }

        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var result = await _orderService.GetOrderDetailByIdAsync(orderId);
            return Ok(result);
        }

        [HttpGet("me/purchases")]
        public async Task<IActionResult> GetMyPurchases()
        {
            var buyerId = _currentUser.GetRequiredUserId();
            var result = await _orderService.GetPurchasesOrdersAsync(buyerId);
            return Ok(result);
        }

        [HttpGet("me/sales")]
        public async Task<IActionResult> GetMySales()
        {
            var sellerId = _currentUser.GetRequiredUserId();
            var result = await _orderService.GetSalesOrdersAsync(sellerId);
            return Ok(result);
        }

        [HttpPut("{orderId:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int orderId,
            [FromQuery] string status)
        {
            var userId = _currentUser.GetRequiredUserId();
            var result = await _orderService.UpdateOrderStatusAsync(orderId, status, userId);
            return Ok(result);
        }

        [HttpPut("{orderId:int}/shipper")]
        public async Task<IActionResult> ShipperUpdate(
            int orderId,
            [FromQuery] string status)
        {
            var result = await _orderService.UpdateOrderStatusByShipperAsync(orderId, status);
            return Ok(result);
        }

        [HttpPost("{orderId:int}/refund")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateRefund(
            int orderId,
            [FromForm] CreateRefundRequestDto request)
        {
            var buyerId = _currentUser.GetRequiredUserId();

            var result = await _orderService.CreateRefundRequestAsync(
                orderId,
                buyerId,
                request);

            return Ok(result);
        }

        [HttpGet("refunds")]
        public async Task<IActionResult> GetAllRefunds()
        {
            var result = await _orderService.GetAllRefundRequestsAsync();
            return Ok(result);
        }

        [HttpGet("refunds/me")]
        public async Task<IActionResult> GetMyRefunds()
        {
            var buyerId = _currentUser.GetRequiredUserId();
            var result = await _orderService.GetMyRefundRequestsAsync(buyerId);
            return Ok(result);
        }

        [HttpPut("{orderId:int}/refund/approve")]
        public async Task<IActionResult> ApproveRefund(int orderId)
        {
            var result = await _orderService.UpdateRefundStatus(orderId);
            return Ok(result);
        }

        [HttpPut("{orderId:int}/refund/reject")]
        public async Task<IActionResult> RejectRefund(
            int orderId,
            [FromQuery] string note)
        {
            var result = await _orderService.RejectRefundAsync(orderId, note);
            return Ok(result);
        }

        [HttpGet("delivered")]
        public async Task<IActionResult> GetDeliveredOrders()
        {
            var result = await _orderService.GetDeliveredOrdersAsync();
            return Ok(result);
        }

        [HttpGet("purchases/me")]
        public async Task<IActionResult> GetMyPurchaseOrders(
    [FromQuery] OrderFilterRequest request)
        {
            var buyerId = _currentUser.GetRequiredUserId();

            var result = await _orderService.GetMyPurchasesFilteredAsync(
                buyerId,
                request);

            return Ok(result);
        }

        [HttpGet("sales/me")]
        public async Task<IActionResult> GetMySalesOrders(
            [FromQuery] OrderFilterRequest request)
        {
            var sellerId = _currentUser.GetRequiredUserId();

            var result = await _orderService.GetMySalesFilteredAsync(
                sellerId,
                request);

            return Ok(result);
        }
    }
}