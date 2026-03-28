using Repositories.Entities;
using Riok.Mapperly.Abstractions;
using Services.Request.ItemReq;
using Services.Response.ItemResp;


namespace Services.Mappers
{
    [Mapper]
    public partial class FashionMapper
    {
        [MapperIgnoreTarget(nameof(Item.ItemEmbedding))]
        [MapperIgnoreTarget(nameof(Item.Images))]
        public partial Item ToEntity(ProductUploadDto dto);
        [MapProperty(nameof(Repositories.Entities.Item.Images), nameof(ItemResponseDto.PrimaryImageUrl))]
        public partial ItemResponseDto ToResponse(Item item);

        private string? MapImageUrl(Repositories.Entities.Item item) =>
            item.Images.FirstOrDefault()?.ImageUrl;
        protected string? MapImagesToPrimaryUrl(ICollection<Image> images) =>
        images?.FirstOrDefault()?.ImageUrl;
    }
}
