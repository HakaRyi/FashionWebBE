using Microsoft.AspNetCore.Http;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.ItemResp
{
    public class ItemResponseDto
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }

        public string? Category { get; set; }
        public string? ItemType { get; set; }

        public string? MainColor { get; set; }
        public string? Material { get; set; }
        public string? Style { get; set; }
        public string? Brand { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
