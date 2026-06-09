using Application.Helpers;
using Application.Interfaces;
using Application.Jobs;
using Application.Mappers;
using Application.RabbitMQ;
using Application.Services;
using Application.Services.AdminImp;
using Application.Services.AI;
using Application.Services.BackgroundServices;
using Application.Services.ChatImp;
using Application.Services.EventServices;
using Application.Services.Follow;
using Application.Services.ImageImp;
using Application.Services.Items;
using Application.Services.ItemSaveImp;
using Application.Services.ModelImp;
using Application.Services.NotificationImp;
using Application.Services.OrderImp;
using Application.Services.OutfitImp;
using Application.Services.PaymentService;
using Application.Services.PostImp;
using Application.Services.RecommendationImp;
using Application.Services.SearchImp;
using Application.Services.SocialImp;
using Application.Services.TryOn;
using Application.Services.UserReportImp;
using Application.Services.WalletImp;
using Application.Services.Wardrobe;
using Application.Utils;
using Application.Utils.AIDectection;
using Application.Utils.CloundStorage;
using Application.Utils.File;
using Application.Utils.Gateways;
using Application.Utils.SignalR;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Persistence.Seeders;
using Infrastructure.Repositories;
using Infrastructure.UnitOfWork;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.Endpoints;
using Presentation.Middlewares;
using Presentation.Services;
using Quartz;
using System.Text;

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

#region BASIC SERVICES

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

#endregion

#region OPTIONS

builder.Services.Configure<VnPayOptions>(
    builder.Configuration.GetSection("VnPaySettings"));

builder.Services.Configure<ZaloPayOptions>(
    builder.Configuration.GetSection("ZaloPaySettings"));

#endregion

#region DATABASE

builder.Services.AddInfrastructureServices(builder.Configuration);

#endregion

#region REPOSITORIES

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentReactionRepository, CommentReactionRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IEscrowSessionRepository, EscrowSessionRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventCriterionRepository, EventCriterionRepository>();
builder.Services.AddScoped<IEventExpertRepository, EventExpertRepository>();
builder.Services.AddScoped<IEventWinnerRepository, EventWinnerRepository>();
builder.Services.AddScoped<IExpertProfileRepository, ExpertProfileRepository>();
builder.Services.AddScoped<IExpertRatingRepository, ExpertRatingRepository>();
builder.Services.AddScoped<IExpertRequestRepository, ExpertRequestRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IItemSaveRepository, ItemSaveRepository>();
builder.Services.AddScoped<IItemVariantRepository, ItemVariantRepository>();
builder.Services.AddScoped<IModelRepository, ModelRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutfitRepository, OutfitRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPhysicalProfileRepository, PhysicalProfileRepository>();
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
builder.Services.AddScoped<IPinMessageRepository, PinMessageRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostSaveRepository, PostSaveRepository>();
builder.Services.AddScoped<IPrizeEventRepository, PrizeEventRepository>();
builder.Services.AddScoped<IReactionRepository, ReactionRepository>();
builder.Services.AddScoped<IRecommendationHistoryRepository, RecommendationHistoryRepository>();
builder.Services.AddScoped<IRefundRequestRepository, RefundRequestRepository>();
builder.Services.AddScoped<IReputationHistoryRepository, ReputationHistoryRepository>();
builder.Services.AddScoped<IScoreboardRepository, ScoreboardRepository>();
builder.Services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();
builder.Services.AddScoped<ISocialRepository, SocialRepository>();
builder.Services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITryOnHistoryRepository, TryOnHistoryRepository>();
builder.Services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
builder.Services.AddScoped<Domain.Interfaces.IUserReportRepository, Infrastructure.Repositories.UserReportRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWardrobeRepository, WardrobeRepository>();
builder.Services.AddScoped<IAdminUserDashboardRepository, AdminUserDashboardRepository>();
builder.Services.AddScoped<IAdminPostDashboardRepository, AdminPostDashboardRepository>();
builder.Services.AddScoped<IPostTrendRepository, PostTrendRepository>();
builder.Services.AddScoped<ITrendingTopicRepository, TrendingTopicRepository>();
builder.Services.AddScoped<IHashtagRepository, HashtagRepository>();
builder.Services.AddScoped<IOrderStatusHistoryRepository, OrderStatusHistoryRepository>();
builder.Services.AddScoped<IEscrowStatusHistoryRepository, EscrowStatusHistoryRepository>();

