using System.ComponentModel.DataAnnotations;

namespace Application.Request.ItemReq
{
    public class PublishItemForSaleRequest
    {
        [Range(1, double.MaxValue, ErrorMessage = "Listed price must be greater than 0.")]
        public decimal ListedPrice { get; set; }

        [MaxLength(50)]
        public string? Condition { get; set; }

        public List<CreateItemVariantRequest> Variants { get; set; } = new();
    }
}