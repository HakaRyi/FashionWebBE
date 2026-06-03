using Application.Response.ItemResp;
using Domain.Constants;
using Domain.Interfaces;
using Domain.Entities;
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

        public ItemAnalysisService(
            IItemRepository itemRepository,
            IOrderRepository orderRepository,
            ITransactionRepository transactionRepository)
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
            string viewMode = "date")
        {
            // Chuẩn hóa ViewMode
            viewMode = viewMode?.Trim().ToLower() == "month" ? "month" : "date";

            // 1. Chuẩn hóa khoảng thời gian mặc định dựa trên ViewMode
            var end = (endDate ?? DateTime.UtcNow).Date;
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

            // Trích xuất map giữa OrderId và Thời điểm Hoàn thành từ StatusHistories
            var orderCompletedDates = completedOrders.ToDictionary(
                o => o.OrderId,
                o => o.StatusHistories
                        .Where(h => h.Status.Equals(OrderStatus.Completed, StringComparison.OrdinalIgnoreCase))
                        .Select(h => h.ChangedAt)
                        .FirstOrDefault() == default
                        ? o.CreatedAt
                        : o.StatusHistories.First(h => h.Status.Equals(OrderStatus.Completed, StringComparison.OrdinalIgnoreCase)).ChangedAt
            );

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
            // 2. XỬ LÝ TRỤC THỜI GIAN ĐỘNG (ĐÃ SỬA ĐỂ KHÔNG BỊ LỖI KIỂU DỮ LIỆU)
            // -----------------------------------------------------------------
            string timeFormat = viewMode == "month" ? "yyyy-MM" : "yyyy-MM-dd";

            // Thêm ?? "" để đảm bảo Key trả về luôn là `string` chứ không phải `string?`
            var globalUploadsByPeriod = rawItems
                .Where(i => i.CreatedAt.HasValue)
                .GroupBy(i => i.CreatedAt!.Value.ToString(timeFormat) ?? "")
                .ToDictionary(g => g.Key, g => g.Count());

            var globalSalesByPeriod = completedOrders
                .GroupBy(o => {
                    var completionDate = orderCompletedDates.TryGetValue(o.OrderId, out var dt) ? dt : o.CreatedAt;
                    return completionDate.ToString(timeFormat) ?? "";
                })
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(o => o.OrderDetails).Where(od => od != null).Sum(od => od.Quantity)
                );

            var specificSalesByPeriod = filteredOrderDetails
                .GroupBy(x => {
                    var completionDate = orderCompletedDates.TryGetValue(x.Order.OrderId, out var dt) ? dt : x.Order.CreatedAt;
                    return completionDate.ToString(timeFormat) ?? "";
                })
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Detail.Quantity));

            var timelineData = new List<AdminMarketTimelineDto>();

            int divisorFactor = rawItems.Select(i => i.Category).Distinct().Count();
            if (divisorFactor == 0) divisorFactor = 1;

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
            else
            {
                int totalMonths = ((end.Year - start.Year) * 12) + end.Month - start.Month + 1;
                for (int i = 0; i < totalMonths; i++)
                {
                    var targetMonth = start.AddMonths(i);
                    string lookupKey = targetMonth.ToString(timeFormat);
                    string label = targetMonth.ToString("yyyy/MM");

                    BuildTimelineRow(lookupKey, label, globalUploadsByPeriod, globalSalesByPeriod, specificSalesByPeriod, divisorFactor, hasFilter, timelineData);
                }
            }

            // -----------------------------------------------------------------
            // 3. CHI TIẾT PHÂN PHỐI THUỘC TÍNH
            // -----------------------------------------------------------------
            var attributeData = new List<AttributeDistributionDto>();

            var purchasesByColor = filteredOrderDetails.GroupBy(x => (x.Detail.Item.MainColor ?? "").Trim().ToLower()).ToDictionary(g => g.Key, g => g.Sum(x => x.Detail.Quantity));
            var purchasesByFabric = filteredOrderDetails.GroupBy(x => (x.Detail.Item.Material ?? "").Trim().ToLower()).ToDictionary(g => g.Key, g => g.Sum(x => x.Detail.Quantity));
            var purchasesByStyle = filteredOrderDetails.GroupBy(x => (x.Detail.Item.Style ?? "").Trim().ToLower()).ToDictionary(g => g.Key, g => g.Sum(x => x.Detail.Quantity));
            var purchasesByCategory = filteredOrderDetails.GroupBy(x => (x.Detail.Item.Category ?? "").Trim().ToLower()).ToDictionary(g => g.Key, g => g.Sum(x => x.Detail.Quantity));

            var colorGroups = rawItems.GroupBy(i => string.IsNullOrEmpty(i.MainColor) ? "unassigned" : i.MainColor.Trim().ToLower());
            foreach (var g in colorGroups)
            {
                purchasesByColor.TryGetValue(g.Key, out var purchases);
                attributeData.Add(new AttributeDistributionDto { Type = "Color", Value = g.Key, TotalUploads = g.Count(), TotalPurchases = purchases });
            }

            var fabricGroups = rawItems.GroupBy(i => string.IsNullOrEmpty(i.Material) ? "unassigned" : i.Material.Trim().ToLower());
            foreach (var g in fabricGroups)
            {
                purchasesByFabric.TryGetValue(g.Key, out var purchases);
                attributeData.Add(new AttributeDistributionDto { Type = "Fabric", Value = g.Key, TotalUploads = g.Count(), TotalPurchases = purchases });
            }

            var styleGroups = rawItems.GroupBy(i => string.IsNullOrEmpty(i.Style) ? "unassigned" : i.Style.Trim().ToLower());
            foreach (var g in styleGroups)
            {
                purchasesByStyle.TryGetValue(g.Key, out var purchases);
                attributeData.Add(new AttributeDistributionDto { Type = "Style", Value = g.Key, TotalUploads = g.Count(), TotalPurchases = purchases });
            }

            var categoryGroups = rawItems.GroupBy(i => string.IsNullOrEmpty(i.Category) ? "unassigned" : i.Category.Trim().ToLower());
            foreach (var g in categoryGroups)
            {
                purchasesByCategory.TryGetValue(g.Key, out var purchases);
                attributeData.Add(new AttributeDistributionDto { Type = "Category", Value = g.Key, TotalUploads = g.Count(), TotalPurchases = purchases });
            }

            // -----------------------------------------------------------------
            // 4. XỬ LÝ PIE ATTRIBUTE SHARE
            // -----------------------------------------------------------------
            var attributeShareData = new List<AttributeShareDto>();
            string targetPieType = !string.IsNullOrEmpty(normType) ? normType : "category";

            var targetGroup = attributeData
                .Where(a => a.Type.Equals(targetPieType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            int totalGroupUploads = targetGroup.Sum(a => a.TotalUploads);

            if (totalGroupUploads > 0)
            {
                attributeShareData = targetGroup.Select(a => new AttributeShareDto
                {
                    Name = a.Value,
                    Value = a.TotalUploads,
                    Percentage = Math.Round(((decimal)a.TotalUploads / totalGroupUploads) * 100, 1),
                    IsHighlighted = hasFilter && a.Value.Equals(normValue, StringComparison.OrdinalIgnoreCase)
                })
                .OrderByDescending(x => x.Value)
                .ToList();
            }

            // -----------------------------------------------------------------
            // 5. ĐỒNG BỘ CHỈ SỐ TỔNG HỢP KPI THỊ TRƯỜNG
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
                Date = label,
                GlobalUploads = uploads,
                GlobalSales = sales,
                SpecificValue = hasFilter ? specSales : 0,
                AverageUnits = uploads > 0 ? Math.Round((decimal)uploads / divisorFactor, 1) : 0
            });
        }
    }
}