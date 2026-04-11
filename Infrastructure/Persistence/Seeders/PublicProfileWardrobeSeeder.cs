using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Domain.Entities;
using Domain.Constants;

namespace Infrastructure.Persistence.Seeders
{
    public static class PublicProfileWardrobeSeeder
    {
        private const string TargetEmail = "nvhoang0975@gmail.com";
        private const string SeedMarker = "PUBLIC_PROFILE_WARDROBE_TEST_SEED";
        private const string DefaultUserRole = "User";
        private const string DefaultPassword = "123456Aa@";

        public static async Task SeedAsync(
            FashionDbContext context,
            UserManager<Account> userManager)
        {
            // =========================================================
            // 0. Chỉ seed khi target account CHƯA tồn tại
            // =========================================================
            var existedTarget = await userManager.FindByEmailAsync(TargetEmail);
            if (existedTarget != null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var random = new Random(20260331);

            var wardrobes = context.Set<Wardrobe>();
            var items = context.Set<Item>();
            var images = context.Set<Image>();
            var outfits = context.Set<Outfit>();
            var outfitItems = context.Set<OutfitItem>();
            var wallets = context.Set<Wallet>();
            var posts = context.Set<Post>();
            var follows = context.Set<Follow>();

            // =========================================================
            // 1. Seed category name dùng nội bộ
            // =========================================================
            var categoryNames = new List<string>
            {
                "Áo thun",
                "Áo sơ mi",
                "Áo hoodie",
                "Áo khoác",
                "Quần jean",
                "Quần short",
                "Chân váy",
                "Đầm",
                "Giày",
                "Túi xách",
                "Phụ kiện"
            };

            // =========================================================
            // 2. Tạo account chính để test bằng UserManager
            // =========================================================
            var targetAccount = new Account
            {
                UserName = "nvhoang0975",
                NormalizedUserName = "NVHOANG0975",
                Email = TargetEmail,
                NormalizedEmail = TargetEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CreatedAt = now,
                Status = "Active",
                VerificationCode = null,
                CodeExpiredAt = null,
                FreeTryOn = 20,
                Description = $"{SeedMarker} - tài khoản chính để test xem profile và wardrobe public",
                CountPost = 0,
                CountFollower = 0,
                CountFollowing = 0,
                IsOnline = "Offline",
                LockoutEnabled = false,
                AccessFailedCount = 0
            };

            await CreateUserWithRoleAsync(userManager, targetAccount, DefaultPassword, DefaultUserRole);

            // Avatar account chính
            var targetAvatar = new Image
            {
                AccountAvatarId = targetAccount.Id,
                ImageUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=500",
                OwnerType = "AccountAvatar",
                CreatedAt = now
            };
            await images.AddAsync(targetAvatar);

            // Wallet account chính
            var targetWallet = new Wallet
            {
                AccountId = targetAccount.Id,
                Balance = 5000000m,
                LockedBalance = 0m,
                Currency = "VND",
                UpdatedAt = now
            };
            await wallets.AddAsync(targetWallet);

            // Wardrobe account chính
            var targetWardrobe = new Wardrobe
            {
                AccountId = targetAccount.Id,
                Name = "Tủ đồ của Hoàng",
                CreatedAt = now
            };
            await wardrobes.AddAsync(targetWardrobe);

            await context.SaveChangesAsync();

            // =========================================================
            // 3. Tạo nhiều account khác bằng vòng lặp
            // =========================================================
            var demoAccounts = new List<Account>();
            var demoWardrobes = new List<Wardrobe>();

            int demoUserCount = 12;

            for (int i = 1; i <= demoUserCount; i++)
            {
                var email = $"publictester{i}@example.com";
                var userName = $"publictester{i}";
                var createdAt = now.AddDays(-i);

                var acc = new Account
                {
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = createdAt,
                    Status = "Active",
                    VerificationCode = null,
                    CodeExpiredAt = null,
                    FreeTryOn = 10 + i % 5,
                    Description = $"{SeedMarker} - tài khoản demo số {i}",
                    CountPost = 0,
                    CountFollower = 0,
                    CountFollowing = 0,
                    IsOnline = i % 2 == 0 ? "Online" : "Offline",
                    LockoutEnabled = false,
                    AccessFailedCount = 0
                };

                await CreateUserWithRoleAsync(userManager, acc, DefaultPassword, DefaultUserRole);
                demoAccounts.Add(acc);
            }

            // Wallet + Avatar + Wardrobe cho demo accounts
            var walletList = new List<Wallet>();
            var avatarList = new List<Image>();

            for (int i = 0; i < demoAccounts.Count; i++)
            {
                var acc = demoAccounts[i];
                var idx = i + 1;

                walletList.Add(new Wallet
                {
                    AccountId = acc.Id,
                    Balance = 1000000m + idx * 150000m,
                    LockedBalance = idx % 3 == 0 ? 50000m : 0m,
                    Currency = "VND",
                    UpdatedAt = now.AddDays(-idx)
                });

                avatarList.Add(new Image
                {
                    AccountAvatarId = acc.Id,
                    ImageUrl = GetAvatarUrl(idx),
                    OwnerType = "AccountAvatar",
                    CreatedAt = now.AddDays(-idx)
                });

                demoWardrobes.Add(new Wardrobe
                {
                    AccountId = acc.Id,
                    Name = $"Tủ đồ demo {idx}",
                    CreatedAt = now.AddDays(-idx)
                });
            }

            await wallets.AddRangeAsync(walletList);
            await images.AddRangeAsync(avatarList);
            await wardrobes.AddRangeAsync(demoWardrobes);
            await context.SaveChangesAsync();

            // =========================================================
            // 4. Seed items cho target account
            // =========================================================
            var itemNames = new[]
            {
                "Áo thun basic", "Áo sơ mi trắng", "Áo hoodie xám", "Áo khoác denim",
                "Quần jean xanh", "Quần short kaki", "Chân váy chữ A", "Đầm hoa",
                "Sneaker trắng", "Túi đeo chéo", "Mũ lưỡi trai", "Kính râm"
            };

            var itemTypes = new[]
            {
                "Top", "Top", "Top", "Outer",
                "Bottom", "Bottom", "Bottom", "Fullbody",
                "Shoes", "Bag", "Accessory", "Accessory"
            };

            var mainColors = new[]
            {
                "Trắng", "Đen", "Xanh", "Xám", "Be", "Nâu", "Hồng", "Đỏ"
            };

            var brands = new[]
            {
                "Zara", "H&M", "Uniqlo", "Routine", "Yody", "Canifa", "Nike", "Adidas"
            };

            var targetItems = new List<Item>();
            var targetItemImages = new List<Image>();

            for (int i = 0; i < 18; i++)
            {
                var idx = i % itemNames.Length;
                var createdAt = now.AddDays(-(i + 1));

                var item = new Item
                {
                    WardrobeId = targetWardrobe.WardrobeId,
                    ItemName = $"{itemNames[idx]} #{i + 1}",
                    ItemType = itemTypes[idx],
                    Category = categoryNames[idx % categoryNames.Count],
                    SubCategory = $"Sub-{idx + 1}",
                    Style = i % 2 == 0 ? "Casual" : "Streetwear",
                    Gender = i % 3 == 0 ? "Unisex" : i % 2 == 0 ? "Male" : "Female",
                    MainColor = mainColors[i % mainColors.Length],
                    SubColor = mainColors[(i + 2) % mainColors.Length],
                    Material = i % 2 == 0 ? "Cotton" : "Polyester",
                    Pattern = i % 4 == 0 ? "Plain" : "Minimal",
                    Fit = i % 3 == 0 ? "Regular" : "Slim",
                    Neckline = itemTypes[idx] == "Top" ? "Round Neck" : null,
                    SleeveLength = itemTypes[idx] == "Top" || itemTypes[idx] == "Outer"
                        ? i % 2 == 0 ? "Short Sleeve" : "Long Sleeve"
                        : null,
                    Length = itemTypes[idx] == "Bottom" ? "Long" : null,
                    Size = i % 4 == 0 ? "S" : i % 4 == 1 ? "M" : i % 4 == 2 ? "L" : "XL",
                    Brand = brands[i % brands.Length],
                    Description = $"{SeedMarker} - item target #{i + 1}",
                    ItemEmbedding = CreateEmbedding(i + 1),
                    IsPublic = i < 12,
                    Status = i % 11 == 0 ? ItemStatus.Inactive : ItemStatus.Active,
                    CreatedAt = createdAt,
                    UpdateAt = createdAt
                };

                targetItems.Add(item);
            }

            await items.AddRangeAsync(targetItems);
            await context.SaveChangesAsync();

            foreach (var item in targetItems)
            {
                targetItemImages.Add(new Image
                {
                    ItemId = item.ItemId,
                    ImageUrl = GetItemImageUrl(item.ItemName ?? string.Empty),
                    OwnerType = "Item",
                    CreatedAt = item.CreatedAt
                });
            }

            await images.AddRangeAsync(targetItemImages);
            await context.SaveChangesAsync();

            // =========================================================
            // 5. Seed items cho các account demo
            // =========================================================
            var allDemoItems = new List<Item>();
            var allDemoItemImages = new List<Image>();

            for (int userIndex = 0; userIndex < demoAccounts.Count; userIndex++)
            {
                var wardrobe = demoWardrobes[userIndex];
                int totalItems = 8 + userIndex % 5;

                for (int j = 0; j < totalItems; j++)
                {
                    int idx = (userIndex + j) % itemNames.Length;
                    var createdAt = now.AddDays(-(userIndex * 2 + j + 1));

                    var item = new Item
                    {
                        WardrobeId = wardrobe.WardrobeId,
                        ItemName = $"{itemNames[idx]} - U{userIndex + 1}-{j + 1}",
                        ItemType = itemTypes[idx],
                        Category = categoryNames[idx % categoryNames.Count],
                        SubCategory = $"Demo-{idx + 1}",
                        Style = j % 2 == 0 ? "Minimal" : "Korean",
                        Gender = j % 3 == 0 ? "Unisex" : j % 2 == 0 ? "Female" : "Male",
                        MainColor = mainColors[(userIndex + j) % mainColors.Length],
                        SubColor = mainColors[(userIndex + j + 3) % mainColors.Length],
                        Material = j % 2 == 0 ? "Cotton" : "Denim",
                        Pattern = j % 3 == 0 ? "Plain" : "Pattern",
                        Fit = j % 2 == 0 ? "Regular" : "Loose",
                        Neckline = itemTypes[idx] == "Top" ? "Round Neck" : null,
                        SleeveLength = itemTypes[idx] == "Top" || itemTypes[idx] == "Outer"
                            ? j % 2 == 0 ? "Short Sleeve" : "Long Sleeve"
                            : null,
                        Length = itemTypes[idx] == "Bottom" ? j % 2 == 0 ? "Long" : "Short" : null,
                        Size = j % 4 == 0 ? "S" : j % 4 == 1 ? "M" : j % 4 == 2 ? "L" : "XL",
                        Brand = brands[(userIndex + j) % brands.Length],
                        Description = $"{SeedMarker} - item demo user {userIndex + 1}, item {j + 1}",
                        ItemEmbedding = CreateEmbedding((userIndex + 1) * 100 + j + 1),
                        IsPublic = j < Math.Max(3, totalItems - 2),
                        Status = j % 7 == 0 ? ItemStatus.Inactive : ItemStatus.Active,
                        CreatedAt = createdAt,
                        UpdateAt = createdAt
                    };

                    allDemoItems.Add(item);
                }
            }

            await items.AddRangeAsync(allDemoItems);
            await context.SaveChangesAsync();

            foreach (var item in allDemoItems)
            {
                allDemoItemImages.Add(new Image
                {
                    ItemId = item.ItemId,
                    ImageUrl = GetItemImageUrl(item.ItemName ?? string.Empty),
                    OwnerType = "Item",
                    CreatedAt = item.CreatedAt
                });
            }

            await images.AddRangeAsync(allDemoItemImages);
            await context.SaveChangesAsync();

            // =========================================================
            // 6. Seed outfit
            // =========================================================
            var allOutfits = new List<Outfit>();

            for (int i = 0; i < 4; i++)
            {
                allOutfits.Add(new Outfit
                {
                    AccountId = targetAccount.Id,
                    OutfitName = $"Outfit target #{i + 1}",
                    ImageUrl = $"https://images.unsplash.com/photo-1529139574466-a303027c1d8b?w=800&sig={i + 1}",
                    CreatedAt = now.AddDays(-(i + 2))
                });
            }

            for (int i = 0; i < demoAccounts.Count; i++)
            {
                int outfitCount = 2 + i % 2;
                for (int j = 0; j < outfitCount; j++)
                {
                    allOutfits.Add(new Outfit
                    {
                        AccountId = demoAccounts[i].Id,
                        OutfitName = $"Outfit user {i + 1} - #{j + 1}",
                        ImageUrl = $"https://images.unsplash.com/photo-1496747611176-843222e1e57c?w=800&sig={(i + 1) * 10 + j}",
                        CreatedAt = now.AddDays(-(i + j + 1))
                    });
                }
            }

            await outfits.AddRangeAsync(allOutfits);
            await context.SaveChangesAsync();

            var targetPublicActiveItems = targetItems
                .Where(x => x.IsPublic == true && x.Status == ItemStatus.Active)
                .Take(8)
                .ToList();

            var targetOutfits = allOutfits.Where(x => x.AccountId == targetAccount.Id).ToList();
            var outfitItemList = new List<OutfitItem>();

            for (int i = 0; i < targetOutfits.Count; i++)
            {
                if (targetPublicActiveItems.Count >= 2)
                {
                    outfitItemList.Add(new OutfitItem
                    {
                        OutfitId = targetOutfits[i].OutfitId,
                        ItemId = targetPublicActiveItems[i % targetPublicActiveItems.Count].ItemId,
                        Slot = "Top"
                    });

                    outfitItemList.Add(new OutfitItem
                    {
                        OutfitId = targetOutfits[i].OutfitId,
                        ItemId = targetPublicActiveItems[(i + 1) % targetPublicActiveItems.Count].ItemId,
                        Slot = "Bottom"
                    });
                }
            }

            foreach (var acc in demoAccounts)
            {
                var userOutfits = allOutfits.Where(x => x.AccountId == acc.Id).ToList();

                var userWardrobeId = demoWardrobes.First(x => x.AccountId == acc.Id).WardrobeId;
                var userItems = allDemoItems.Where(x => x.WardrobeId == userWardrobeId).ToList();

                var activePublicItems = userItems
                    .Where(x => x.IsPublic == true && x.Status == ItemStatus.Active)
                    .ToList();

                for (int i = 0; i < userOutfits.Count; i++)
                {
                    if (activePublicItems.Count >= 2)
                    {
                        outfitItemList.Add(new OutfitItem
                        {
                            OutfitId = userOutfits[i].OutfitId,
                            ItemId = activePublicItems[i % activePublicItems.Count].ItemId,
                            Slot = "Top"
                        });

                        outfitItemList.Add(new OutfitItem
                        {
                            OutfitId = userOutfits[i].OutfitId,
                            ItemId = activePublicItems[(i + 1) % activePublicItems.Count].ItemId,
                            Slot = "Bottom"
                        });
                    }
                }
            }

            await outfitItems.AddRangeAsync(outfitItemList);
            await context.SaveChangesAsync();

            // =========================================================
            // 7. Seed posts
            // =========================================================
            var postList = new List<Post>();
            var postImageList = new List<Image>();

            for (int i = 0; i < 6; i++)
            {
                var p = new Post
                {
                    AccountId = targetAccount.Id,
                    Title = $"Bài đăng test target #{i + 1}",
                    Content = $"{SeedMarker} - nội dung bài đăng target #{i + 1}",
                    CreatedAt = now.AddDays(-(i + 1)),
                    UpdatedAt = now.AddDays(-(i + 1)),
                    IsExpertPost = false,
                    Status = PostStatus.Published,
                    Visibility = PostVisibility.Visible,
                    LikeCount = 3 + i,
                    CommentCount = i,
                    ShareCount = i % 2
                };
                postList.Add(p);
            }

            for (int i = 0; i < demoAccounts.Count; i++)
            {
                int postCount = 3 + i % 4;

                for (int j = 0; j < postCount; j++)
                {
                    var p = new Post
                    {
                        AccountId = demoAccounts[i].Id,
                        Title = $"Post user {i + 1} - #{j + 1}",
                        Content = $"{SeedMarker} - bài đăng demo user {i + 1}, post {j + 1}",
                        CreatedAt = now.AddDays(-(i + j + 1)),
                        UpdatedAt = now.AddDays(-(i + j + 1)),
                        IsExpertPost = false,
                        Status = j % 6 == 0 ? PostStatus.Draft : PostStatus.Published,
                        Visibility = j % 5 == 0 ? PostVisibility.Hidden : PostVisibility.Visible,
                        LikeCount = 2 + j + i,
                        CommentCount = j,
                        ShareCount = j % 3
                    };
                    postList.Add(p);
                }
            }

            await posts.AddRangeAsync(postList);
            await context.SaveChangesAsync();

            foreach (var post in postList.Where(x => x.Status == PostStatus.Published))
            {
                postImageList.Add(new Image
                {
                    PostId = post.PostId,
                    ImageUrl = $"https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=900&sig={post.PostId}",
                    OwnerType = "Post",
                    CreatedAt = post.CreatedAt
                });
            }

            await images.AddRangeAsync(postImageList);
            await context.SaveChangesAsync();

            // =========================================================
            // 8. Seed follow
            // =========================================================
            var followList = new List<Follow>();

            foreach (var acc in demoAccounts.Take(8))
            {
                followList.Add(new Follow
                {
                    UserId = targetAccount.Id,
                    FollowerId = acc.Id,
                    CreatedAt = now.AddDays(-random.Next(1, 20))
                });
            }

            foreach (var acc in demoAccounts.Skip(2).Take(5))
            {
                followList.Add(new Follow
                {
                    UserId = acc.Id,
                    FollowerId = targetAccount.Id,
                    CreatedAt = now.AddDays(-random.Next(1, 15))
                });
            }

            for (int i = 0; i < demoAccounts.Count; i++)
            {
                if (i + 1 < demoAccounts.Count)
                {
                    followList.Add(new Follow
                    {
                        UserId = demoAccounts[i].Id,
                        FollowerId = demoAccounts[i + 1].Id,
                        CreatedAt = now.AddDays(-(i + 1))
                    });
                }

                if (i + 2 < demoAccounts.Count)
                {
                    followList.Add(new Follow
                    {
                        UserId = demoAccounts[i].Id,
                        FollowerId = demoAccounts[i + 2].Id,
                        CreatedAt = now.AddDays(-(i + 2))
                    });
                }
            }

            var distinctFollowList = followList
                .GroupBy(x => new { x.UserId, x.FollowerId })
                .Select(g => g.First())
                .Where(x => x.UserId != x.FollowerId)
                .ToList();

            await follows.AddRangeAsync(distinctFollowList);
            await context.SaveChangesAsync();

            // =========================================================
            // 9. Cập nhật thống kê
            // =========================================================
            var allAccounts = new List<Account> { targetAccount };
            allAccounts.AddRange(demoAccounts);

            foreach (var acc in allAccounts)
            {
                acc.CountPost = await posts.CountAsync(x =>
                    x.AccountId == acc.Id &&
                    x.Status == PostStatus.Published &&
                    x.Visibility == PostVisibility.Visible);

                acc.CountFollower = await follows.CountAsync(x => x.UserId == acc.Id);
                acc.CountFollowing = await follows.CountAsync(x => x.FollowerId == acc.Id);
            }

            await context.SaveChangesAsync();
        }

        private static async Task CreateUserWithRoleAsync(
            UserManager<Account> userManager,
            Account account,
            string password,
            string role)
        {
            var createResult = await userManager.CreateAsync(account, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Tạo tài khoản '{account.Email}' thất bại: {errors}");
            }

            var roleResult = await userManager.AddToRoleAsync(account, role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Gán role '{role}' cho tài khoản '{account.Email}' thất bại: {errors}");
            }
        }

        private static Vector CreateEmbedding(int seed)
        {
            var values = new float[768];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (seed + i) % 17 / 17f;
            }
            return new Vector(values);
        }

        private static string GetAvatarUrl(int index)
        {
            var urls = new[]
            {
                "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=500",
                "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=500",
                "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=500",
                "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=500",
                "https://images.unsplash.com/photo-1504593811423-6dd665756598?w=500",
                "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=500"
            };

            return urls[index % urls.Length];
        }

        private static string GetItemImageUrl(string itemName)
        {
            var lower = itemName.Trim().ToLowerInvariant();

            if (lower.Contains("áo"))
                return "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=700";

            if (lower.Contains("quần"))
                return "https://images.unsplash.com/photo-1542272604-787c3835535d?w=700";

            if (lower.Contains("váy") || lower.Contains("đầm"))
                return "https://images.unsplash.com/photo-1496747611176-843222e1e57c?w=700";

            if (lower.Contains("giày"))
                return "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=700";

            if (lower.Contains("túi"))
                return "https://images.unsplash.com/photo-1584917865442-de89df76afd3?w=700";

            return "https://images.unsplash.com/photo-1483985988355-763728e1935b?w=700";
        }
    }
}