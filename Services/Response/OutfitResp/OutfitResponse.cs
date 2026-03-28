using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.OutfitResp
{
    public class OutfitResponse
    {
    }

    public class OutfitResponseDto
    {
        public int OutfitId { get; set; }
        public string? OutfitName { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<OutfitItemResponseDto> Items { get; set; } = new();
    }

    public class OutfitItemResponseDto
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public string? Slot { get; set; }
    }
}
