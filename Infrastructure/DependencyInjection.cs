using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pgvector;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found.");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector();

            var dataSource = dataSourceBuilder.Build();

            services.AddSingleton(dataSource);

            services.AddDbContext<FashionDbContext>(options =>
            {
                options.UseNpgsql(dataSource, npgsql =>
                {
                    npgsql.UseVector();
                    npgsql.MigrationsAssembly(typeof(FashionDbContext).Assembly.FullName);
                });
            });

            services.AddIdentityCore<Account>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<FashionDbContext>()
            .AddSignInManager<SignInManager<Account>>()
            .AddDefaultTokenProviders();

            return services;
        }
    }
}