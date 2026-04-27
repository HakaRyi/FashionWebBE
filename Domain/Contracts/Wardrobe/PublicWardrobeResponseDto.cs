using Domain.Contracts.Common;
using Domain.Contracts.Wardrobe;

namespace Domain.Dto.Wardrobe
{
    public class PublicWardrobeResponseDto
    {
        public int AccountId { get; set; }
        public int WardrobeId { get; set; }
        public int TotalPublicItems { get; set; }
        public PagedResultDto<PublicWardrobeItemDto> Items { get; set; } = new();
    }
}