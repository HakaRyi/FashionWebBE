using System.ComponentModel.DataAnnotations;

namespace Application.Request.ItemReq
{
    public class CreateItemVariantRequest
    {
        [Required]
        [MaxLength(100)]
        public string Sku { get; set; } = null!;

        [MaxLength(20)]
        public string? SizeCode { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }
    }
}