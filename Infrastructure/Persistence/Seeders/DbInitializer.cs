using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Domain.Entities;
using Infrastructure.Persistence;

namespace Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Account>>();
            var context = serviceProvider.GetRequiredService<FashionDbContext>();

            // --- 1. Seed Roles ---
            string[] roles = { "Admin", "User", "Expert", "Vendor" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            // --- 2. Seed Admin User ---
            var adminEmail = configuration["AdminSettings:Email"] ?? "admin@fashion.com";
            var adminPassword = configuration["AdminSettings:Password"] ?? "Admin@123";
            var adminUsername = configuration["AdminSettings:Username"] ?? "admin";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new Account
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Status = "Active",
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            if (adminUser != null)
            {
                var adminWallet = context.Wallets.FirstOrDefault(w => w.AccountId == adminUser.Id);

                if (adminWallet == null)
                {
                    adminWallet = new Wallet
                    {
                        AccountId = adminUser.Id,
                        Balance = 0,
                        LockedBalance = 0,
                        Currency = "VND",
                        UpdatedAt = DateTime.Now
                    };
                    context.Wallets.Add(adminWallet);
                    await context.SaveChangesAsync();
                }

                var settings = new List<SystemSetting>
                {
                    new SystemSetting
                    {
                        SettingKey = "EVENT_FEE_PERCENTAGE",
                        SettingValue = "5",
                        DataType = "Decimal",
                        Description = "Tỷ lệ phần trăm thu phí tạo sự kiện (%)"
                    },
                    new SystemSetting
                    {
                        SettingKey = "EVENT_MIN_FEE",
                        SettingValue = "10000",
                        DataType = "Decimal",
                        Description = "Phí tạo sự kiện tối thiểu (VNĐ)"
                    },
                    new SystemSetting
                    {
                        SettingKey = "MIN_EXPERTS_PER_EVENT",
                        SettingValue = "2",
                        DataType = "Number",
                        Description = "Số lượng chuyên gia tối thiểu để sự kiện có thể bắt đầu"
                    }
                };

                foreach (var setting in settings)
                {
                    if (!context.SystemSettings.Any(s => s.SettingKey == setting.SettingKey))
                    {
                        context.SystemSettings.Add(setting);
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }
}