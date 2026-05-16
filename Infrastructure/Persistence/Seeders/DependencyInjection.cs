using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seeders
{
    public static class PersistenceSeederExtensions
    {
        public static async Task SeedDatabase(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<FashionDbContext>();
            var logger = services.GetRequiredService<ILogger<FashionDbContext>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var userManager = services.GetRequiredService<UserManager<Account>>();

            try
            {
                await DbInitializer.SeedRolesAndAdminAsync(services, configuration);

                //await PublicProfileWardrobeSeeder.SeedAsync(context, userManager);

                logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}