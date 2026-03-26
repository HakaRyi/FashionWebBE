using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.OufitReq
{
    public class OutfitRequest
    {
    }

    public class SaveOutfitRequestDto
    {
        public string? OutfitName { get; set; }
        public string? ImageUrl { get; set; }
        public List<OutfitItemRequestDto> Items { get; set; } = new();
    }

    public class OutfitItemRequestDto
    {
        public int ItemId { get; set; }

        public string? Slot { get; set; }
    }
}
