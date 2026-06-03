using Microsoft.EntityFrameworkCore;
using Domain.Constants;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly FashionDbContext _context;

        public OrderRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetPendingPaymentOrdersBeforeAsync(DateTime deadline)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o =>
                    o.Status == OrderStatus.PendingPayment &&
                    o.CreatedAt <= deadline)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order> CreateAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            return order;
        }

        public Order Update(Order order)
        {
            _context.Orders.Update(order);
            return order;
        }

        public IQueryable<Order> Query()
        {
            return _context.Orders.AsQueryable();
        }

        public async Task<Order?> GetByIdAsync(int orderId)
        {
            return await BuildOrderDetailQuery(isTracking: true).Include(o => o.StatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetByIdNoTrackingAsync(int orderId)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<Order>> GetOrdersBySellerIdAsync(int sellerId)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .Where(o => o.SellerId == sellerId)
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByBuyerIdAsync(int buyerId)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .Where(o => o.BuyerId == buyerId)
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByStatusAsync(string status)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByStatusesAsync(params string[] statuses)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .Where(o => statuses.Contains(o.Status))
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetPaidOrdersAsync()
        {
            return await GetByStatusAsync(OrderStatus.Processing);
        }

        public async Task<List<Order>> GetCompletedOrdersAsync()
        {
            return await GetByStatusAsync(OrderStatus.Completed);
        }

        public async Task<List<Order>> GetDeliveredOrdersAsync()
        {
            return await GetByStatusAsync(OrderStatus.Delivered);
        }

        public async Task<List<Order>> GetCancelledOrdersAsync()
        {
            return await GetByStatusesAsync(OrderStatus.Cancelled, OrderStatus.Refunded);
        }

        public async Task<List<Order>> GetShippingOrdersAsync()
        {
            return await GetByStatusAsync(OrderStatus.Shipping);
        }

        public async Task<bool> ExistsPendingOrderByBuyerAsync(int buyerId, int itemVariantId)
        {
            return await _context.Orders
                .AsNoTracking()
                .AnyAsync(o =>
                    o.BuyerId == buyerId &&
                    (o.Status == OrderStatus.PendingPayment ||
                     o.Status == OrderStatus.Processing ||
                     o.Status == OrderStatus.Shipping ||
                     o.Status == OrderStatus.Delivered ||
                     o.Status == OrderStatus.Completed) &&
                    o.OrderDetails.Any(d => d.ItemVariantId == itemVariantId));
        }

        private IQueryable<Order> BuildOrderDetailQuery(bool isTracking)
        {
            IQueryable<Order> query = _context.Orders;

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.EscrowSession)
                .Include(o => o.RefundRequest)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Item)
                        .ThenInclude(i => i.Images)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ItemVariant);
        }

        public async Task<List<Order>> GetDeliveredOrdersBeforeAsync(DateTime deadline)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.EscrowSession)
                .Where(o =>
                    o.Status == OrderStatus.Delivered &&
                    o.StatusHistories.Any(h => h.Status == OrderStatus.Delivered && h.ChangedAt <= deadline))
                .ToListAsync();
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetOrdersByBuyerIdFilteredAsync(
    int buyerId,
    int page,
    int pageSize,
    string? status,
    DateTime? fromDate,
    DateTime? toDate,
    string? sellerName,
    string? orderCode)
        {
            var query = BuildOrderDetailQuery(isTracking: false)
                .Where(o => o.BuyerId == buyerId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status.Trim());
            }

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(o => o.CreatedAt < endDate);
            }

            if (!string.IsNullOrWhiteSpace(sellerName))
            {
                string keyword = sellerName.Trim().ToLower();

                query = query.Where(o =>
                    o.Seller != null &&
                    o.Seller.UserName.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                string keyword = orderCode.Trim().ToLower();

                query = query.Where(o =>
                    o.OrderCode.ToLower().Contains(keyword));
            }

            int totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetOrdersBySellerIdFilteredAsync(
            int sellerId,
            int page,
            int pageSize,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            string? buyerName,
            string? orderCode)
        {
            var query = BuildOrderDetailQuery(isTracking: false)
                .Where(o => o.SellerId == sellerId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status.Trim());
            }

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(o => o.CreatedAt < endDate);
            }

            if (!string.IsNullOrWhiteSpace(buyerName))
            {
                string keyword = buyerName.Trim().ToLower();

                query = query.Where(o =>
                    o.Buyer != null &&
                    o.Buyer.UserName.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                string keyword = orderCode.Trim().ToLower();

                query = query.Where(o =>
                    o.OrderCode.ToLower().Contains(keyword));
            }

            int totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetPagedOrdersForAdminAsync(int pageNumber, int pageSize, string? status, string? search)
        {
            var query = _context.Set<Order>()
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status.ToLower() == status.ToLower());
            }

            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLower();
                query = query.Where(o =>
                    (o.OrderCode != null && o.OrderCode.ToLower().Contains(searchLower)) ||
                    (o.ReceiverName != null && o.ReceiverName.ToLower().Contains(searchLower)) ||
                    (o.Buyer != null && o.Buyer.UserName != null && o.Buyer.UserName.ToLower().Contains(searchLower)) ||
                    (o.Seller != null && o.Seller.UserName != null && o.Seller.UserName.ToLower().Contains(searchLower))
                );
            }

            int totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<Order?> GetOrderWithDetailsByCodeAsync(string orderCode)
        {
            // 1. Tách và phân tích chuỗi orderCode ngay trên bộ nhớ C#
            int? parsedOrderId = null;

            // Nếu Admin truyền vào format "ORD-123", ta bóc tách lấy con số 123
            if (!string.IsNullOrEmpty(orderCode) && orderCode.StartsWith("ORD-", StringComparison.OrdinalIgnoreCase))
            {
                string idPart = orderCode.Substring(4); // Lấy phần chuỗi sau "ORD-"
                if (int.TryParse(idPart, out int id))
                {
                    parsedOrderId = id;
                }
            }
            // Nếu Admin truyền thẳng số "123" vào ô tìm kiếm
            else if (int.TryParse(orderCode, out int id))
            {
                parsedOrderId = id;
            }

            // 2. Thực hiện Query xuống Database bằng các kiểu dữ liệu nguyên bản (Ngăn lỗi Translation)
            return await _context.Set<Order>()
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.RefundRequest)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Item)
                    .ThenInclude(i => i.Images)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ItemVariant)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode || (parsedOrderId.HasValue && o.OrderId == parsedOrderId.Value));
        }

        public async Task<List<Order>> GetOrdersWithDetailsAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Item)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();
        }
    }
}