using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Repositories.Entities;
using Repositories.Repos.AccountSubscriptionRepos;
using Repositories.Repos.NotificationRepos;
using Repositories.Repos.Payments;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Implements.NotificationImp;
using Services.Implements.WalletImp;
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
        private readonly IAccountSubscriptionRepository _subscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly IWalletService _walletService;
        private readonly IWalletRepository _walletRepo;

        public PaymentService(
            IPaymentRepository paymentRepo,
            ITransactionRepository transactionRepo,
            IAccountSubscriptionRepository subscriptionRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            IWalletService walletService,
            IWalletRepository walletRepository
            )
        {
            _paymentRepo = paymentRepo;
            _transactionRepo = transactionRepo;
            _subscriptionRepo = subscriptionRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _walletService = walletService;
            _walletRepo = walletRepository;
        }

        public async Task<object> CreateOrderAsync(CreateOrderRequest request)
        {
            var appTransId = DateTime.Now.ToString("yyMMdd") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);

            var payment = new Payment
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                Provider = "VNPAY",
                OrderCode = appTransId,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.CreatePaymentAsync(payment);

            var zaloResult = await CreateZaloOrder(appTransId, request.Amount, request.AccountId);

            return zaloResult;
        }

        public async Task HandleCallbackAsync(ZaloCallbackRequest request)
        {
            var jsonData = JsonDocument.Parse(request.data);
            var root = jsonData.RootElement;

            string appTransId = root.GetProperty("app_trans_id").GetString()!;
            int status = root.GetProperty("status").GetInt32();

            var payment = await _paymentRepo.GetByOrderCodeAsync(appTransId);
            if (payment == null) return;

            if (status == 1)
            {
                payment.Status = "SUCCESS";
                payment.PaidAt = DateTime.UtcNow;
            }
            else
            {
                payment.Status = "FAILED";
            }

            await _paymentRepo.UpdatePaymentAsync(payment);
        }

        private async Task<object> CreateZaloOrder(string appTransId, decimal amount, int accountId)
        {
            var appId = "2553";
            var key1 = "PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL";
            var endpoint = "https://sb-openapi.zalopay.vn/v2/create";

            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long amountLong = (long)amount;

            var embedData = "{\"redirecturl\": \"fashionmobile://payment-result\"}";
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
                { "callback_url", "https://sliding-rudderless-consuelo.ngrok-free.dev/api/payment/callback" }
            };

            string data = $"{appId}|{appTransId}|{appUser}|{amountLong}|{appTime}|{embedData}|{items}";
            order.Add("mac", GenerateMac(data, key1));

            using var client = new HttpClient();
            var response = await client.PostAsync(endpoint, new FormUrlEncodedContent(order));
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || result.TrimStart().StartsWith("<"))
            {
                throw new Exception($"ZaloPay API Failed. StatusCode: {response.StatusCode}. Response: {result}");
            }

            return JsonSerializer.Deserialize<object>(result)!;
        }

        private string GenerateMac(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public async Task<PaymentResponse?> CreatePackagePaymentAsync(PaymentRequest request)
        {
            int accountId = _currentUserService.GetRequiredUserId();

            var package = (await _paymentRepo.GetActivePackagesAsync())
                .FirstOrDefault(p => p.PackageId == request.PackageId);

            if (package == null)
                throw new Exception("Gói dịch vụ không khả dụng.");

            var orderCode = $"PKG-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var payment = new Payment
            {
                AccountId = accountId,
                PackageId = request.PackageId,
                Amount = package.Price,
                Provider = "VnPay",
                OrderCode = orderCode,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return new PaymentResponse
            {
                OrderCode = orderCode,
                Amount = payment.Amount,
                Description = $"Mua gói: {package.Name}",
                Status = payment.Status
            };
        }

        public async Task<PaymentResponse?> CreateTopUpPaymentAsync(decimal amount)
        {
            if (amount < 10000) throw new Exception("Số tiền tối thiểu là 10,000đ.");

            int accountId = _currentUserService.GetRequiredUserId();
            var orderCode = $"TOP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var payment = new Payment
            {
                AccountId = accountId,
                PackageId = null,
                Amount = amount,
                Provider = "VnPay",
                OrderCode = orderCode,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return new PaymentResponse
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = "Nạp tiền vào ví",
                Status = payment.Status
            };
        }

        public async Task<bool> ProcessPaymentCallbackAsync(string orderCode, bool isSuccess)
        {
            using var transactionScope = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _paymentRepo.GetPaymentWithWalletAsync(orderCode);
                if (payment == null || payment.Status != "Pending") return false;

                if (isSuccess)
                {
                    payment.Status = "Success";
                    payment.PaidAt = DateTime.UtcNow;

                    var wallet = payment.Account?.Wallet;
                    if (wallet == null) throw new Exception("Ví không tồn tại.");

                    decimal balanceBefore = wallet.Balance;
                    wallet.Balance += payment.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;

                    // 1. Tạo Transaction Log
                    var transactionEntry = new Transaction
                    {
                        WalletId = wallet.WalletId,
                        PaymentId = payment.PaymentId,
                        Amount = payment.Amount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = wallet.Balance,
                        Type = "Deposit",
                        ReferenceType = payment.PackageId.HasValue ? "Subscription" : "TopUp",
                        ReferenceId = payment.PaymentId,
                        Description = payment.PackageId.HasValue
                            ? $"Thanh toán gói {payment.Package?.Name}"
                            : "Nạp tiền vào ví qua VnPay",
                        CreatedAt = DateTime.UtcNow,
                        Status = "Success"
                    };
                    await _transactionRepo.AddAsync(transactionEntry);

                    // 2. Logic Gia hạn/Kích hoạt Gói hội viên
                    if (payment.PackageId.HasValue && payment.Package != null)
                    {
                        // Kiểm tra xem khách có đang trong thời hạn gói nào không
                        var latestSub = await _subscriptionRepo.GetLatestActiveSubscriptionAsync(payment.AccountId);

                        // Nếu còn hạn thì bắt đầu từ lúc hết hạn cũ, nếu không thì bắt đầu từ bây giờ
                        DateTime startDate = (latestSub != null && latestSub.EndDate > DateTime.UtcNow)
                                            ? latestSub.EndDate
                                            : DateTime.UtcNow;

                        var newSub = new AccountSubscription
                        {
                            AccountId = payment.AccountId,
                            PackageId = payment.PackageId.Value,
                            StartDate = startDate,
                            EndDate = startDate.AddDays(payment.Package.DurationDays),
                            IsActive = true
                        };
                        await _subscriptionRepo.AddAsync(newSub);
                    }
                }
                else
                {
                    payment.Status = "Failed";
                }

                await _unitOfWork.SaveChangesAsync();
                await transactionScope.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transactionScope.RollbackAsync();
                return false;
            }
        }

        public async Task<object> CreateVnPayOrderAsync(CreateOrderRequest request, string ipAddress)
        {
            var vnp_Returnurl = "https://sliding-rudderless-consuelo.ngrok-free.dev/api/Payment/vnpay-return";
            var vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var vnp_TmnCode = "1K7OF3A1".Trim();
            var vnp_HashSecret = "9G9LFM1HFI9KFAM2LGS7WQY4K2EXM51Z".Trim();

            var appTransId = DateTime.Now.ToString("yyMMdd") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            long amountLong = (long)request.Amount;

            var payment = new Payment
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                Provider = "VNPAY",
                OrderCode = appTransId,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.CreatePaymentAsync(payment);

            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            vnp_Params.Add("vnp_Amount", (amountLong * 100).ToString());
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnp_Params.Add("vnp_CurrCode", "VND");

            if (ipAddress == "::1" || ipAddress.StartsWith("192.168") || ipAddress.StartsWith("127.0"))
            {
                ipAddress = "113.190.238.169";
            }
            vnp_Params.Add("vnp_IpAddr", ipAddress);
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_OrderInfo", $"NapTienWapoPay{amountLong}");
            vnp_Params.Add("vnp_OrderType", "other");
            vnp_Params.Add("vnp_ReturnUrl", vnp_Returnurl);
            vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
            vnp_Params.Add("vnp_TxnRef", appTransId);
            vnp_Params.Add("vnp_Version", "2.1.0");

            var hashDataBuilder = new StringBuilder();
            var queryBuilder = new StringBuilder();

            foreach (var kv in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    hashDataBuilder.Append(System.Net.WebUtility.UrlEncode(kv.Key) + "=" + System.Net.WebUtility.UrlEncode(kv.Value) + "&");
                    queryBuilder.Append(System.Net.WebUtility.UrlEncode(kv.Key) + "=" + System.Net.WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string hashData = hashDataBuilder.ToString().Remove(hashDataBuilder.Length - 1, 1);
            string query = queryBuilder.ToString().Remove(queryBuilder.Length - 1, 1);

            string vnp_SecureHash = HmacSha512(vnp_HashSecret, hashData);
            string paymentUrl = vnp_Url + "?" + query + "&vnp_SecureHash=" + vnp_SecureHash;

            return new { order_url = paymentUrl };
        }

        public async Task<bool> ProcessPaymentReturn(IQueryCollection query)
        {
            string vnp_ResponseCode = query["vnp_ResponseCode"];
            string vnp_TxnRef = query["vnp_TxnRef"];

            var payment = await _paymentRepo.GetByOrderCodeAsync(vnp_TxnRef);
            if (payment == null)
            {
                return false;
            }

            if (vnp_ResponseCode == "00")
            {
                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    payment.Status = "SUCCESS";
                    payment.PaidAt = DateTime.UtcNow;
                    await _paymentRepo.UpdatePaymentAsync(payment);

                    var wallet = await _walletRepo.GetByAccountIdAsync(payment.AccountId);
                    if (wallet == null) throw new Exception("Ví không tồn tại.");

                    decimal balanceBefore = wallet.Balance;
                    wallet.Balance += payment.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _walletRepo.Update(wallet);

                    var trans = new Transaction
                    {
                        WalletId = wallet.WalletId,
                        PaymentId = payment.PaymentId,
                        Amount = payment.Amount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = wallet.Balance,
                        Type = "Credit",
                        ReferenceType = "TopUp",
                        ReferenceId = payment.PaymentId,
                        Description = "Nạp tiền qua VNPAY",
                        Status = "Success",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _transactionRepo.AddAsync(trans);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

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
                        UpdatedAt = wallet.UpdatedAt
                    });

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            else
            {
                payment.Status = "FAILED";
                await _paymentRepo.UpdatePaymentAsync(payment);
                return false;
            }
        }

        private string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        public class VnPayCompare : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                var vnpCompare = string.Compare(x, y, StringComparison.Ordinal);
                if (vnpCompare == 0) return string.Compare(x, y, StringComparison.Ordinal);
                return vnpCompare;
            }
        }
    }
    
}