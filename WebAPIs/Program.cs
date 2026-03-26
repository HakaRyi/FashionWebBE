using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using Quartz.Impl.Matchers;
using Repositories.Data;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.AccountSubscriptionRepos;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.EventExpertRepos;
using Repositories.Repos.Events;
using Repositories.Repos.EventWinnerRepos;
using Repositories.Repos.ExpertProfileRepos;
using Repositories.Repos.ExpertRatingRepos;
using Repositories.Repos.ExpertRequestRepos;
using Repositories.Repos.FollowRepos;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.ItemRespos;
using Repositories.Repos.OutfitRepos;
using Repositories.Repos.PackageRepos;
using Repositories.Repos.Payments;
using Repositories.Repos.PostRepos;
using Repositories.Repos.PrizeEventRepos;
using Repositories.Repos.ScoreboardRepos;
using Repositories.Repos.SocialRepos;
using Repositories.Repos.SystemSettingRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.UserReportRepos;
using Repositories.Repos.WalletRepos;
using Repositories.Repos.WardrobeRepos;
using Repositories.UnitOfWork;
using Services.AI;
using Services.Helpers;
using Services.Implements.AccountService;
using Services.Implements.Auth;
using Services.Implements.BackgroundServices;
using Services.Implements.EventExpertSer;
using Services.Implements.Events;
using Services.Implements.Experts;
using Services.Implements.ExpertsService.ExpertRequestImp;
using Services.Implements.Follow;
using Services.Implements.ImageImp;
using Services.Implements.Items;
using Services.Implements.OutfitImp;
using Services.Implements.PackageImp;
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
using System.Text;
using WebAPIs.Endpoints;
using WebAPIs.Services;

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

//-------------------------------------------------------------------------------//
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var schemaName = builder.Configuration["DatabaseSettings:SchemaName"] ?? "fashion_db";
builder.Services.AddDbContext<FashionDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
        //npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schemaName);
    });

    //options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

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

    //Lockout (If the account has multiple incorrect password attempts)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.User.RequireUniqueEmail = true;

    // Identity Email
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<FashionDbContext>()
.AddDefaultTokenProviders();

// Repository Layer
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<ISocialRepository, SocialRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<IUserReportRepository, UserReportRepository>();
builder.Services.AddScoped<IExpertRequestRepository, ExpertRequestRepository>();
builder.Services.AddScoped<IExpertProfileRepository, ExpertProfileRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IWardrobeRepository, WardrobeRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IOutfitRepository, OutfitRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IPrizeEventRepository, PrizeEventRepository>();
builder.Services.AddScoped<IEscrowSessionRepository, EscrowSessionRepository>();
builder.Services.AddScoped<IAccountSubscriptionRepository, AccountSubscriptionRepository>();
builder.Services.AddScoped<IEventExpertRepository, EventExpertRepository>();
builder.Services.AddScoped<IExpertRatingRepository, ExpertRatingRepository>();
builder.Services.AddScoped<IScoreboardRepository, ScoreboardRepository>();
builder.Services.AddScoped<IEventWinnerRepository, EventWinnerRepository>();
builder.Services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();


// Service Layer
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IExpertService, ExpertService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
//builder.Services.AddScoped<IFileService, LocalFileService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICloudStorageService, CloundStorageService>();
builder.Services.AddScoped<IAIDetectionService, AIDetectionService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IExpertRequestService, ExpertRequestService>();
builder.Services.AddScoped<IUserReportService, UserReportService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IEventExpertService, EventExpertService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IFileService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new LocalFileService(env.WebRootPath);
});
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient<ITryOnService, TryOnService>();
builder.Services.AddScoped<IOutfitService, OutfitService>();


builder.Services.AddHttpClient<IAIDetectionService, AIDetectionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();
builder.Services.AddHostedService<PostProcessingWorker>();

MapsterConfig.Configure();
builder.Services.AddMapster();

/////

//-------------------------------------------------------------------------------//
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
});

//-------------------------------------SWAGGER---------------------------------------//
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fashion Project API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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
        logger.LogError(ex, "Errors seeding Roles");
    }
}

app.MapQuartzEndpoints();

app.Run();
