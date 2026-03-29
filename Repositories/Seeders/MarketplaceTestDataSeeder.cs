//using Microsoft.EntityFrameworkCore;
//using Pgvector;
//using Repositories.Constants;
//using Repositories.Entities;
//using System.Globalization;

//namespace Repositories.Seeders
//{
//    public static class MarketplaceTestDataSeeder
//    {
//        private const string TargetEmail = "nvhoang0975@gmail.com";
//        private const string SeedPrefix = "MKTEST";
//        private const string SeedMarker = "MARKETPLACE_TEST_SEED";

//        public static async Task SeedAsync(DbContext context)
//        {
//            var accounts = context.Set<Account>();
//            var wardrobes = context.Set<Wardrobe>();
//            var items = context.Set<Item>();
//            var categories = context.Set<Category>();
//            var images = context.Set<Image>();
//            var outfits = context.Set<Outfit>();
//            var wallets = context.Set<Wallet>();
//            var payments = context.Set<Payment>();
//            var transactions = context.Set<Transaction>();
//            var orders = context.Set<Order>();
//            var orderDetails = context.Set<OrderDetail>();
//            var escrows = context.Set<EscrowSession>();

//            // =========================================================
//            // 1. Tìm user chính để test
//            // =========================================================
//            var targetUser = await accounts.FirstOrDefaultAsync(x =>
//                x.Email != null &&
//                x.Email.Trim().ToLower() == TargetEmail.ToLower());

//            if (targetUser == null)
//            {
//                throw new Exception($"Không tìm thấy user có email {TargetEmail}");
//            }

//            // Nếu đã seed rồi thì bỏ qua
//            var alreadySeeded = await transactions.AnyAsync(x =>
//                x.TransactionCode.StartsWith($"{SeedPrefix}-") ||
//                (x.Description != null && x.Description.Contains(SeedMarker)));

//            if (alreadySeeded)
//            {
//                return;
//            }

//            // =========================================================
//            // 2. Seed category cơ bản
//            // =========================================================
//            var categoryNames = new List<string>
//            {
//                "Áo thun",
//                "Áo sơ mi",
//                "Áo hoodie",
//                "Quần jean",
//                "Quần short",
//                "Chân váy",
//                "Đầm",
//                "Áo khoác",
//                "Giày",
//                "Túi xách",
//                "Phụ kiện"
//            };

//            var existingCategories = await categories.ToListAsync();
//            var categoryMap = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

//            foreach (var name in categoryNames)
//            {
//                var existing = existingCategories.FirstOrDefault(c =>
//                    c.CategoryName.Trim().ToLower() == name.Trim().ToLower());

//                if (existing == null)
//                {
//                    existing = new Category
//                    {
//                        CategoryName = name,
//                        CreatedAt = DateTime.Now
//                    };
//                    categories.Add(existing);
//                    existingCategories.Add(existing);
//                }

//                categoryMap[name] = existing;
//            }

//            await context.SaveChangesAsync();

//            // =========================================================
//            // 3. Đảm bảo targetUser có wallet + wardrobe
//            // =========================================================
//            var targetWallet = await wallets.FirstOrDefaultAsync(x => x.AccountId == targetUser.Id);
//            if (targetWallet == null)
//            {
//                targetWallet = new Wallet
//                {
//                    AccountId = targetUser.Id,
//                    Balance = 0,
//                    LockedBalance = 0,
//                    Currency = "VND",
//                    UpdatedAt = DateTime.Now
//                };
//                wallets.Add(targetWallet);
//            }

//            var targetWardrobe = await wardrobes.FirstOrDefaultAsync(x => x.AccountId == targetUser.Id);
//            if (targetWardrobe == null)
//            {
//                targetWardrobe = new Wardrobe
//                {
//                    AccountId = targetUser.Id,
//                    Name = $"{targetUser.UserName ?? "target"} Wardrobe",
//                    CreatedAt = DateTime.Now
//                };
//                wardrobes.Add(targetWardrobe);
//            }

