using Mapster;
using Repositories.Entities;
using Services.Request.EventReq;
using Services.Request.ItemReq;
using Services.Response.ExpertResp;
using Services.Response.ItemResp;


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

            TypeAdapterConfig<CreateEventRequest, Event>
                .NewConfig()
                .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
                .Map(dest => dest.Status, src => "Pending_Review")
                .Map(dest => dest.MinExpertsToStart, src => src.MinExpertsRequired)
                .Ignore(dest => dest.Images);

            TypeAdapterConfig<ExpertProfile, ExpertManagementByAdminDto>
                .NewConfig()
                .Map(dest => dest.UserName, src => src.Account != null ? src.Account.UserName : null)
                .Map(dest => dest.ExpertRequests, src => src.ExpertRequests.OrderByDescending(r => r.CreatedAt))
                .PreserveReference(true);

            TypeAdapterConfig<ExpertRequest, ExpertFileByAdminDto>
                .NewConfig();
        }
    }
}