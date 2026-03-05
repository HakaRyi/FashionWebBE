using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositories.Data;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.Events;
using Repositories.Repos.ExpertFileRepos;
using Repositories.Repos.ExpertProfileRepos;
using Repositories.Repos.FollowRepos;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.ItemRespos;
using Repositories.Repos.PackageCoinRepos;
using Repositories.Repos.Payments;
using Repositories.Repos.PostRepos;
using Repositories.Repos.SocialRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.UserReportRepos;
using Repositories.Repos.WardrobeRepos;
using Repositories.UnitOfWork;
using Services.AI;
using Services.Helpers;
using Services.Implements.AccountService;
using Services.Implements.Auth;
using Services.Implements.BackgroundServices;
using Services.Implements.Events;
using Services.Implements.Experts;
using Services.Implements.ExpertsService.ExpertFileImp;
using Services.Implements.Follow;
using Services.Implements.ImageImp;
using Services.Implements.Items;
using Services.Implements.PackageCoinImp;
using Services.Implements.PaymentService;
using Services.Implements.PostImp;
using Services.Implements.SocialImp;
using Services.Implements.TransactionImp;
using Services.Implements.TryOn;
using Services.Implements.UserReportImp;
using Services.Implements.Wardrobe;
using Services.Mappers;
using Services.RabbitMQ;
using Services.Utils;
using Services.Utils.AIDectection;
using Services.Utils.CloundStorage;
using Services.Utils.File;
using System.Text;

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
builder.Services.AddScoped<IPackageCoinRepository, PackageCoinRepository>();
builder.Services.AddScoped<IUserReportRepository, UserReportRepository>();
builder.Services.AddScoped<IExpertFileRepository, ExpertFileRepository>();
builder.Services.AddScoped<IExpertProfileRepository, ExpertProfileRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IWardrobeRepository, WardrobeRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICloudStorageService, CloundStorageService>();
builder.Services.AddScoped<IAIDetectionService, AIDetectionService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IExpertFileService, ExpertFileService>();
builder.Services.AddScoped<IUserReportService, UserReportService>();
builder.Services.AddScoped<IPackageCoinService, PackageCoinService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();



// Service Layer
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IExpertService, ExpertService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAiService, AiService>();
//builder.Services.AddScoped<IFileService, LocalFileService>();
builder.Services.AddScoped<IFileService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new LocalFileService(env.WebRootPath);
});
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient<ITryOnService, TryOnService>();


builder.Services.AddHttpClient<IAIDetectionService, AIDetectionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();
builder.Services.AddHostedService<PostProcessingWorker>();

builder.Services.AddSingleton<FashionMapper>();

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

app.Run();
