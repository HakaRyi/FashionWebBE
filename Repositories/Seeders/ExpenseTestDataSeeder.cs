//using Microsoft.EntityFrameworkCore;
//using Repositories.Constants;
//using Repositories.Entities;

//namespace Repositories.Seeders
//{
//    public static class ExpenseTestDataSeeder
//    {
//        private const string TargetEmail = "nvhoang0975@gmail.com";
//        private const string SeedPrefix = "EXPTEST";
//        private const string SeedNote = "EXPENSE_TEST_SEED";

//        public static async Task SeedAsync(DbContext context)
//        {
//            var accounts = context.Set<Account>();
//            var wallets = context.Set<Wallet>();
//            var payments = context.Set<Payment>();
//            var transactions = context.Set<Transaction>();
//            var orders = context.Set<Order>();
//            var orderDetails = context.Set<OrderDetail>();

//            var targetUser = await accounts
//                .FirstOrDefaultAsync(x =>
//                    x.Email != null &&
//                    x.Email.Trim().ToLower() == TargetEmail);

//            if (targetUser == null)
//            {
//                throw new Exception($"Không tìm thấy user có email {TargetEmail}");
//            }

//            var alreadySeeded = await transactions.AnyAsync(x =>
//                x.Wallet.AccountId == targetUser.Id &&
//                x.TransactionCode.StartsWith(SeedPrefix));

//            if (alreadySeeded)
//            {
//                return;
//            }

//            var seller = await accounts
//                .Where(x => x.Id != targetUser.Id)
//                .OrderBy(x => x.Id)
//                .FirstOrDefaultAsync();

//            await using var dbTransaction = await context.Database.BeginTransactionAsync();

//            try
//            {
//                var buyerWallet = await wallets.FirstOrDefaultAsync(x => x.AccountId == targetUser.Id);
//                if (buyerWallet == null)
//                {
//                    buyerWallet = new Wallet
//                    {
//                        AccountId = targetUser.Id,
//                        Balance = 0,
//                        LockedBalance = 0,
//                        Currency = "VND",
//                        UpdatedAt = DateTime.UtcNow
//                    };
//                    await wallets.AddAsync(buyerWallet);
//                    await context.SaveChangesAsync();
//                }

//                Wallet? sellerWallet = null;
//                if (seller != null)
//                {
//                    sellerWallet = await wallets.FirstOrDefaultAsync(x => x.AccountId == seller.Id);
//                    if (sellerWallet == null)
//                    {
//                        sellerWallet = new Wallet
//                        {
//                            AccountId = seller.Id,
//                            Balance = 0,
//                            LockedBalance = 0,
//                            Currency = "VND",
//                            UpdatedAt = DateTime.UtcNow
//                        };
//                        await wallets.AddAsync(sellerWallet);
//                        await context.SaveChangesAsync();
//                    }
//                }

//                decimal buyerBalance = buyerWallet.Balance;
//                decimal buyerLocked = buyerWallet.LockedBalance;

//                int txCounter = 1;
//                int payCounter = 1;
//                int refCounter = 1000;

//                var firstPackage = await packages.OrderBy(x => x.PackageId).FirstOrDefaultAsync();

//                string NextTxCode(DateTime when)
//                    => $"{SeedPrefix}-TX-{when:yyyyMMddHHmmss}-{txCounter++.ToString("D3")}";

//                string NextOrderCode(DateTime when, string prefix)
//                    => $"{prefix}-{when:yyMMdd}-{payCounter++.ToString("D4")}";

//                async Task<Payment> CreatePaymentAsync(
//                    int accountId,
//                    decimal amount,
//                    string provider,
//                    string status,
//                    DateTime createdAt,
//                    DateTime? paidAt = null,
//                    string prefix = "PAY")
//                {
//                    var payment = new Payment
//                    {
//                        AccountId = accountId,
//                        Amount = amount,
//                        Provider = provider,
//                        OrderCode = NextOrderCode(createdAt, prefix),
//                        Status = status,
//                        CreatedAt = createdAt,
//                        PaidAt = paidAt
//                    };

//                    await payments.AddAsync(payment);
//                    await context.SaveChangesAsync();
//                    return payment;
//                }

//                async Task<Transaction> AddBuyerTransactionAsync(
//                    decimal amount,
//                    string type,
//                    string referenceType,
//                    int? referenceId,
//                    string description,
//                    DateTime createdAt,
//                    string status = TransactionStatus.Success,
//                    int? paymentId = null)
//                {
//                    var before = buyerBalance;
//                    var after = type == TransactionType.Credit
//                        ? before + amount
//                        : before - amount;