//            await context.SaveChangesAsync();

//            // =========================================================
//            // 4. Tạo thêm seller accounts
//            // =========================================================
//            var sellerAccounts = new List<Account>();

//            for (int i = 1; i <= 6; i++)
//            {
//                string email = $"seller{i}.{SeedPrefix.ToLower()}@mail.com";
//                string normalizedEmail = email.ToUpperInvariant();
//                string userName = $"seller_{SeedPrefix.ToLower()}_{i}";
//                string normalizedUserName = userName.ToUpperInvariant();

//                var seller = await accounts.FirstOrDefaultAsync(x =>
//                    x.NormalizedEmail == normalizedEmail ||
//                    x.NormalizedUserName == normalizedUserName);

//                if (seller == null)
//                {
//                    seller = new Account
//                    {
//                        Email = email,
//                        NormalizedEmail = normalizedEmail,
//                        UserName = userName,
//                        NormalizedUserName = normalizedUserName,
//                        EmailConfirmed = true,
//                        PhoneNumber = $"0900000{i:D3}",
//                        PhoneNumberConfirmed = true,
//                        SecurityStamp = Guid.NewGuid().ToString(),
//                        ConcurrencyStamp = Guid.NewGuid().ToString(),
//                        CreatedAt = DateTime.Now.AddDays(-30 + i),
//                        Status = "Active",
//                        FreeTryOn = 20,
//                        Description = $"{SeedMarker} - Seller test account {i}",
//                        CountFollower = 10 * i,
//                        CountFollowing = 3 * i,
//                        CountPost = 5 * i,
//                        IsOnline = "False"
//                    };

//                    accounts.Add(seller);
//                    await context.SaveChangesAsync();
//                }

//                sellerAccounts.Add(seller);

//                var sellerWallet = await wallets.FirstOrDefaultAsync(x => x.AccountId == seller.Id);
//                if (sellerWallet == null)
//                {
//                    wallets.Add(new Wallet
//                    {
//                        AccountId = seller.Id,
//                        Balance = 500000m * i,
//                        LockedBalance = 0,
//                        Currency = "VND",
//                        UpdatedAt = DateTime.Now
//                    });
//                }

//                var sellerWardrobe = await wardrobes.FirstOrDefaultAsync(x => x.AccountId == seller.Id);
//                if (sellerWardrobe == null)
//                {
//                    wardrobes.Add(new Wardrobe
//                    {
//                        AccountId = seller.Id,
//                        Name = $"Wardrobe Seller {i}",
//                        CreatedAt = DateTime.Now.AddDays(-20 + i)
//                    });
//                }
//            }

//            // =========================================================
//            // 5. Tạo thêm buyer phụ
//            // =========================================================
//            var extraBuyerAccounts = new List<Account>();

//            for (int i = 1; i <= 3; i++)
//            {
//                string email = $"buyer{i}.{SeedPrefix.ToLower()}@mail.com";
//                string normalizedEmail = email.ToUpperInvariant();
//                string userName = $"buyer_{SeedPrefix.ToLower()}_{i}";
//                string normalizedUserName = userName.ToUpperInvariant();

//                var buyer = await accounts.FirstOrDefaultAsync(x =>
//                    x.NormalizedEmail == normalizedEmail ||
//                    x.NormalizedUserName == normalizedUserName);

//                if (buyer == null)
//                {
//                    buyer = new Account
//                    {
//                        Email = email,
//                        NormalizedEmail = normalizedEmail,
//                        UserName = userName,
//                        NormalizedUserName = normalizedUserName,
//                        EmailConfirmed = true,
//                        PhoneNumber = $"0911111{i:D3}",
//                        PhoneNumberConfirmed = true,
//                        SecurityStamp = Guid.NewGuid().ToString(),
//                        ConcurrencyStamp = Guid.NewGuid().ToString(),
//                        CreatedAt = DateTime.Now.AddDays(-15 + i),
//                        Status = "Active",
//                        FreeTryOn = 15,
//                        Description = $"{SeedMarker} - Buyer test account {i}",
//                        CountFollower = 2 * i,
//                        CountFollowing = 5 * i,
//                        CountPost = 1 * i,
//                        IsOnline = "False"
//                    };

