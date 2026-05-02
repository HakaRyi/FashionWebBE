using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Response.ItemResp;

namespace Application.Request.CollectionDto

{
    public class CollectionCreateDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public List<int> ItemIds { get; set; } = new();
    }
    public class CollectionUpdateDto
    {
        public string NewTitle { get; set; } = null!;
        public string? NewDescription { get; set; }
        public List<int> NewItemIds { get; set; } = new();
    }

    public class CollectionResponseDto
    {
        public int CollectionId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ItemResponseDto> Items { get; set; } = new();
    }
}