//                    var tx = new Transaction
//                    {
//                        WalletId = buyerWallet.WalletId,
//                        PaymentId = paymentId,
//                        TransactionCode = NextTxCode(createdAt),
//                        Amount = amount,
//                        BalanceBefore = before,
//                        BalanceAfter = after,
//                        Type = type,
//                        ReferenceType = referenceType,
//                        ReferenceId = referenceId,
//                        Description = description,
//                        CreatedAt = createdAt,
//                        Status = status
//                    };

//                    buyerBalance = after;
//                    buyerWallet.Balance = buyerBalance;
//                    buyerWallet.UpdatedAt = createdAt;

//                    await transactions.AddAsync(tx);
//                    await context.SaveChangesAsync();
//                    return tx;
//                }

//                async Task<Transaction> AddSellerTransactionAsync(
//                    Wallet targetWallet,
//                    decimal amount,
//                    string type,
//                    string referenceType,
//                    int? referenceId,
//                    string description,
//                    DateTime createdAt,
//                    string status = TransactionStatus.Success)
//                {
//                    var before = targetWallet.Balance;
//                    var after = type == TransactionType.Credit
//                        ? before + amount
//                        : before - amount;

//                    var tx = new Transaction
//                    {
//                        WalletId = targetWallet.WalletId,
//                        PaymentId = null,
//                        TransactionCode = $"{SeedPrefix}-SELLER-{createdAt:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
//                        Amount = amount,
//                        BalanceBefore = before,
//                        BalanceAfter = after,
//                        Type = type,
//                        ReferenceType = referenceType,
//                        ReferenceId = referenceId,
//                        Description = description,
//                        CreatedAt = createdAt,
//                        Status = status
//                    };

//                    targetWallet.Balance = after;
//                    targetWallet.UpdatedAt = createdAt;

//                    await transactions.AddAsync(tx);
//                    await context.SaveChangesAsync();
//                    return tx;
//                }

//                async Task<Order> CreateOrderAsync(
//                    int buyerId,
//                    int sellerId,
//                    decimal subTotal,
//                    decimal serviceFee,
//                    string status,
//                    DateTime createdAt,
//                    DateTime? updatedAt,
//                    string note)
//                {
//                    var order = new Order
//                    {
//                        BuyerId = buyerId,
//                        SellerId = sellerId,
//                        SubTotal = subTotal,
//                        ServiceFee = serviceFee,
//                        TotalAmount = subTotal + serviceFee,
//                        Status = status,
//                        Note = note,
//                        ShippingAddress = "123 Nguyễn Huệ, Quận 1, TP.HCM",
//                        ReceiverName = "Nguyễn Văn Hoàng",
//                        ReceiverPhone = "0909123456",
//                        CreatedAt = createdAt,
//                        UpdatedAt = updatedAt
//                    };

//                    await orders.AddAsync(order);
//                    await context.SaveChangesAsync();

//                    var detail = new OrderDetail
//                    {
//                        OrderId = order.OrderId,
//                        OutfitId = null,
//                        ItemId = null,
//                        Quantity = 1,
//                        UnitPrice = subTotal
//                    };

//                    await orderDetails.AddAsync(detail);
//                    await context.SaveChangesAsync();

//                    return order;
//                }

//                async Task SeedTopUpAsync(decimal amount, string provider, DateTime when)
//                {
//                    var payment = await CreatePaymentAsync(
//                        targetUser.Id,
//                        amount,
//                        provider,
//                        PaymentStatus.Success,
//                        when,
//                        when.AddMinutes(2),
//                        "TOP");

//                    await AddBuyerTransactionAsync(
//                        amount,
//                        TransactionType.Credit,
//                        TransactionReferenceType.TopUp,
//                        payment.PaymentId,
//                        $"Nạp tiền qua {provider} - {SeedNote}",
//                        when.AddMinutes(3),
//                        TransactionStatus.Success,
//                        payment.PaymentId);
//                }

//                async Task SeedFailedTopUpAsync(decimal amount, string provider, DateTime when, string finalStatus)
//                {
//                    await CreatePaymentAsync(
//                        targetUser.Id,
//                        amount,
//                        provider,
//                        finalStatus,
//                        when,
//                        null,
//                        "TOP");
//                }