//                    accounts.Add(buyer);
//                    await context.SaveChangesAsync();
//                }

//                extraBuyerAccounts.Add(buyer);

//                var buyerWallet = await wallets.FirstOrDefaultAsync(x => x.AccountId == buyer.Id);
//                if (buyerWallet == null)
//                {
//                    wallets.Add(new Wallet
//                    {
//                        AccountId = buyer.Id,
//                        Balance = 3000000m,
//                        LockedBalance = 0,
//                        Currency = "VND",
//                        UpdatedAt = DateTime.Now
//                    });
//                }

//                var buyerWardrobe = await wardrobes.FirstOrDefaultAsync(x => x.AccountId == buyer.Id);
//                if (buyerWardrobe == null)
//                {
//                    wardrobes.Add(new Wardrobe
//                    {
//                        AccountId = buyer.Id,
//                        Name = $"Wardrobe Buyer {i}",
//                        CreatedAt = DateTime.Now.AddDays(-10 + i)
//                    });
//                }
//            }

//            await context.SaveChangesAsync();

//            // =========================================================
//            // 6. Lấy lại wardrobe map
//            // =========================================================
//            var allSellerIds = sellerAccounts.Select(x => x.Id).ToList();
//            var allBuyerIds = extraBuyerAccounts.Select(x => x.Id).Append(targetUser.Id).ToList();

//            var sellerWardrobes = await wardrobes
//                .Where(x => allSellerIds.Contains(x.AccountId))
//                .ToListAsync();

//            var buyerWardrobes = await wardrobes
//                .Where(x => allBuyerIds.Contains(x.AccountId))
//                .ToListAsync();

//            var sellerWallets = await wallets
//                .Where(x => allSellerIds.Contains(x.AccountId))
//                .ToListAsync();

//            var buyerWallets = await wallets
//                .Where(x => allBuyerIds.Contains(x.AccountId))
//                .ToListAsync();

//            targetWallet = await wallets.FirstAsync(x => x.AccountId == targetUser.Id);

//            // =========================================================
//            // 7. Seed item + image + outfit cho sellers
//            // =========================================================
//            var random = new Random();

//            var itemTemplates = new List<(string Name, string Color, string Pattern, string Style, string Texture, string Fabric, string Brand, string Placement, string Category)>
//            {
//                ("Áo thun basic", "Trắng", "Trơn", "Casual", "Mềm", "Cotton", "UniWear", "Top", "Áo thun"),
//                ("Áo sơ mi form rộng", "Xanh", "Kẻ sọc", "Smart Casual", "Mịn", "Linen", "OfficeX", "Top", "Áo sơ mi"),
//                ("Hoodie local brand", "Đen", "Trơn", "Streetwear", "Dày", "Nỉ", "UrbanFox", "Top", "Áo hoodie"),
//                ("Quần jean slimfit", "Xanh đậm", "Trơn", "Casual", "Cứng", "Denim", "JeanLab", "Bottom", "Quần jean"),
//                ("Quần short kaki", "Be", "Trơn", "Casual", "Mềm", "Kaki", "DailyFit", "Bottom", "Quần short"),
//                ("Chân váy tennis", "Trắng", "Trơn", "Korean", "Nhẹ", "Polyester", "SweetWear", "Bottom", "Chân váy"),
//                ("Đầm hoa nhí", "Hồng", "Hoa", "Vintage", "Mềm", "Voan", "Bloomy", "FullBody", "Đầm"),
//                ("Áo khoác bomber", "Rêu", "Trơn", "Streetwear", "Dày", "Nylon", "OuterLab", "Outer", "Áo khoác"),
//                ("Sneaker trắng", "Trắng", "Trơn", "Minimal", "Mịn", "Canvas", "StepUp", "Feet", "Giày"),
//                ("Túi đeo chéo", "Nâu", "Trơn", "Casual", "Nhám", "Da", "BagSpace", "Accessory", "Túi xách")
//            };

