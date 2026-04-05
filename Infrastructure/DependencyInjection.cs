using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 1. Database Setup
            services.AddDbContext<FashionDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseVector();
                    npgsql.MigrationsAssembly(typeof(FashionDbContext).Assembly.FullName);
                });
            });

            // 2. Identity Setup
            // Note: Specifying IdentityRole<int> to match your custom PK requirement
            // 2. Identity Setup (Optimized for APIs)
            services.AddIdentityCore<Account>(options =>
            {
                // Your password/lockout settings stay the same
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<IdentityRole<int>>() // Manually add role support
            .AddEntityFrameworkStores<FashionDbContext>()
            .AddSignInManager<SignInManager<Account>>()
            .AddDefaultTokenProviders();

            // 3. Repository Registrations
            // services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();

            return services;
        }
    }
}