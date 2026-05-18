using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.ItemResp
{
    public class AnalyticItemsResponse
    {
    }

    public class FashionIntelligenceResp
    {
        public AdminKpiDto Kpis { get; set; } = new();
        public List<AdminMarketTimelineDto> TimelineData { get; set; } = new();
        public List<AttributeDistributionDto> AttributeData { get; set; } = new();
        public List<AttributeShareDto> AttributeShare { get; set; } = new();
    }

    public class AdminKpiDto
    {
        public int TotalPlatformUploads { get; set; }
        public int GlobalItemsSold { get; set; }
        public int PersonalSales { get; set; } // Đại diện cho lượng bán của thuộc tính được chỉ định
        public decimal MarketVelocity { get; set; }
    }

    public class AdminMarketTimelineDto
    {
        public string Date { get; set; } = string.Empty;
        public int GlobalUploads { get; set; }
        public int GlobalSales { get; set; }
        public int SpecificValue { get; set; }  // <--- THÊM MỚI: Sản lượng của riêng đồ lọc (ví dụ: Áo thun đen)
        public decimal AverageUnits { get; set; } // <--- THÊM MỚI: Chỉ số trung bình cộng đồng trong ngày
    }

    public class AttributeDistributionDto
    {
        public string Type { get; set; } = string.Empty;       // "Color", "Fabric", "Style", "Category"
        public string Value { get; set; } = string.Empty;      // Các giá trị phân loại chữ thường đã xử lý
        public int TotalUploads { get; set; }
        public int TotalPurchases { get; set; }
    }

    public class AttributeShareDto
    {
        public string Name { get; set; } = string.Empty;       // Tên thuộc tính (Ví dụ: "black", "white", "red"...)
        public int Value { get; set; }                          // Số lượng uploads/hoặc purchases tương ứng
        public decimal Percentage { get; set; }                // % Chiếm dụng thị phần trong nhóm
        public bool IsHighlighted { get; set; }                 // FE dựa vào đây: true thì màu đậm/nổi bật, false thì mờ đi
    }
}