#endregion

#region SERVICES

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IAIDetectionService, AIDetectionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IChatShareService, ChatShareService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEventAwardingService, EventAwardingService>();
builder.Services.AddScoped<IEventCreationService, EventCreationService>();
builder.Services.AddScoped<IEventExpertService, EventExpertService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IExpertRatingService, ExpertRatingService>();
builder.Services.AddScoped<IExpertRequestService, ExpertRequestService>();
builder.Services.AddScoped<IExpertService, ExpertService>();
builder.Services.AddScoped<IFileService, GoogleDriveService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IItemSaveService, ItemSaveService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IModelService, ModelService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOutfitService, OutfitService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPostSaveService, PostSaveService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IReputationHistoryService, ReputationHistoryService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<ITopUpPaymentProcessor, TopUpPaymentProcessor>();
builder.Services.AddScoped<ITryOnHistoryService, TryOnHistoryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IFileService, GoogleDriveService>();
builder.Services.AddScoped<IChatShareService, ChatShareService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<IOrderAdminService, OrderAdminService>();
builder.Services.AddScoped<IWhaleService, WhaleService>();
builder.Services.AddScoped<IItemAnalysisService, ItemAnalysisService>();
builder.Services.AddScoped<IUserReportService, UserReportService>();
builder.Services.AddScoped<IVnPayGatewayService, VnPayGatewayService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddScoped<IZaloPayGatewayService, ZaloPayGatewayService>();
builder.Services.AddScoped<IOrderAdminService, OrderAdminService>();
builder.Services.AddScoped<IWhaleService, WhaleService>();
builder.Services.AddScoped<IItemAnalysisService, ItemAnalysisService>();
builder.Services.AddScoped<IHashtagService, HashtagService>();
builder.Services.AddScoped<IAdminSocialDashboardService, AdminSocialDashboardService>();
builder.Services.AddScoped<IPostTrendService, PostTrendService>();
builder.Services.AddScoped<ITrendingTopicRepository, TrendingTopicRepository>();
builder.Services.AddScoped<ITrendingTopicService, TrendingTopicService>();
builder.Services.AddScoped<IEscrowStatusHistoryService, EscrowStatusHistoryService>();



#endregion

#region EXTERNAL SERVICES

builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();

builder.Services.AddHttpClient<IAIDetectionService, AIDetectionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ITryOnService, TryOnService>();

builder.Services.AddScoped<EmailService>();

#endregion

#region BACKGROUND SERVICES

builder.Services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();

builder.Services.AddHostedService<ChatConsumerWorker>();

builder.Services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(100));
builder.Services.AddHostedService<ModelProcessingWorker>();

#endregion

#region QUARTZ

var quartzConfig = builder.Configuration.GetSection("Quartz");

builder.Services.Configure<QuartzOptions>(options =>
{
    options.Scheduling.IgnoreDuplicates = true;
    options.Scheduling.OverWriteExistingData = true;
});