//            var sellerItemsMap = new Dictionary<int, List<Item>>();

//            foreach (var seller in sellerAccounts)
//            {
//                var wardrobe = sellerWardrobes.First(x => x.AccountId == seller.Id);
//                var currentItems = new List<Item>();

//                for (int j = 0; j < 8; j++)
//                {
//                    var t = itemTemplates[(j + seller.Id) % itemTemplates.Count];

//                    var item = new Item
//                    {
//                        WardrobeId = wardrobe.WardrobeId,
//                        ItemName = $"{t.Name} {seller.UserName} #{j + 1}",
//                        Description = $"{SeedMarker} - Item test của seller {seller.UserName}, mẫu {j + 1}",
//                        MainColor = t.Color,
//                        Pattern = t.Pattern,
//                        Style = t.Style,
//                        Texture = t.Texture,
//                        Fabric = t.Fabric,
//                        Brand = t.Brand,
//                        Placement = t.Placement,
//                        StyleScore = 6.5 + (j % 4),
//                        ItemEmbedding = CreateZeroVector768(),
//                        CreatedAt = DateTime.Now.AddDays(-(20 - j)),
//                        UpdateAt = DateTime.Now.AddDays(-(10 - j)),
//                        Status = "Active"
//                    };

//                    item.Categories.Add(categoryMap[t.Category]);

//                    items.Add(item);
//                    currentItems.Add(item);
//                }

//                sellerItemsMap[seller.Id] = currentItems;
//            }

//            await context.SaveChangesAsync();

//            // seed image cho item
//            foreach (var seller in sellerAccounts)
//            {
//                var wardrobe = sellerWardrobes.First(x => x.AccountId == seller.Id);
//                var sellerItems = await items
//                    .Where(x => x.WardrobeId == wardrobe.WardrobeId)
//                    .ToListAsync();

//                foreach (var item in sellerItems)
//                {
//                    images.Add(new Image
//                    {
//                        ItemId = item.ItemId,
//                        ImageUrl = $"https://picsum.photos/seed/{SeedPrefix.ToLower()}-item-{item.ItemId}/600/800",
//                        OwnerType = "Item",
//                        CreatedAt = DateTime.Now
//                    });
//                }
//            }

//            // seed outfit cho seller
//            foreach (var seller in sellerAccounts)
//            {
//                for (int i = 1; i <= 3; i++)
//                {
//                    outfits.Add(new Outfit
//                    {
//                        AccountId = seller.Id,
//                        OutfitName = $"{SeedMarker} Outfit Seller {seller.Id}-{i}",
//                        ImageUrl = $"https://picsum.photos/seed/{SeedPrefix.ToLower()}-outfit-{seller.Id}-{i}/800/1000",
//                        CreatedAt = DateTime.Now.AddDays(-i)
//                    });
//                }
//            }

//            // outfit cho target user
//            for (int i = 1; i <= 3; i++)
//            {
//                outfits.Add(new Outfit
//                {
//                    AccountId = targetUser.Id,
//                    OutfitName = $"{SeedMarker} Outfit Target {i}",
//                    ImageUrl = $"https://picsum.photos/seed/{SeedPrefix.ToLower()}-target-outfit-{i}/800/1000",
//                    CreatedAt = DateTime.Now.AddDays(-i)
//                });
//            }

//            await context.SaveChangesAsync();

//            // =========================================================
//            // 8. Seed top-up payment + transaction cho target user
//            // =========================================================
//            var topUpAmounts = new[] { 2000000m, 3500000m, 5000000m };

//            foreach (var amount in topUpAmounts)
//            {
//                var before = targetWallet.Balance;
//                var after = before + amount;

