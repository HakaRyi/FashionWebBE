using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.RecommendationResp
{
    public class RecommendationHistoryResponseDto
    {
        public int Id { get; set; }
        public string? Prompt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ReferenceItemId { get; set; }
        public string? ReferenceItemName { get; set; }
        public string? ReferenceItemImage { get; set; }
    }

    public class RecommendationDetailResponseDto
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemType { get; set; } // Thêm
        public string? Category { get; set; }
        public string? SubCategory { get; set; } // Thêm
        public string? Style { get; set; } // Thêm
        public string? Gender { get; set; } // Thêm
        public string? MainColor { get; set; }
        public string? SubColor { get; set; } // Thêm
        public string? Material { get; set; } // Thêm
        public string? Pattern { get; set; } // Thêm
        public string? Fit { get; set; } // Thêm
        public string? Neckline { get; set; } // Thêm
        public string? SleeveLength { get; set; } // Thêm
        public string? Size { get; set; } // Thêm
        public string? Length { get; set; } // Thêm
        public string? Brand { get; set; } // Thêm
        public string? Description { get; set; } // Thêm
        public bool IsPublic { get; set; } // Thêm
        public int Status { get; set; } // Thêm

        // Đổi tên thành PrimaryImageUrl cho giống API GetMyItems
        public string? PrimaryImageUrl { get; set; }

        public DateTime? CreatedAt { get; set; } // Thêm
        public DateTime? UpdateAt { get; set; } // Thêm
    }
}