builder.Services.AddQuartz(q =>
{
    q.SchedulerId = quartzConfig["SchedulerId"] ?? "AUTO";
    q.SchedulerName = quartzConfig["SchedulerName"] ?? "FashionShop-Scheduler";

    q.UsePersistentStore(store =>
    {
        store.UsePostgres(postgres =>
        {
            postgres.ConnectionString =
                builder.Configuration.GetConnectionString("QuartzDb")
                ?? throw new InvalidOperationException("ConnectionString 'QuartzDb' was not found.");
        });

        store.UseNewtonsoftJsonSerializer();
        store.UseClustering();
    });

    var autoReleaseOrderJobKey = new JobKey("AutoReleaseDeliveredOrdersJob");

    q.AddJob<AutoReleaseDeliveredOrdersJob>(options =>
        options.WithIdentity(autoReleaseOrderJobKey)
            .WithDescription("Automatically completes delivered orders after 3 days and releases escrow payment to the seller.")
            .StoreDurably());

    q.AddTrigger(options => options
        .ForJob(autoReleaseOrderJobKey)
        .WithIdentity("AutoReleaseDeliveredOrdersJob-trigger")
        .WithDescription("Runs every 1 hour to find delivered orders that are ready to be auto-completed.")
        .WithSimpleSchedule(schedule => schedule
            .WithIntervalInHours(1)
            .RepeatForever()));


    var autoCancelPendingPaymentJobKey = new JobKey("AutoCancelPendingPaymentOrdersJob");

    q.AddJob<AutoCancelPendingPaymentOrdersJob>(options =>
        options.WithIdentity(autoCancelPendingPaymentJobKey)
            .WithDescription("Automatically cancels pending payment orders after 30 minutes and releases reserved stock.")
            .StoreDurably());

    q.AddTrigger(options => options
        .ForJob(autoCancelPendingPaymentJobKey)
        .WithIdentity("AutoCancelPendingPaymentOrdersJob-trigger")
        .WithDescription("Runs every 5 minutes to cancel unpaid orders that have stayed in pending payment for more than 30 minutes.")
        .WithSimpleSchedule(schedule => schedule
            .WithIntervalInMinutes(5)
            .RepeatForever()));

    //var recomputePostTrendJobKey = new JobKey("RecomputePostTrendJob");

    //q.AddJob<RecomputePostTrendJob>(options =>
    //    options.WithIdentity(recomputePostTrendJobKey)
    //        .WithDescription("Recompute trending posts every 1 hour.")
    //        .StoreDurably());

    //q.AddTrigger(options => options
    //    .ForJob(recomputePostTrendJobKey)
    //    .WithIdentity("RecomputePostTrendJob-trigger")
    //    .WithDescription("Runs every 1 hour to recompute post trends.")
    //    .WithSimpleSchedule(schedule => schedule
    //        //.WithIntervalInHours(1)
    //        .WithIntervalInMinutes(5)
    //        .RepeatForever()));

    var recomputeTrendingTopicJobKey =
    new JobKey("RecomputeTrendingTopicJob");

    q.AddJob<RecomputeTrendingTopicJob>(options =>
        options.WithIdentity(recomputeTrendingTopicJobKey)
            .WithDescription(
                "Recompute trending hashtags/topics every 1 hour.")
            .StoreDurably());

    q.AddTrigger(options => options
        .ForJob(recomputeTrendingTopicJobKey)
        .WithIdentity("RecomputeTrendingTopicJob-trigger")
        .WithDescription(
            "Runs every 1 hour to recompute trending hashtags/topics.")
        .WithSimpleSchedule(schedule => schedule
            //.WithIntervalInHours(1)
            .WithIntervalInMinutes(5)
            .RepeatForever()));

    var refundReturnKey = new JobKey(nameof(AutoRefundReturnDeliveredOrdersJob));
    q.AddJob<AutoRefundReturnDeliveredOrdersJob>(opts => opts.WithIdentity(refundReturnKey));
    q.AddTrigger(opts => opts
        .ForJob(refundReturnKey)
        .WithIdentity($"{nameof(AutoRefundReturnDeliveredOrdersJob)}-trigger")
        .WithSimpleSchedule(s => s.WithIntervalInMinutes(30).RepeatForever()));
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete =
        bool.Parse(quartzConfig["WaitForJobsToComplete"] ?? "true");
});

#endregion

#region MAPPERS

MapsterConfig.Configure();
builder.Services.AddMapster();

#endregion

#region SIGNALR

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

#endregion

#region JWT AUTHENTICATION

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(
    jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey was not found."));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrWhiteSpace(accessToken) &&
                (path.StartsWithSegments("/notificationHub") ||
                 path.StartsWithSegments("/chatHub") ||
                 path.StartsWithSegments("/orderHub")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

#endregion

#region SWAGGER

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fashion Project API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT token.",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });

    options.CustomSchemaIds(type => type.FullName);
});

#endregion

#region CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175",
                "https://wapofashion.vercel.app",
                "https://wapo.io.vn",
                "https://waposhipper.vercel.app"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

#endregion

var app = builder.Build();

#region DATABASE SEEDING

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDatabase();
}

#endregion

#region MIDDLEWARE

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    context.Response.Headers["ngrok-skip-browser-warning"] = "true";
    await next();
});

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<OrderHub>("/orderHub");

app.MapControllers();

app.MapQuartzEndpoints();

app.MapGet("/health", () => Results.Ok("healthy"));

#endregion

app.Run();