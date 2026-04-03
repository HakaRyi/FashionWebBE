using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using Repositories.Data;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.AdminRepos;
using Repositories.Repos.ChatRepos;
using Repositories.Repos.CommentReactionRepos;
using Repositories.Repos.CommentRepos;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.EventExpertRepos;
using Repositories.Repos.Events;
using Repositories.Repos.EventWinnerRepos;
using Repositories.Repos.ExpertProfileRepos;
using Repositories.Repos.ExpertRatingRepos;
using Repositories.Repos.ExpertRequestRepos;
using Repositories.Repos.FollowRepos;
using Repositories.Repos.GroupRepos;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.ItemRespos;
using Repositories.Repos.ModelRepos;
using Repositories.Repos.NotificationRepos;
using Repositories.Repos.OrderRepos;
using Repositories.Repos.OutfitRepos;
using Repositories.Repos.PaymentsRespo;
using Repositories.Repos.PostRepos;
using Repositories.Repos.PostSaveRepos;
using Repositories.Repos.PrizeEventRepos;
using Repositories.Repos.ReactionRepos;
using Repositories.Repos.ReputationHistoryRepos;
using Repositories.Repos.ScoreboardRepos;
using Repositories.Repos.SocialRepos;
using Repositories.Repos.SystemSettingRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.TryOn;
using Repositories.Repos.UserReportRepos;
using Repositories.Repos.WalletRepos;
using Repositories.Repos.WardrobeRepos;
using Repositories.UnitOfWork;
using Services.AI;
using Services.Helpers;
using Services.Implements.AccountService;
using Services.Implements.AdminImp;
using Services.Implements.Auth;
using Services.Implements.BackgroundServices;
using Services.Implements.ChatImp;
using Services.Implements.EventAwardingImp;
using Services.Implements.EventCreationImp;
using Services.Implements.EventExpertSer;
using Services.Implements.Events;
using Services.Implements.ExpertRatingImp;
using Services.Implements.ExpertsService;
using Services.Implements.ExpertsService.ExpertRequestImp;
using Services.Implements.Follow;
using Services.Implements.ImageImp;
using Services.Implements.Items;
using Services.Implements.ModelImp;
using Services.Implements.NotificationImp;
using Services.Implements.OrderImp;
using Services.Implements.OutfitImp;
using Services.Implements.PaymentService;
using Services.Implements.PostImp;
using Services.Implements.SocialImp;
using Services.Implements.TransactionImp;
using Services.Implements.TryOn;
using Services.Implements.UserReportImp;
using Services.Implements.WalletImp;
using Services.Implements.Wardrobe;
using Services.Mappers;
using Services.RabbitMQ;
using Services.Utils;
using Services.Utils.AIDectection;
using Services.Utils.CloundStorage;
using Services.Utils.File;
using Services.Utils.SignalR;
using System.Text;
using WebAPIs.Endpoints;
using WebAPIs.Services;
using Services.Implements.SearchImp;
using Repositories.Repos.SearchRepos;
using Repositories.Seeders;
using Services.Utils.Gateways;

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

#region BASIC SERVICES

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

#endregion

#region DATABASE

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<FashionDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.UseVector();
    });
});

#endregion

#region IDENTITY


//background service
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

// 2. Chạy Quartz dưới dạng Hosted Service
builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = bool.Parse(quartzConfig["WaitForJobsToComplete"] ?? "true");
});



//identity

builder.Services.AddIdentity<Account, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<FashionDbContext>()
.AddDefaultTokenProviders();

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
builder.Services.AddScoped<IUserReportRepository, UserReportRepository>();
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
//builder.Services.AddScoped<IFileService, LocalFileService>();
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
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowFrontend", policy =>
//    {
//        policy.WithOrigins("http://localhost:5500")
//              .AllowAnyHeader()
//              .AllowAnyMethod()
//              .AllowCredentials();
//    });
//});

#endregion

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FashionDbContext>();
    //await ExpenseTestDataSeeder.SeedAsync(dbContext);
    //await MarketplaceTestDataSeeder.SeedAsync(dbContext);
    await PublicProfileWardrobeSeeder.SeedAsync(dbContext);
}

#region MIDDLEWARE

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
//app.UseCors("AllowFrontend");

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

#region SEED DATA

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var configuration = services.GetRequiredService<IConfiguration>();

    try
    {
        await DbInitializer.SeedRolesAndAdminAsync(services, configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error seeding roles");
    }
}


#endregion


app.MapQuartzEndpoints();

app.Run();

