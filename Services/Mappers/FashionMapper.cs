using Riok.Mapperly.Abstractions;
using Services.Response.ItemResp;

namespace Services.Mappers
{
    [Mapper]
    public partial class FashionMapper
    {
        [MapperIgnoreTarget(nameof(Repositories.Entities.Item.ItemEmbedding))]
        [MapperIgnoreTarget(nameof(Repositories.Entities.Item.Images))]
        public partial Repositories.Entities.Item ToEntity(ProductUploadDto dto);
        [MapProperty(nameof(Repositories.Entities.Item.Images), nameof(ItemResponseDto.PrimaryImageUrl))]
        public partial ItemResponseDto ToResponse(Repositories.Entities.Item item);

        private string? MapImageUrl(Repositories.Entities.Item item) =>
            item.Images.FirstOrDefault()?.ImageUrl;
        protected string? MapImagesToPrimaryUrl(ICollection<Repositories.Entities.Image> images) =>
        images?.FirstOrDefault()?.ImageUrl;
    }
}
