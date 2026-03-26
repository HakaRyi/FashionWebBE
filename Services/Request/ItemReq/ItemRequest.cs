using Microsoft.AspNetCore.Http;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.ItemReq
{
    public class ItemRequest
    {
    }

    public class ProductUploadDto
    {
        public string? ItemName { get; set; }

        public int WardrobeId { get; set; }

        public string? ItemType { get; set; }

        public string? Category { get; set; }

        public string? SubCategory { get; set; }

        public string? Style { get; set; }

        public string? Gender { get; set; }

        public string? MainColor { get; set; }

        public string? SubColor { get; set; }

        public string? Material { get; set; }

        public string? Pattern { get; set; }

        public string? Fit { get; set; }

        public string? Neckline { get; set; }

        public string? SleeveLength { get; set; }

        public string? Length { get; set; }

        public string? Brand { get; set; }

        public string? Description { get; set; }

        public bool? IsPublic { get; set; }

        public ItemStatus? Status { get; set; } = ItemStatus.Active;

        public IFormFile? File { get; set; }
    }

    public class SmartRecommendationRequestDto
    {
        public string Prompt { get; set; } = string.Empty;

        public int? ReferenceItemId { get; set; }

        //(SCOPE)

        // Mặc định true: Luôn ưu tiên dùng đồ trong tủ cá nhân của user
        public bool UseMyWardrobe { get; set; } = true;

        // Mặc định true: Dùng những món đồ user đã thích và lưu lại
        public bool UseSavedItems { get; set; } = true;

        // Tùy chọn: Có muốn mượn đồ public của cộng đồng để mix không?
        public bool UseCommunityItems { get; set; } = false;

        // Giới hạn số lượng gợi ý trả về (để nhẹ tải)
        public int Limit { get; set; } = 10;
    }
}
