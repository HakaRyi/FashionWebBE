using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Request.OutfitItemReq
{
    public class CreateOutfitItemRequest
    {
        public string OutfitName { get; set; }
        public string? ImageUrl { get; set; } 
        public string? DateScheduled { get; set; } 
        public List<int> ItemIds { get; set; }
    }
}
