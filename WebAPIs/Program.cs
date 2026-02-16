using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositories.Data;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.ExpertFileRepos;
using Repositories.Repos.ExpertProfileRepos;
using Repositories.Repos.FollowRepos;
using Repositories.Repos.PackageCoinRepos;
using Repositories.Repos.PostRepos;
using Repositories.Repos.SocialRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.UserReportRepos;
using Repositories.Repos.WardrobeRepos;
using Services.Helpers;
using Services.Implements.AccountService;
using Services.Implements.Auth;
using Services.Implements.BackgroundServices;
using Services.Implements.ExpertFileImp;
using Services.Implements.Follow;
using Services.Implements.PackageCoinImp;
using Services.Implements.PostImp;
using Services.Implements.SocialImp;
using Services.Implements.TransactionImp;
using Services.Implements.TryOn;
using Services.Implements.UserReportImp;
using Services.Implements.Wardrobe;
using Services.RabbitMQ;
using Services.Utils;
using Services.Utils.AIDectection;
using Services.Utils.CloundStorage;
using System.Text;
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//-------------------------------------------------------------------------------//
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var schemaName = builder.Configuration["DatabaseSettings:SchemaName"] ?? "fashion_db";
builder.Services.AddDbContext<FashionDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schemaName);
    });

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Repository Layer
builder.Services.AddScoped<ISocialRepository, SocialRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPackageCoinRepository, PackageCoinRepository>();
builder.Services.AddScoped<IUserReportRepository, UserReportRepository>();
builder.Services.AddScoped<IExpertFileRepository, ExpertFileRepository>();
builder.Services.AddScoped<IExpertProfileRepository, ExpertProfileRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IWardrobeRepository, WardrobeRepository>();
builder.Services.AddScoped<IFollowRepository,FollowRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICloudStorageService, CloundStorageService>();
builder.Services.AddScoped<IAIDetectionService, AIDetectionService>();
builder.Services.AddScoped<IAccountService,AccountService>();
builder.Services.AddScoped<IExpertFileService, ExpertFileService>();
builder.Services.AddScoped<IUserReportService, UserReportService>();
builder.Services.AddScoped<IPackageCoinService, PackageCoinService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ISocialService,SocialService>();


// Service Layer
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient<ITryOnService, TryOnService>();

builder.Services.AddHttpClient<IAIDetectionService, AIDetectionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();
builder.Services.AddHostedService<PostProcessingWorker>();

//-------------------------------------------------------------------------------//
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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
