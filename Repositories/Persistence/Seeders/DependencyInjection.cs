using Infrastructure.Data;
using Infrastructure.Seeders;
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

            try
            {

                var configuration = services.GetRequiredService<IConfiguration>();

                await DbInitializer.SeedRolesAndAdminAsync(services, configuration);

                await PublicProfileWardrobeSeeder.SeedAsync(context);

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
