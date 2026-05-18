using Application.Response.ItemResp;
using Domain.Constants;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Items
{
    public class ItemAnalysisService : IItemAnalysisService
    {
        private readonly IItemRepository _itemRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ITransactionRepository _transactionRepository;

        public ItemAnalysisService(IItemRepository itemRepository, IOrderRepository orderRepository, ITransactionRepository transactionRepository)
        {
            _itemRepository = itemRepository;
            _orderRepository = orderRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task<FashionIntelligenceResp> GetMarketThroughputAsync(
    DateTime? startDate,
    DateTime? endDate,
    string? filterType = null,
    string? filterValue = null,
    string viewMode = "date") // "date" hoặc "month"
        {
            // Chuẩn hóa ViewMode
            viewMode = viewMode?.Trim().ToLower() == "month" ? "month" : "date";

            // 1. Chuẩn hóa khoảng thời gian mặc định dựa trên ViewMode
            var end = (endDate ?? DateTime.UtcNow).Date;
            // Nếu xem theo tháng, mặc định lấy 6 tháng gần nhất. Nếu xem theo ngày, lấy 7 ngày gần nhất.
            var start = startDate.HasValue
                ? startDate.Value.Date
                : (viewMode == "month" ? end.AddMonths(-5).Date : end.AddDays(-6).Date);

            // Kéo dữ liệu thô từ cơ sở dữ liệu
            var rawItems = await _itemRepository.GetAdminIntelRawDataAsync(start, end);
            var orders = await _orderRepository.GetOrdersWithDetailsAsync(start, end);

            // Lọc các đơn hàng đã hoàn thành hợp lệ
            var completedOrders = orders
                .Where(o => !string.IsNullOrEmpty(o.Status) &&
                            o.Status.Equals(OrderStatus.Completed, StringComparison.OrdinalIgnoreCase) &&
                            o.OrderDetails != null)
                .ToList();

            // Chuẩn hóa tham số lọc để tìm kiếm chính xác
            string? normType = filterType?.Trim().ToLower();
            string? normValue = filterValue?.Trim().ToLower();
            bool hasFilter = !string.IsNullOrEmpty(normType) && !string.IsNullOrEmpty(normValue);

            // -----------------------------------------------------------------
            // KHỐI LỌC ĐỘNG THEO PARAMETER
            // -----------------------------------------------------------------
            var filteredItems = rawItems.ToList();
            if (hasFilter)
            {
                switch (normType)
                {
                    case "color":
                        filteredItems = rawItems.Where(i => (i.MainColor ?? "").Trim().ToLower() == normValue).ToList();
                        break;
                    case "fabric":
                    case "material":
                        filteredItems = rawItems.Where(i => (i.Material ?? "").Trim().ToLower() == normValue).ToList();
                        break;
                    case "style":
                        filteredItems = rawItems.Where(i => (i.Style ?? "").Trim().ToLower() == normValue).ToList();
                        break;
                    case "category":
                        filteredItems = rawItems.Where(i => (i.Category ?? "").Trim().ToLower() == normValue).ToList();
                        break;
                }
            }

            var filteredOrderDetails = completedOrders
                .SelectMany(o => o.OrderDetails.Where(od => od != null && od.Item != null).Select(od => new { Order = o, Detail = od }))
                .Where(x => !hasFilter ||
                    (normType == "color" && (x.Detail.Item.MainColor ?? "").Trim().ToLower() == normValue) ||
                    ((normType == "fabric" || normType == "material") && (x.Detail.Item.Material ?? "").Trim().ToLower() == normValue) ||
                    (normType == "style" && (x.Detail.Item.Style ?? "").Trim().ToLower() == normValue) ||
                    (normType == "category" && (x.Detail.Item.Category ?? "").Trim().ToLower() == normValue)
                )
                .ToList();

            // -----------------------------------------------------------------
            // 2. XỬ LÝ TRỤC THỜI GIAN ĐỘNG (HỖ TRỢ DATE & MONTH)
            // -----------------------------------------------------------------
            // Định nghĩa định dạng Group dữ liệu tùy theo chế độ xem
            string timeFormat = viewMode == "month" ? "yyyy-MM" : "yyyy-MM-dd";

            // [Global] Uploads theo nhóm thời gian
            var globalUploadsByPeriod = rawItems
                .Where(i => i.CreatedAt.HasValue)
                .GroupBy(i => i.CreatedAt!.Value.ToString(timeFormat))
                .ToDictionary(g => g.Key, g => g.Count());

            // [Global] Sales theo nhóm thời gian
            var globalSalesByPeriod = completedOrders
                .GroupBy(o => (o.CompletedAt ?? o.CreatedAt).ToString(timeFormat))
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(o => o.OrderDetails).Where(od => od != null).Sum(od => od.Quantity)
                );

            // [Specific] Sales của sản phẩm được lọc theo nhóm thời gian
            var specificSalesByPeriod = filteredOrderDetails
                .GroupBy(x => (x.Order.CompletedAt ?? x.Order.CreatedAt).ToString(timeFormat))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Detail.Quantity));

            var timelineData = new List<AdminMarketTimelineDto>();

            int divisorFactor = rawItems.Select(i => i.Category).Distinct().Count();
            if (divisorFactor == 0) divisorFactor = 1;

            // Tiến hành dựng dữ liệu Timeline động
            if (viewMode == "date")
            {
                int totalDays = (end - start).Days + 1;
                for (int i = 0; i < totalDays; i++)
                {
                    var targetDate = start.AddDays(i);
                    string lookupKey = targetDate.ToString(timeFormat);
                    string label = targetDate.ToString("MM/dd");

                    BuildTimelineRow(lookupKey, label, globalUploadsByPeriod, globalSalesByPeriod, specificSalesByPeriod, divisorFactor, hasFilter, timelineData);
                }
            }
            else // Chế độ "month"
            {
                // Tính toán số tháng chênh lệch giữa Start và End
                int totalMonths = ((end.Year - start.Year) * 12) + end.Month - start.Month + 1;
                for (int i = 0; i < totalMonths; i++)
                {
                    var targetMonth = start.AddMonths(i);
                    string lookupKey = targetMonth.ToString(timeFormat);
                    string label = targetMonth.ToString("yyyy/MM"); // Nhãn trục biểu đồ tháng (VD: 2026/05)

                    BuildTimelineRow(lookupKey, label, globalUploadsByPeriod, globalSalesByPeriod, specificSalesByPeriod, divisorFactor, hasFilter, timelineData);
                }
            }

            // -----------------------------------------------------------------
            // 3. CHI TIẾT PHÂN PHỐI THUỘC TÍNH (Giữ nguyên logic chuẩn hóa)
            // -----------------------------------------------------------------
            var attributeData = new List<AttributeDistributionDto>();
            var colorGroups = rawItems.GroupBy(i => string.IsNullOrEmpty(i.MainColor) ? "unassigned" : i.MainColor.Trim().ToLower());
            foreach (var g in colorGroups)
            {
                var purchases = filteredOrderDetails.Where(x => (x.Detail.Item.MainColor ?? "").Trim().ToLower() == g.Key).Sum(x => x.Detail.Quantity);
                attributeData.Add(new AttributeDistributionDto { Type = "Color", Value = g.Key, TotalUploads = g.Count(), TotalPurchases = purchases });
            }

            var fabricGroups = rawItems.GroupBy(i => string.IsNullOrEmpty(i.Material) ? "unassigned" : i.Material.Trim().ToLower());
            foreach (var g in fabricGroups)
            {
                var purchases = filteredOrderDetails.Where(x => (x.Detail.Item.Material ?? "").Trim().ToLower() == g.Key).Sum(x => x.Detail.Quantity);
                attributeData.Add(new AttributeDistributionDto { Type = "Fabric", Value = g.Key, TotalUploads = g.Count(), TotalPurchases = purchases });
            }

            var styleGroups = rawItems.GroupBy(i => string.IsNullOrEmpty(i.Style) ? "unassigned" : i.Style.Trim().ToLower());
            foreach (var g in styleGroups)
            {
                var purchases = filteredOrderDetails.Where(x => (x.Detail.Item.Style ?? "").Trim().ToLower() == g.Key).Sum(x => x.Detail.Quantity);
                attributeData.Add(new AttributeDistributionDto { Type = "Style", Value = g.Key, TotalUploads = g.Count(), TotalPurchases = purchases });
            }

            var categoryGroups = rawItems.GroupBy(i => string.IsNullOrEmpty(i.Category) ? "unassigned" : i.Category.Trim().ToLower());
            foreach (var g in categoryGroups)
            {
                var purchases = filteredOrderDetails.Where(x => (x.Detail.Item.Category ?? "").Trim().ToLower() == g.Key).Sum(x => x.Detail.Quantity);
                attributeData.Add(new AttributeDistributionDto { Type = "Category", Value = g.Key, TotalUploads = g.Count(), TotalPurchases = purchases });
            }

            // -----------------------------------------------------------------
            // 5. THÊM MỚI: XỬ LÝ PIE ATTRIBUTE SHARE (ĐỘNG THEO FILTER TYPE VÀ DATE)
            // -----------------------------------------------------------------
            var attributeShareData = new List<AttributeShareDto>();

            // Xác định nhóm thuộc tính cần phân tích cho Pie (Mặc định nếu trống là phân tích thị phần Category)
            string targetPieType = !string.IsNullOrEmpty(normType) ? normType : "category";

            // Lấy danh sách các bản ghi thô thuộc loại thuộc tính này
            var targetGroup = attributeData
                .Where(a => a.Type.Equals(targetPieType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Tính tổng số lượng upload trong nhóm này để làm mẫu số tính %
            int totalGroupUploads = targetGroup.Sum(a => a.TotalUploads);

            if (totalGroupUploads > 0)
            {
                attributeShareData = targetGroup.Select(a => new AttributeShareDto
                {
                    Name = a.Value,
                    Value = a.TotalUploads,
                    Percentage = Math.Round(((decimal)a.TotalUploads / totalGroupUploads) * 100, 1),
                    // Đánh dấu nổi bật (Highlight): Nếu trùng với giá trị đang tìm kiếm thì TRUE
                    IsHighlighted = hasFilter && a.Value.Equals(normValue, StringComparison.OrdinalIgnoreCase)
                })
                .OrderByDescending(x => x.Value) // Sắp xếp từ lớn đến nhỏ để hiển thị Pie đẹp hơn
                .ToList();
            }

            // -----------------------------------------------------------------
            // 4. ĐỒNG BỘ CHỈ SỐ TỔNG HỢP KPI THỊ TRƯỜNG
            // -----------------------------------------------------------------
            int totalUploads = rawItems.Count;
            int totalSales = completedOrders.SelectMany(o => o.OrderDetails).Where(od => od != null).Sum(od => od.Quantity);
            int targetedSales = filteredOrderDetails.Sum(x => x.Detail.Quantity);

            return new FashionIntelligenceResp
            {
                Kpis = new AdminKpiDto
                {
                    TotalPlatformUploads = totalUploads,
                    GlobalItemsSold = totalSales,
                    PersonalSales = targetedSales,
                    MarketVelocity = totalUploads > 0 ? Math.Round(((decimal)totalSales / totalUploads) * 100, 1) : 0
                },
                TimelineData = timelineData,
                AttributeData = attributeData,
                AttributeShare = attributeShareData
            };
        }

        // Hàm Helper phụ trợ giúp tái sử dụng mã nguồn cho cả 2 vòng lặp (Date/Month) tránh trùng lặp code
        private void BuildTimelineRow(
            string lookupKey,
            string label,
            Dictionary<string, int> globalUploads,
            Dictionary<string, int> globalSales,
            Dictionary<string, int> specificSales,
            int divisorFactor,
            bool hasFilter,
            List<AdminMarketTimelineDto> timelineList)
        {
            globalUploads.TryGetValue(lookupKey, out var uploads);
            globalSales.TryGetValue(lookupKey, out var sales);
            specificSales.TryGetValue(lookupKey, out var specSales);

            timelineList.Add(new AdminMarketTimelineDto
            {
                Date = label, // Gán nhãn tương ứng (MM/dd hoặc yyyy/MM)
                GlobalUploads = uploads,
                GlobalSales = sales,
                SpecificValue = hasFilter ? specSales : 0,
                AverageUnits = uploads > 0 ? Math.Round((decimal)uploads / divisorFactor, 1) : 0
            });
        }
    }
}