//                async Task SeedCompletedOrderAsync(decimal subTotal, decimal serviceFee, DateTime when)
//                {
//                    if (seller == null || sellerWallet == null) return;

//                    var order = await CreateOrderAsync(
//                        targetUser.Id,
//                        seller.Id,
//                        subTotal,
//                        serviceFee,
//                        "Completed",
//                        when,
//                        when.AddDays(3),
//                        $"{SeedNote} - completed order");

//                    buyerLocked += order.TotalAmount;
//                    buyerWallet.LockedBalance = buyerLocked;
//                    buyerWallet.UpdatedAt = when;

//                    await AddBuyerTransactionAsync(
//                        order.TotalAmount,
//                        TransactionType.Debit,
//                        TransactionReferenceType.OrderPayment,
//                        order.OrderId,
//                        $"Giữ tiền thanh toán đơn hàng #{order.OrderId} - {SeedNote}",
//                        when.AddMinutes(10));

//                    buyerLocked -= order.TotalAmount;
//                    buyerWallet.LockedBalance = buyerLocked;
//                    buyerWallet.UpdatedAt = when.AddDays(3);

//                    await AddSellerTransactionAsync(
//                        sellerWallet,
//                        order.SubTotal,
//                        TransactionType.Credit,
//                        TransactionReferenceType.OrderPayment,
//                        order.OrderId,
//                        $"Nhận tiền từ đơn hàng #{order.OrderId} - {SeedNote}",
//                        when.AddDays(3).AddMinutes(30));
//                }

//                async Task SeedRefundedOrderAsync(decimal subTotal, decimal serviceFee, DateTime when)
//                {
//                    if (seller == null) return;

//                    var order = await CreateOrderAsync(
//                        targetUser.Id,
//                        seller.Id,
//                        subTotal,
//                        serviceFee,
//                        "Refunded",
//                        when,
//                        when.AddDays(2),
//                        $"{SeedNote} - refunded order");

//                    buyerLocked += order.TotalAmount;
//                    buyerWallet.LockedBalance = buyerLocked;
//                    buyerWallet.UpdatedAt = when;

//                    await AddBuyerTransactionAsync(
//                        order.TotalAmount,
//                        TransactionType.Debit,
//                        TransactionReferenceType.OrderPayment,
//                        order.OrderId,
//                        $"Giữ tiền thanh toán đơn hàng #{order.OrderId} - {SeedNote}",
//                        when.AddMinutes(8));

//                    buyerLocked -= order.TotalAmount;
//                    buyerWallet.LockedBalance = buyerLocked;
//                    buyerWallet.UpdatedAt = when.AddDays(2);

//                    await AddBuyerTransactionAsync(
//                        order.TotalAmount,
//                        TransactionType.Credit,
//                        TransactionReferenceType.OrderRefund,
//                        order.OrderId,
//                        $"Hoàn tiền đơn hàng #{order.OrderId} - {SeedNote}",
//                        when.AddDays(2).AddMinutes(20));
//                }

//                async Task SeedShippingOrderAsync(decimal subTotal, decimal serviceFee, DateTime when)
//                {
//                    if (seller == null) return;

//                    var order = await CreateOrderAsync(
//                        targetUser.Id,
//                        seller.Id,
//                        subTotal,
//                        serviceFee,
//                        "Shipping",
//                        when,
//                        when.AddDays(1),
//                        $"{SeedNote} - shipping order");

//                    buyerLocked += order.TotalAmount;
//                    buyerWallet.LockedBalance = buyerLocked;
//                    buyerWallet.UpdatedAt = when;

//                    await AddBuyerTransactionAsync(
//                        order.TotalAmount,
//                        TransactionType.Debit,
//                        TransactionReferenceType.OrderPayment,
//                        order.OrderId,
//                        $"Giữ tiền thanh toán đơn hàng #{order.OrderId} - {SeedNote}",
//                        when.AddMinutes(12));
//                }

//                var cursor = DateTime.UtcNow.Date.AddDays(-85).AddHours(9);

//                await SeedTopUpAsync(500000m, "VNPAY", cursor);
//                cursor = cursor.AddDays(3);

//                await SeedTopUpAsync(300000m, "ZALOPAY", cursor);
//                cursor = cursor.AddDays(2);

//                await AddBuyerTransactionAsync(
//                    50000m,
//                    TransactionType.Credit,
//                    TransactionReferenceType.EventReward,
//                    refCounter++,
//                    $"Thưởng sự kiện tháng - {SeedNote}",
//                    cursor);
//                cursor = cursor.AddDays(1);

