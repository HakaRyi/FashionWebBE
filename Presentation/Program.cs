using Domain.Interfaces;
using Infrastructure.Persistence.Seeders;
using Infrastructure.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using Application.Helpers;
using Application.Mappers;
using Application.RabbitMQ;
using Application.Utils;
using Application.Utils.AIDectection;
using Application.Utils.CloundStorage;
using Application.Utils.File;
using Application.Utils.Gateways;
using Application.Utils.SignalR;
using System.Text;
using Presentation.Endpoints;
using Presentation.Services;
using Infrastructure;
using Application.Interfaces;
using Application.Services;
using Application.Services.Wardrobe;
using Application.Services.WalletImp;
using Application.Services.AdminImp;
using Application.Services.BackgroundServices;
using Application.Services.NotificationImp;
using Application.Services.ModelImp;
using Application.Services.UserReportImp;
using Application.Services.TryOn;
using Application.Services.SocialImp;
using Application.Services.PostImp;
using Application.Services.SearchImp;
using Application.Services.EventServices;
using Application.Services.ChatImp;
using Application.Services.ImageImp;
using Application.Services.Follow;
using Application.Services.OutfitImp;
using Application.Services.PaymentService;
using Application.Services.OrderImp;
using Application.Services.Items;
using Application.Services.AI;
using Infrastructure.UnitOfWork;

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

#region BASIC SERVICES

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddScoped<IEscrowSessionRepository, EscrowSessionRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventExpertRepository, EventExpertRepository>();
builder.Services.AddScoped<IEventWinnerRepository, EventWinnerRepository>();
builder.Services.AddScoped<IExpertProfileRepository, ExpertProfileRepository>();
builder.Services.AddScoped<IExpertRatingRepository, ExpertRatingRepository>();
builder.Services.AddScoped<IExpertRequestRepository, ExpertRequestRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IModelRepository, ModelRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IOutfitRepository, OutfitRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
builder.Services.AddScoped<IPinMessageRepository, PinMessageRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostSaveRepository, PostSaveRepository>();
builder.Services.AddScoped<IPrizeEventRepository, PrizeEventRepository>();
builder.Services.AddScoped<IReactionRepository, ReactionRepository>();
builder.Services.AddScoped<IScoreboardRepository, ScoreboardRepository>();
builder.Services.AddScoped<ISocialRepository, SocialRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITryOnHistoryRepository, TryOnHistoryRepository>();
builder.Services.AddScoped<Domain.Interfaces.IUserReportRepository, Infrastructure.Repositories.UserReportRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWardrobeRepository, WardrobeRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IPinMessageRepository, PinMessageRepository>();
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IPrizeEventRepository, PrizeEventRepository>();
builder.Services.AddScoped<IEscrowSessionRepository, EscrowSessionRepository>();
builder.Services.AddScoped<IEventExpertRepository, EventExpertRepository>();
builder.Services.AddScoped<IExpertRatingRepository, ExpertRatingRepository>();
builder.Services.AddScoped<IReputationHistoryRepository, ReputationHistoryRepository>();
builder.Services.AddScoped<IScoreboardRepository, ScoreboardRepository>();
builder.Services.AddScoped<IEventWinnerRepository, EventWinnerRepository>();
builder.Services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
builder.Services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();

#endregion

#region SERVICES

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IExpertService, ExpertService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IAIDetectionService, AIDetectionService>();
builder.Services.AddScoped<IExpertRequestService, ExpertRequestService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IOutfitService, OutfitService>();
builder.Services.AddScoped<IModelService, ModelService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IUserReportService, UserReportService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IEventExpertService, EventExpertService>();
builder.Services.AddScoped<IEventAwardingService, EventAwardingService>();
builder.Services.AddScoped<IExpertRatingService, ExpertRatingService>();
builder.Services.AddScoped<IEventCreationService, EventCreationService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IPostSaveService, PostSaveService>();
builder.Services.AddScoped<ITryOnHistoryService, TryOnHistoryService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IVnPayGatewayService, VnPayGatewayService>();
builder.Services.AddScoped<IZaloPayGatewayService, ZaloPayGatewayService>();
builder.Services.AddScoped<ITopUpPaymentProcessor, TopUpPaymentProcessor>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<IRefundRequestRepository, RefundRequestRepository>();

#endregion

#region EXTERNAL SERVICES

builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();

builder.Services.AddHttpClient<IAIDetectionService, AIDetectionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ITryOnService, TryOnService>();

builder.Services.AddScoped<IFileService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new LocalFileService(env.WebRootPath);
});

builder.Services.AddScoped<EmailService>();

#endregion

#region BACKGROUND

builder.Services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();
builder.Services.AddHostedService<PostProcessingWorker>();
builder.Services.AddHostedService<ChatConsumerWorker>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundTaskQueue(100));
builder.Services.AddHostedService<ModelProgessingWorker>();

var quartzConfig = builder.Configuration.GetSection("Quartz");

builder.Services.AddQuartz(q =>
{
    q.SchedulerId = quartzConfig["SchedulerId"] ?? "AUTO";
    q.SchedulerName = quartzConfig["SchedulerName"] ?? "FashionShop-Scheduler";

    q.UsePersistentStore(s =>
    {
        s.UsePostgres(postgres =>
        {
            postgres.ConnectionString = builder.Configuration.GetConnectionString("QuartzDb")
                ?? throw new InvalidOperationException("ConnectionString 'QuartzDb' không tìm thấy!");
        });

        s.UseNewtonsoftJsonSerializer();
        s.UseClustering();
    });
});

builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = bool.Parse(quartzConfig["WaitForJobsToComplete"] ?? "true");
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
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

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

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/notificationHub") ||
                 path.StartsWithSegments("/chatHub")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

#endregion

#region SWAGGER

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fashion Project API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    c.CustomSchemaIds(type => type.FullName);
});

#endregion

#region CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();

        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

#endregion

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDatabase();
}

#region MIDDLEWARE

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("ngrok-skip-browser-warning", "true");
    await next();
});

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<OrderHub>("/orderHub");
app.MapControllers();

#endregion

app.MapQuartzEndpoints();

app.Run();