//                var payment = new Payment
//                {
//                    AccountId = targetUser.Id,
//                    PackageId = null,
//                    Amount = amount,
//                    Provider = PaymentProvider.VnPay,
//                    OrderCode = $"{SeedPrefix}-TOPUP-{Guid.NewGuid():N}".Substring(0, 26),
//                    Status = PaymentStatus.Success,
//                    ExternalTransactionId = $"{SeedPrefix}-EXT-{Guid.NewGuid():N}".Substring(0, 24),
//                    CreatedAt = DateTime.Now.AddDays(-random.Next(10, 30)),
//                    PaidAt = DateTime.Now.AddDays(-random.Next(5, 9))
//                };

//                payments.Add(payment);
//                await context.SaveChangesAsync();

//                transactions.Add(new Transaction
//                {
//                    WalletId = targetWallet.WalletId,
//                    PaymentId = payment.PaymentId,
//                    TransactionCode = $"{SeedPrefix}-TXN-TOPUP-{Guid.NewGuid():N}".Substring(0, 32),
//                    Amount = amount,
//                    BalanceBefore = before,
//                    BalanceAfter = after,
//                    Type = TransactionType.Credit,
//                    ReferenceType = TransactionReferenceType.TopUp,
//                    ReferenceId = payment.PaymentId,
//                    Description = $"{SeedMarker} - Top up ví test {amount.ToString("N0", CultureInfo.InvariantCulture)}",
//                    CreatedAt = DateTime.Now.AddDays(-random.Next(5, 25)),
//                    Status = TransactionStatus.Success
//                });

//                targetWallet.Balance = after;
//                targetWallet.UpdatedAt = DateTime.Now;
//            }

//            await context.SaveChangesAsync();

//            // refresh target wallet
//            targetWallet = await wallets.FirstAsync(x => x.AccountId == targetUser.Id);

//            // =========================================================
//            // 9. Seed orders buyer = targetUser, seller = các seller
//            // =========================================================
//            var orderStatuses = new List<string>
//            {
//                OrderStatus.PendingPayment,
//                OrderStatus.Processing,
//                OrderStatus.Shipping,
//                OrderStatus.Completed,
//                OrderStatus.Cancelled,
//                OrderStatus.Refunded
//            };

//            int orderCounter = 1;

//            foreach (var seller in sellerAccounts)
//            {
//                var wardrobe = sellerWardrobes.First(x => x.AccountId == seller.Id);
//                var sellerItems = await items
//                    .Where(x => x.WardrobeId == wardrobe.WardrobeId)
//                    .OrderBy(x => x.ItemId)
//                    .ToListAsync();

//                if (sellerItems.Count < 2)
//                    continue;

//                for (int k = 0; k < orderStatuses.Count; k++)
//                {
//                    var item1 = sellerItems[k % sellerItems.Count];
//                    var item2 = sellerItems[(k + 1) % sellerItems.Count];

//                    var unitPrice1 = 120000m + (10000m * k);
//                    var unitPrice2 = 180000m + (15000m * k);
//                    var qty1 = 1 + (k % 2);
//                    var qty2 = 1;

//                    var subTotal = unitPrice1 * qty1 + unitPrice2 * qty2;
//                    var serviceFee = Math.Round(subTotal * 0.05m, 0);
//                    var totalAmount = subTotal + serviceFee;

//                    var order = new Order
//                    {
//                        BuyerId = targetUser.Id,
//                        SellerId = seller.Id,
//                        SubTotal = subTotal,
//                        ServiceFee = serviceFee,
//                        TotalAmount = totalAmount,
//                        Status = orderStatuses[k],
//                        Note = $"{SeedMarker} - Order test #{orderCounter}",
//                        ShippingAddress = $"123 Đường Test, Phường Test, Quận Test {orderCounter}",
//                        ReceiverName = "Nguyen Van Hoang",
//                        ReceiverPhone = "0909999999",
//                        CreatedAt = DateTime.Now.AddDays(-(20 - orderCounter)),
//                        UpdatedAt = DateTime.Now.AddDays(-(19 - orderCounter))
//                    };

