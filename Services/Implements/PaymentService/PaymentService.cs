using Microsoft.AspNetCore.Http;
using Repositories.Constants;
using Repositories.Entities;
using Repositories.Repos.PaymentsRespo;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Implements.NotificationImp;
using Services.Request.NotificationReq;
using Services.Request.PaymentReq;
using Services.Response.PaymentResp;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Services.Implements.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly IWalletRepository _walletRepo;

        public PaymentService(
            IPaymentRepository paymentRepo,
            ITransactionRepository transactionRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            IWalletRepository walletRepository)
        {
            _paymentRepo = paymentRepo;
            _transactionRepo = transactionRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _walletRepo = walletRepository;
        }

        public async Task<PaymentResponse?> CreatePackagePaymentAsync(PaymentRequest request)
        {
            await Task.CompletedTask;
            throw new Exception("Hệ thống hiện không còn hỗ trợ thanh toán gói.");
        }

        public async Task<PaymentResponse?> CreateTopUpPaymentAsync(decimal amount)
        {
            if (amount < 10000)
                throw new Exception("Số tiền tối thiểu là 10,000đ.");

            int accountId = _currentUserService.GetRequiredUserId();
            string orderCode = $"TOP-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";

            var payment = new Payment
            {
                AccountId = accountId,
                Amount = amount,
                Provider = PaymentProvider.VnPay,
                OrderCode = orderCode,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return new PaymentResponse
            {
                PaymentId = payment.PaymentId,
                OrderCode = payment.OrderCode!,
                Amount = payment.Amount,
                Provider = payment.Provider!,
                Status = payment.Status!,
                Description = "Nạp tiền vào ví",
                CreatedAt = payment.CreatedAt ?? DateTime.UtcNow,
                PaymentUrl = null
            };
        }

        // API cũ create-order: giữ lại cho ZaloPay top-up
        public async Task<object> CreateOrderAsync(CreateOrderRequest request)
        {
            var appTransId = DateTime.UtcNow.ToString("yyMMdd") + "_" + Guid.NewGuid().ToString("N")[..6];

            var payment = new Payment
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                Provider = PaymentProvider.ZaloPay,
                OrderCode = appTransId,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return await CreateZaloOrder(appTransId, request.Amount, request.AccountId);
        }

        public async Task<object> CreateVnPayOrderAsync(CreateOrderRequest request, string ipAddress)
        {
            var vnpReturnUrl = "https://0992-118-71-8-38.ngrok-free.app/api/payment/vnpay-return";
            var vnpUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var vnpTmnCode = "1K7OF3A1".Trim();
            var vnpHashSecret = "9G9LFM1HFI9KFAM2LGS7WQY4K2EXM51Z".Trim();

            var orderCode = DateTime.UtcNow.ToString("yyMMdd") + "_" + Guid.NewGuid().ToString("N")[..6];
            long amountLong = (long)request.Amount;

            var payment = new Payment
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                Provider = PaymentProvider.VnPay,
                OrderCode = orderCode,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            var vnpParams = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Amount", (amountLong * 100).ToString() },
                { "vnp_Command", "pay" },
                { "vnp_CreateDate", DateTime.UtcNow.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Nap tien {amountLong} VND" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", vnpReturnUrl },
                { "vnp_TmnCode", vnpTmnCode },
                { "vnp_TxnRef", orderCode },
                { "vnp_Version", "2.1.0" }
            };

            if (ipAddress == "::1" || ipAddress.StartsWith("192.168") || ipAddress.StartsWith("127.0"))
                ipAddress = "113.190.238.169";

            vnpParams.Add("vnp_IpAddr", ipAddress);

            var hashDataBuilder = new StringBuilder();
            var queryBuilder = new StringBuilder();

            foreach (var kv in vnpParams)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    hashDataBuilder.Append(System.Net.WebUtility.UrlEncode(kv.Key))
                        .Append('=')
                        .Append(System.Net.WebUtility.UrlEncode(kv.Value))
                        .Append('&');

                    queryBuilder.Append(System.Net.WebUtility.UrlEncode(kv.Key))
                        .Append('=')
                        .Append(System.Net.WebUtility.UrlEncode(kv.Value))
                        .Append('&');
                }
            }

            string hashData = hashDataBuilder.ToString().TrimEnd('&');
            string query = queryBuilder.ToString().TrimEnd('&');
            string secureHash = HmacSha512(vnpHashSecret, hashData);
            string paymentUrl = $"{vnpUrl}?{query}&vnp_SecureHash={secureHash}";

            return new { order_url = paymentUrl };
        }

        public async Task HandleCallbackAsync(ZaloCallbackRequest request)
        {
            using var jsonData = JsonDocument.Parse(request.data);
            var root = jsonData.RootElement;

            string orderCode = root.GetProperty("app_trans_id").GetString()!;
            int status = root.GetProperty("status").GetInt32();

            bool isSuccess = status == 1;
            await ProcessTopUpPaymentAsync(orderCode, isSuccess);
        }

        public async Task<bool> ProcessPaymentCallbackAsync(string orderCode, bool isSuccess)
        {
            return await ProcessTopUpPaymentAsync(orderCode, isSuccess);
        }

        public async Task<bool> ProcessPaymentReturn(IQueryCollection query)
        {
            string responseCode = query["vnp_ResponseCode"];
            string orderCode = query["vnp_TxnRef"];

            bool isSuccess = responseCode == "00";
            return await ProcessTopUpPaymentAsync(orderCode, isSuccess);
        }

        private async Task<bool> ProcessTopUpPaymentAsync(string orderCode, bool isSuccess)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _paymentRepo.GetPaymentWithWalletAsync(orderCode);
                if (payment == null)
                    return false;

                if (payment.Status != PaymentStatus.Pending)
                    return false;

                if (!isSuccess)
                {
                    payment.Status = PaymentStatus.Failed;
                    _paymentRepo.Update(payment);
                    await _unitOfWork.CommitAsync();
                    return false;
                }

                payment.Status = PaymentStatus.Success;
                payment.PaidAt = DateTime.UtcNow;
                _paymentRepo.Update(payment);

                var wallet = payment.Account?.Wallet;
                if (wallet == null)
                    throw new Exception("Ví không tồn tại.");

                decimal balanceBefore = wallet.Balance;
                wallet.Balance += payment.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(wallet);

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = wallet.WalletId,
                    PaymentId = payment.PaymentId,
                    TransactionCode = GenerateTransactionCode("TRX"),
                    Amount = payment.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = TransactionType.Credit,
                    ReferenceType = TransactionReferenceType.TopUp,
                    ReferenceId = payment.PaymentId,
                    Description = $"Nạp tiền qua {payment.Provider} - Order {payment.OrderCode}",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                });

                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    SenderId = payment.AccountId,
                    TargetUserId = payment.AccountId,
                    Title = "Nạp ví thành công",
                    Content = $"Bạn đã nạp thành công {payment.Amount:N0} VND vào ví.",
                    Type = "WalletTopUp"
                });

                await _notificationService.SendWalletUpdatedAsync(payment.AccountId, new
                {
                    WalletId = wallet.WalletId,
                    Balance = wallet.Balance,
                    LockedBalance = wallet.LockedBalance,
                    UpdatedAt = wallet.UpdatedAt
                });

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private async Task<object> CreateZaloOrder(string appTransId, decimal amount, int accountId)
        {
            var appId = "2553";
            var key1 = "PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL";
            var endpoint = "https://sb-openapi.zalopay.vn/v2/create";

            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long amountLong = (long)amount;

            var embedData = "{\"redirecturl\":\"fashionmobile://payment-result\"}";
            var items = "[]";
            var appUser = accountId.ToString();

            var order = new Dictionary<string, string>
            {
                { "app_id", appId },
                { "app_user", appUser },
                { "app_trans_id", appTransId },
                { "app_time", appTime.ToString() },
                { "amount", amountLong.ToString() },
                { "embed_data", embedData },
                { "item", items },
                { "description", $"Nap tien {amountLong} VND" },
                { "callback_url", "https://0992-118-71-8-38.ngrok-free.app/api/payment/callback" }
            };

            string data = $"{appId}|{appTransId}|{appUser}|{amountLong}|{appTime}|{embedData}|{items}";
            order.Add("mac", GenerateMac(data, key1));

            using var client = new HttpClient();
            var response = await client.PostAsync(endpoint, new FormUrlEncodedContent(order));
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || result.TrimStart().StartsWith("<"))
                throw new Exception($"ZaloPay API Failed. StatusCode: {response.StatusCode}. Response: {result}");

            return JsonSerializer.Deserialize<object>(result)!;
        }

        private static string GenerateMac(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        private static string GenerateTransactionCode(string prefix)
        {
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        public class VnPayCompare : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }
    }
}