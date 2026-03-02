using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.OufitReq
{
    public class AddOutfitRequest
    {
        public string OutfitName { get; set; } = null!;
        public IFormFile Image { get; set; } = null!;
    }
}