//                    orders.Add(order);
//                    await context.SaveChangesAsync();

//                    orderDetails.AddRange(
//                        new OrderDetail
//                        {
//                            OrderId = order.OrderId,
//                            ItemId = item1.ItemId,
//                            OutfitId = null,
//                            ItemName = item1.ItemName,
//                            Quantity = qty1,
//                            UnitPrice = unitPrice1,
//                            ImageUrl = $"https://picsum.photos/seed/order-item-{item1.ItemId}/500/700"
//                        },
//                        new OrderDetail
//                        {
//                            OrderId = order.OrderId,
//                            ItemId = item2.ItemId,
//                            OutfitId = null,
//                            ItemName = item2.ItemName,
//                            Quantity = qty2,
//                            UnitPrice = unitPrice2,
//                            ImageUrl = $"https://picsum.photos/seed/order-item-{item2.ItemId}/500/700"
//                        });

//                    await context.SaveChangesAsync();

//                    // Payment + transaction + escrow theo status
//                    await CreateOrderFinancialDataAsync(
//                        context: context,
//                        buyerWallet: targetWallet,
//                        sellerWallet: sellerWallets.First(x => x.AccountId == seller.Id),
//                        order: order,
//                        seedCounter: orderCounter);

//                    orderCounter++;
//                }
//            }

//            // =========================================================
//            // 10. Seed thêm orders buyer phụ mua của targetUser/sellers
//            // =========================================================
//            var targetOutfits = await outfits
//                .Where(x => x.AccountId == targetUser.Id)
//                .OrderBy(x => x.OutfitId)
//                .ToListAsync();

//            foreach (var buyer in extraBuyerAccounts)
//            {
//                var buyerWallet = buyerWallets.First(x => x.AccountId == buyer.Id);
//                var seller = sellerAccounts[random.Next(sellerAccounts.Count)];
//                var sellerWallet = sellerWallets.First(x => x.AccountId == seller.Id);
//                var sellerWardrobe = sellerWardrobes.First(x => x.AccountId == seller.Id);
//                var sellerItems = await items
//                    .Where(x => x.WardrobeId == sellerWardrobe.WardrobeId)
//                    .OrderBy(x => x.ItemId)
//                    .Take(2)
//                    .ToListAsync();

//                if (!sellerItems.Any()) continue;

//                var item = sellerItems.First();
//                var unitPrice = 250000m + random.Next(1, 6) * 20000m;
//                var qty = 1;
//                var subTotal = unitPrice * qty;
//                var serviceFee = Math.Round(subTotal * 0.05m, 0);
//                var total = subTotal + serviceFee;

//                var order = new Order
//                {
//                    BuyerId = buyer.Id,
//                    SellerId = seller.Id,
//                    SubTotal = subTotal,
//                    ServiceFee = serviceFee,
//                    TotalAmount = total,
//                    Status = OrderStatus.Completed,
//                    Note = $"{SeedMarker} - Extra buyer order",
//                    ShippingAddress = "456 Buyer phụ, TP Test",
//                    ReceiverName = buyer.UserName ?? "Buyer Test",
//                    ReceiverPhone = "0912222333",
//                    CreatedAt = DateTime.Now.AddDays(-random.Next(1, 7)),
//                    UpdatedAt = DateTime.Now.AddDays(-random.Next(1, 5))
//                };

//                orders.Add(order);
//                await context.SaveChangesAsync();

//                orderDetails.Add(new OrderDetail
//                {
//                    OrderId = order.OrderId,
//                    ItemId = item.ItemId,
//                    OutfitId = targetOutfits.Any() ? targetOutfits[random.Next(targetOutfits.Count)].OutfitId : null,
//                    ItemName = item.ItemName,
//                    Quantity = qty,
//                    UnitPrice = unitPrice,
//                    ImageUrl = $"https://picsum.photos/seed/order-extra-{item.ItemId}/500/700"
//                });

