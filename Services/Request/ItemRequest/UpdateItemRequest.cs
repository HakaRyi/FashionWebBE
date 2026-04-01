using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pgvector;
using Repositories.Entities;

namespace Services.Request.ItemRequest
{
    public class UpdateItemRequest
    {

        public string? ItemName { get; set; }

        public string? ItemType { get; set; }

        public string? Category { get; set; }

        public string? SubCategory { get; set; }

        public string? Style { get; set; }

        public string? Gender { get; set; }
        public string? Size { get; set; }

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

        public ItemStatus? Status { get; set; }

    }
}