//                await AddBuyerTransactionAsync(
//                    29000m,
//                    TransactionType.Debit,
//                    TransactionReferenceType.TryOn,
//                    refCounter++,
//                    $"Thanh toán Try-On premium - {SeedNote}",
//                    cursor);
//                cursor = cursor.AddDays(2);

//                if (firstPackage != null)
//                {
//                    await AddBuyerTransactionAsync(
//                        99000m,
//                        TransactionType.Debit,
//                        TransactionReferenceType.Adjustment,
//                        firstPackage.PackageId,
//                        $"Mua gói dịch vụ #{firstPackage.PackageId} - {SeedNote}",
//                        cursor);
//                    cursor = cursor.AddDays(2);
//                }

//                await SeedFailedTopUpAsync(450000m, "VNPAY", cursor, PaymentStatus.Failed);
//                cursor = cursor.AddDays(2);

//                await SeedCompletedOrderAsync(120000m, 10000m, cursor);
//                cursor = cursor.AddDays(4);

//                await SeedRefundedOrderAsync(180000m, 20000m, cursor);
//                cursor = cursor.AddDays(4);

//                await AddBuyerTransactionAsync(
//                    100000m,
//                    TransactionType.Debit,
//                    TransactionReferenceType.Withdraw,
//                    refCounter++,
//                    $"Rút tiền về tài khoản ngân hàng - {SeedNote}",
//                    cursor);
//                cursor = cursor.AddDays(2);

//                await AddBuyerTransactionAsync(
//                    25000m,
//                    TransactionType.Credit,
//                    TransactionReferenceType.Adjustment,
//                    refCounter++,
//                    $"Điều chỉnh số dư từ hệ thống - {SeedNote}",
//                    cursor);
//                cursor = cursor.AddDays(3);

//                await SeedTopUpAsync(700000m, "VNPAY", cursor);
//                cursor = cursor.AddDays(2);

//                await SeedShippingOrderAsync(320000m, 30000m, cursor);
//                cursor = cursor.AddDays(3);

//                await SeedCompletedOrderAsync(160000m, 15000m, cursor);
//                cursor = cursor.AddDays(3);

//                await SeedCompletedOrderAsync(75000m, 5000m, cursor);
//                cursor = cursor.AddDays(2);

//                await AddBuyerTransactionAsync(
//                    15000m,
//                    TransactionType.Debit,
//                    TransactionReferenceType.TryOn,
//                    refCounter++,
//                    $"Thanh toán Try-On nhanh - {SeedNote}",
//                    cursor);
//                cursor = cursor.AddDays(1);

//                await AddBuyerTransactionAsync(
//                    20000m,
//                    TransactionType.Credit,
//                    TransactionReferenceType.EventReward,
//                    refCounter++,
//                    $"Thưởng tham gia thử thách - {SeedNote}",
//                    cursor);
//                cursor = cursor.AddDays(2);

//                await SeedFailedTopUpAsync(250000m, "ZALOPAY", cursor, PaymentStatus.Cancelled);
//                cursor = cursor.AddDays(2);

//                await SeedRefundedOrderAsync(90000m, 10000m, cursor);
//                cursor = cursor.AddDays(3);

//                await SeedTopUpAsync(250000m, "VNPAY", cursor);
//                cursor = cursor.AddDays(2);

//                await AddBuyerTransactionAsync(
//                    40000m,
//                    TransactionType.Debit,
//                    TransactionReferenceType.Adjustment,
//                    refCounter++,
//                    $"Điều chỉnh giảm số dư - {SeedNote}",
//                    cursor);
//                cursor = cursor.AddDays(1);

//                await AddBuyerTransactionAsync(
//                    60000m,
//                    TransactionType.Credit,
//                    TransactionReferenceType.OrderRefund,
//                    refCounter++,
//                    $"Hoàn tiền hỗ trợ khách hàng - {SeedNote}",
//                    cursor);

//                buyerWallet.Balance = buyerBalance;
//                buyerWallet.LockedBalance = buyerLocked;
//                buyerWallet.UpdatedAt = DateTime.UtcNow;

//                await context.SaveChangesAsync();
//                await dbTransaction.CommitAsync();
//            }
//            catch
//            {
//                await dbTransaction.RollbackAsync();
//                throw;
//            }
//        }
//    }
//}