//                await context.SaveChangesAsync();

//                await CreateOrderFinancialDataAsync(
//                    context: context,
//                    buyerWallet: buyerWallet,
//                    sellerWallet: sellerWallet,
//                    order: order,
//                    seedCounter: 1000 + buyer.Id);
//            }

//            await context.SaveChangesAsync();
//        }

//        // =========================================================
//        // Helpers
//        // =========================================================

//        private static Vector CreateZeroVector768()
//        {
//            return new Vector(Enumerable.Repeat(0f, 768).ToArray());
//        }

//        private static async Task CreateOrderFinancialDataAsync(
//            DbContext context,
//            Wallet buyerWallet,
//            Wallet sellerWallet,
//            Order order,
//            int seedCounter)
//        {
//            var payments = context.Set<Payment>();
//            var transactions = context.Set<Transaction>();
//            var escrows = context.Set<EscrowSession>();

//            // PendingPayment: chưa thanh toán
//            if (order.Status == OrderStatus.PendingPayment)
//            {
//                var pendingPayment = new Payment
//                {
//                    AccountId = order.BuyerId,
//                    Amount = order.TotalAmount,
//                    Provider = PaymentProvider.VnPay,
//                    OrderCode = $"{SeedPrefix}-PAY-PENDING-{order.OrderId}",
//                    Status = PaymentStatus.Pending,
//                    ExternalTransactionId = $"{SeedPrefix}-EXT-PENDING-{order.OrderId}",
//                    CreatedAt = order.CreatedAt,
//                    PaidAt = null
//                };

//                payments.Add(pendingPayment);
//                await context.SaveChangesAsync();
//                return;
//            }

//            // Các case còn lại: giả lập buyer đã thanh toán
//            var payBefore = buyerWallet.Balance;
//            var payAfter = payBefore - order.TotalAmount;

//            var successOrCancelledPaymentStatus =
//                order.Status == OrderStatus.Cancelled ? PaymentStatus.Cancelled :
//                order.Status == OrderStatus.Refunded ? PaymentStatus.Success :
//                PaymentStatus.Success;

//            var payment = new Payment
//            {
//                AccountId = order.BuyerId,
//                Amount = order.TotalAmount,
//                Provider = PaymentProvider.VnPay,
//                OrderCode = $"{SeedPrefix}-PAY-{order.OrderId}",
//                Status = successOrCancelledPaymentStatus,
//                ExternalTransactionId = $"{SeedPrefix}-EXT-{order.OrderId}",
//                CreatedAt = order.CreatedAt,
//                PaidAt = order.Status == OrderStatus.Cancelled ? null : order.CreatedAt.AddMinutes(5)
//            };

//            payments.Add(payment);
//            await context.SaveChangesAsync();

//            // Cancelled: có thể xem như payment bị hủy trước khi trừ ví
//            if (order.Status == OrderStatus.Cancelled)
//            {
//                transactions.Add(new Transaction
//                {
//                    WalletId = buyerWallet.WalletId,
//                    PaymentId = payment.PaymentId,
//                    TransactionCode = $"{SeedPrefix}-TXN-CANCEL-{order.OrderId}",
//                    Amount = order.TotalAmount,
//                    BalanceBefore = payBefore,
//                    BalanceAfter = payBefore,
//                    Type = TransactionType.Debit,
//                    ReferenceType = TransactionReferenceType.OrderPayment,
//                    ReferenceId = order.OrderId,
//                    Description = $"{SeedMarker} - Order cancelled #{order.OrderId}",
//                    CreatedAt = order.CreatedAt.AddMinutes(10),
//                    Status = TransactionStatus.Cancelled
//                });

//                await context.SaveChangesAsync();
//                return;
//            }

