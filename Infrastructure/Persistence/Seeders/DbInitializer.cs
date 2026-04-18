using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.Seeders
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
            var context = scope.ServiceProvider.GetRequiredService<FashionDbContext>();

            await SeedRolesAsync(roleManager);
            var adminUser = await SeedAdminUserAsync(userManager, configuration);

            if (adminUser != null)
            {
                await SeedAdminWalletAsync(context, adminUser.Id);
            }

            await SeedSystemSettingsAsync(context);
            await SeedReportTypesAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roles = { "Admin", "User", "Expert", "Vendor" };

            foreach (var roleName in roles)
            {
                var exists = await roleManager.RoleExistsAsync(roleName);
                if (!exists)
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }
        }

        private static async Task<Account?> SeedAdminUserAsync(
            UserManager<Account> userManager,
            IConfiguration configuration)
        {
            var adminEmail = configuration["AdminSettings:Email"] ?? "admin@fashion.com";
            var adminPassword = configuration["AdminSettings:Password"] ?? "Admin@123";
            var adminUsername = configuration["AdminSettings:Username"] ?? "admin";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser != null)
            {
                var roles = await userManager.GetRolesAsync(adminUser);
                if (!roles.Contains("Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                return adminUser;
            }

            adminUser = new Account
            {
                UserName = adminUsername,
                Email = adminEmail,
                EmailConfirmed = true,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Tạo tài khoản admin thất bại: {errors}");
            }

            await userManager.AddToRoleAsync(adminUser, "Admin");

            return adminUser;
        }

        private static async Task SeedAdminWalletAsync(FashionDbContext context, int adminAccountId)
        {
            var walletExists = await context.Wallets
                .AsNoTracking()
                .AnyAsync(w => w.AccountId == adminAccountId);

            if (walletExists)
            {
                return;
            }

            var adminWallet = new Wallet
            {
                AccountId = adminAccountId,
                Balance = 0,
                LockedBalance = 0,
                Currency = "VND",
                UpdatedAt = DateTime.UtcNow
            };

            await context.Wallets.AddAsync(adminWallet);
            await context.SaveChangesAsync();
        }

        private static async Task SeedSystemSettingsAsync(FashionDbContext context)
        {
            var settings = new List<SystemSetting>
            {
                new SystemSetting
                {
                    SettingKey = "EVENT_FEE_PERCENTAGE",
                    SettingValue = "5",
                    DataType = "Decimal",
                    Description = "Percentage of event creation fees (%)"
                },
                new SystemSetting
                {
                    SettingKey = "EVENT_MIN_FEE",
                    SettingValue = "10000",
                    DataType = "Decimal",
                    Description = "Minimum event creation fee (VNĐ)"
                },
                new SystemSetting
                {
                    SettingKey = "MIN_EXPERTS_PER_EVENT",
                    SettingValue = "2",
                    DataType = "Number",
                    Description = "Minimum number of professionals required for the event to begin."
                }
            };

            foreach (var setting in settings)
            {
                var exists = await context.SystemSettings
                    .AsNoTracking()
                    .AnyAsync(s => s.SettingKey == setting.SettingKey);

                if (!exists)
                {
                    await context.SystemSettings.AddAsync(setting);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedReportTypesAsync(FashionDbContext context)
        {
            var reportTypes = new List<ReportType>
            {
                new ReportType
                {
                    TypeName = "Spam",
                    Description = "Nội dung spam, lặp lại nhiều lần hoặc gây phiền."
                },
                new ReportType
                {
                    TypeName = "Nội dung phản cảm",
                    Description = "Hình ảnh hoặc nội dung không phù hợp với cộng đồng."
                },
                new ReportType
                {
                    TypeName = "Quấy rối / xúc phạm",
                    Description = "Nội dung công kích, xúc phạm hoặc quấy rối người khác."
                },
                new ReportType
                {
                    TypeName = "Lừa đảo / gian lận",
                    Description = "Nội dung có dấu hiệu lừa đảo, giả mạo hoặc gian lận."
                },
                new ReportType
                {
                    TypeName = "Thông tin sai sự thật",
                    Description = "Nội dung gây hiểu nhầm hoặc cung cấp thông tin sai lệch."
                },
                new ReportType
                {
                    TypeName = "Khác",
                    Description = "Lý do khác không thuộc các nhóm trên."
                }
            };

            foreach (var reportType in reportTypes)
            {
                var exists = await context.ReportTypes
                    .AsNoTracking()
                    .AnyAsync(x => x.TypeName == reportType.TypeName);

                if (!exists)
                {
                    await context.ReportTypes.AddAsync(reportType);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}