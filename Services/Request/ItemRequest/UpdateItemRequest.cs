using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pgvector;

namespace Services.Request.ItemRequest
{
    public class UpdateItemRequest
    {

        public string? ItemName { get; set; }

        public string? Description { get; set; }

        public string? MainColor { get; set; }

        public string? Pattern { get; set; }

        public string? Style { get; set; }

        public string? Texture { get; set; }

        public string? Fabric { get; set; }

        public string? Brand { get; set; }

        public string? Placement { get; set; }

        public string? Status { get; set; }
    }
}