//            // Buyer thanh toán thành công
//            transactions.Add(new Transaction
//            {
//                WalletId = buyerWallet.WalletId,
//                PaymentId = payment.PaymentId,
//                TransactionCode = $"{SeedPrefix}-TXN-PAY-{order.OrderId}",
//                Amount = order.TotalAmount,
//                BalanceBefore = payBefore,
//                BalanceAfter = payAfter,
//                Type = TransactionType.Debit,
//                ReferenceType = TransactionReferenceType.OrderPayment,
//                ReferenceId = order.OrderId,
//                Description = $"{SeedMarker} - Buyer paid order #{order.OrderId}",
//                CreatedAt = order.CreatedAt.AddMinutes(10),
//                Status = TransactionStatus.Success
//            });

//            buyerWallet.Balance = payAfter;
//            buyerWallet.UpdatedAt = DateTime.Now;

//            // Escrow giữ tiền nếu đã thanh toán
//            var escrow = new EscrowSession
//            {
//                OrderId = order.OrderId,
//                EventId = null,
//                SenderId = order.BuyerId,
//                ReceiverId = order.SellerId,
//                Amount = order.SubTotal,
//                ServiceFee = order.ServiceFee,
//                Status = order.Status == OrderStatus.Refunded
//                    ? EscrowStatus.Refunded
//                    : order.Status == OrderStatus.Completed
//                        ? EscrowStatus.Released
//                        : EscrowStatus.Held,
//                Description = $"{SeedMarker} - Escrow for order #{order.OrderId}",
//                CreatedAt = order.CreatedAt.AddMinutes(12),
//                ResolvedAt = order.Status == OrderStatus.Completed || order.Status == OrderStatus.Refunded
//                    ? order.CreatedAt.AddDays(3)
//                    : null
//            };

//            escrows.Add(escrow);
//            await context.SaveChangesAsync();

//            // Completed => cộng tiền seller
//            if (order.Status == OrderStatus.Completed)
//            {
//                var sellerBefore = sellerWallet.Balance;
//                var sellerAfter = sellerBefore + order.SubTotal;

//                transactions.Add(new Transaction
//                {
//                    WalletId = sellerWallet.WalletId,
//                    PaymentId = null,
//                    TransactionCode = $"{SeedPrefix}-TXN-SELLER-{order.OrderId}",
//                    Amount = order.SubTotal,
//                    BalanceBefore = sellerBefore,
//                    BalanceAfter = sellerAfter,
//                    Type = TransactionType.Credit,
//                    ReferenceType = TransactionReferenceType.Adjustment,
//                    ReferenceId = order.OrderId,
//                    Description = $"{SeedMarker} - Seller received money from completed order #{order.OrderId}",
//                    CreatedAt = order.CreatedAt.AddDays(3),
//                    Status = TransactionStatus.Success
//                });

//                sellerWallet.Balance = sellerAfter;
//                sellerWallet.UpdatedAt = DateTime.Now;
//            }

//            // Refunded => hoàn tiền buyer
//            if (order.Status == OrderStatus.Refunded)
//            {
//                var refundBefore = buyerWallet.Balance;
//                var refundAfter = refundBefore + order.TotalAmount;

//                transactions.Add(new Transaction
//                {
//                    WalletId = buyerWallet.WalletId,
//                    PaymentId = payment.PaymentId,
//                    TransactionCode = $"{SeedPrefix}-TXN-REFUND-{order.OrderId}",
//                    Amount = order.TotalAmount,
//                    BalanceBefore = refundBefore,
//                    BalanceAfter = refundAfter,
//                    Type = TransactionType.Credit,
//                    ReferenceType = TransactionReferenceType.OrderRefund,
//                    ReferenceId = order.OrderId,
//                    Description = $"{SeedMarker} - Refund for order #{order.OrderId}",
//                    CreatedAt = order.CreatedAt.AddDays(2),
//                    Status = TransactionStatus.Success
//                });

//                buyerWallet.Balance = refundAfter;
//                buyerWallet.UpdatedAt = DateTime.Now;
//            }

//            await context.SaveChangesAsync();
//        }
//    }
//}