using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositories.Data;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.FollowRepos;
using Repositories.Repos.PostRepos;
using Repositories.Repos.WardrobeRepos;
using Services.Helpers;
using Services.Implements.Auth;
using Services.Implements.Follow;
using Services.Implements.PostImp;
using Services.Implements.Wardrobe;
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
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IWardrobeRepository, WardrobeRepository>();
builder.Services.AddScoped<IFollowRepository,FollowRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICloudStorageService, CloundStorageService>();
builder.Services.AddScoped<IAIDetectionService, AIDetectionService>();

// Service Layer
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddScoped<EmailService>();

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
        Description = "Enter Token: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
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
