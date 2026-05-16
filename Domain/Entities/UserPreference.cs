
namespace Domain.Entities
{
    public class UserPreference
    {
        public int Id { get; set; }

        public int AccountId { get; set; }

        // Loại sở thích: "Style", "Color", "Material", "Brand"
        public string? PreferenceType { get; set; }

        // Giá trị: "Minimalism", "Black", "Silk", "Nike"
        public string? Value { get; set; }

        public virtual Account Account { get; set; } = null!;
    }
}
