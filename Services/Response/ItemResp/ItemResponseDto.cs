using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.ItemResp
{
    public class ItemResponseDto
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? MainColor { get; set; }
        public string? Style { get; set; }
        public string? Brand { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ProductUploadDto
    {
        public string? ItemName { get; set; }
        public int WardrobeId { get; set; }
        public string? Description { get; set; }
        public string? MainColor { get; set; }
        public string? Style { get; set; }
        public string? Fabric { get; set; }
        public string? Brand { get; set; }
        public string? Pattern { get; set; }
        public string? Texture { get; set; }
        public string? Placement { get; set; }
        public IFormFile? File { get; set; }
    }
}
