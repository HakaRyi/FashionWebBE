using Mapster;
using Repositories.Entities;
using Services.Request.EventReq;
using Services.Request.ItemReq;
using Services.Response.EventResp;
using Services.Response.ExpertResp;
using Services.Response.ItemResp;
using Services.Response.PostResp;


namespace Services.Mappers
{
    public static class MapsterConfig
    {
        public static void Configure()
        {
            TypeAdapterConfig<Item, ItemResponseDto>.NewConfig()
                .Map(dest => dest.PrimaryImageUrl, src => src.Images.Select(img => img.ImageUrl).FirstOrDefault())
                .Map(dest => dest.Category, src => src.Category != null ? src.Category.ToString() : null);

            TypeAdapterConfig<Request.ItemReq.ProductUploadDto, Item>.NewConfig()
                .Ignore(dest => dest.ItemEmbedding)
                .Ignore(dest => dest.Images)
                .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
                .Map(dest => dest.Status, src => src.Status ?? ItemStatus.Active);

            TypeAdapterConfig<CreateEventRequest, Event>
                .NewConfig()
                .Map(dest => dest.StartTime, src => src.StartTime.ToUniversalTime())
                .Map(dest => dest.EndTime, src => src.EndTime.ToUniversalTime())
                .Map(dest => dest.SubmissionDeadline, src => src.SubmissionDeadline.ToUniversalTime())
                .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
                .Map(dest => dest.Status, _ => "Pending_Review")
                .Map(dest => dest.MinExpertsToStart, src => src.MinExpertsRequired)
                .Ignore(dest => dest.Images);

            TypeAdapterConfig<ExpertProfile, ExpertManagementByAdminDto>
                .NewConfig()
                .Map(dest => dest.UserName, src => src.Account != null ? src.Account.UserName : null)
                .Map(dest => dest.ExpertRequests, src => src.ExpertRequests.OrderByDescending(r => r.CreatedAt))
                .PreserveReference(true);

            TypeAdapterConfig<ExpertRequest, ExpertFileByAdminDto>
                .NewConfig();

            TypeAdapterConfig<Event, EventListDto>.ForType()
                .Map(dest => dest.CreatorName, src => src.Creator != null ? src.Creator.UserName : null)
                .Map(dest => dest.ThumbnailUrl, src => src.Images.Select(img => img.ImageUrl).FirstOrDefault())
                .Map(dest => dest.TotalPrizePool, src => src.PrizeEvents.Sum(p => p.RewardAmount))
                .Map(dest => dest.ParticipantCount, src => src.Posts.Select(p => p.AccountId).Distinct().Count())
                .MaxDepth(3);

            TypeAdapterConfig<Event, EventDetailDto>.NewConfig()
                .Map(dest => dest.CreatorName, src => src.Creator != null ? src.Creator.UserName : null)
                .Map(dest => dest.ParticipantCount, src => src.Posts != null ? src.Posts.Count : 0)
                .Map(dest => dest.TotalPrizePool, src => src.PrizeEvents != null ? src.PrizeEvents.Sum(p => p.RewardAmount) : 0)
                .Map(dest => dest.ThumbnailUrl, src =>
                (src.Images != null && src.Images.Any())
                    ? src.Images.OrderBy(i => i.ImageId).FirstOrDefault().ImageUrl
                    : null)
                .Map(dest => dest.Prizes, src => src.PrizeEvents != null ? src.PrizeEvents.OrderBy(p => p.Ranked) : null)
                .Map(dest => dest.Experts, src => src.EventExperts)
                .Ignore(dest => dest.IsJoined)
                .Ignore(dest => dest.CanManualStart)
                .Ignore(dest => dest.AcceptedExpertsCount);

            TypeAdapterConfig<EventExpert, ExpertInEventDto>.NewConfig()
                .Map(dest => dest.FullName, src => src.Expert != null ? src.Expert.UserName : null)
                .Map(dest => dest.AvatarUrl, src =>
                (src.Expert != null && src.Expert.Avatars != null && src.Expert.Avatars.Any())
                    ? src.Expert.Avatars.OrderByDescending(img => img.CreatedAt).FirstOrDefault().ImageUrl
                    : null);

            TypeAdapterConfig<Post, PostResponse>.NewConfig()
                .Map(dest => dest.UserName, src => src.Account != null ? src.Account.UserName : "Người dùng hệ thống")

                .Map(dest => dest.AvatarUrl, src => (src.Account != null && src.Account.Avatars != null && src.Account.Avatars.Any())
                    ? src.Account.Avatars.OrderByDescending(a => a.CreatedAt).FirstOrDefault().ImageUrl
                    : null)

                .Map(dest => dest.EventName, src => src.Event != null ? src.Event.Title : "Sự kiện chung")

                .Map(dest => dest.ImageUrls, src => (src.Images != null && src.Images.Any())
                    ? src.Images.OrderBy(i => i.CreatedAt).Select(i => i.ImageUrl).ToList()
                    : new List<string>())

                .Ignore(dest => dest.Score)
                .Ignore(dest => dest.Reason);
        }
    }
}