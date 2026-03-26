using Repositories.Entities;
using Services.Request.ItemReq;
using Services.Response.ItemResp;
using Mapster;


namespace Services.Mappers
{
    public static class MapsterConfig
    {
        public static void Configure()
        {
            TypeAdapterConfig<Item, ItemResponseDto>.NewConfig()
                .Map(dest => dest.PrimaryImageUrl, src => src.Images.Select(img => img.ImageUrl).FirstOrDefault())
                .Map(dest => dest.Category, src => src.Category != null ? src.Category.ToString() : null);

            TypeAdapterConfig<ProductUploadDto, Item>.NewConfig()
                .Ignore(dest => dest.ItemEmbedding)
                .Ignore(dest => dest.Images)
                .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
                .Map(dest => dest.Status, src => src.Status ?? ItemStatus.Active);
        }
    }
}