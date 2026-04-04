using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Services.Helpers;
using Services.Request.PaymentReq;
using System.Text;

namespace Services.Utils.Gateways
{
    public class VnPayGatewayService : IVnPayGatewayService
    {
        private readonly VnPayOptions _options;

        public VnPayGatewayService(IOptions<VnPayOptions> options)
        {
            _options = options.Value;
        }

        public Task<string> CreatePaymentUrlAsync(CreateOrderRequest request, string orderCode, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new Exception("VnPaySettings:BaseUrl chưa được cấu hình.");

            if (string.IsNullOrWhiteSpace(_options.ReturnUrl))
                throw new Exception("VnPaySettings:ReturnUrl chưa được cấu hình.");

            if (request.Amount <= 0 || request.Amount != decimal.Truncate(request.Amount))
                throw new Exception("Số tiền không hợp lệ.");

            long amountLong = (long)request.Amount;

            if (ipAddress == "::1" ||
                ipAddress.StartsWith("192.168") ||
                ipAddress.StartsWith("127.0"))
            {
                ipAddress = _options.FallbackIpAddress;
            }

            var vnpParams = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Amount", (amountLong * 100).ToString() },
                { "vnp_Command", _options.Command },
                { "vnp_CreateDate", DateTime.UtcNow.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", _options.CurrCode },
                { "vnp_Locale", _options.Locale },
                { "vnp_OrderInfo", $"Nap tien {amountLong} VND" },
                { "vnp_OrderType", _options.OrderType },
                { "vnp_ReturnUrl", _options.ReturnUrl },
                { "vnp_TmnCode", _options.TmnCode },
                { "vnp_TxnRef", orderCode },
                { "vnp_Version", _options.Version },
                { "vnp_IpAddr", ipAddress }
            };

            var hashDataBuilder = new StringBuilder();
            var queryBuilder = new StringBuilder();

            foreach (var kv in vnpParams)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                {
                    hashDataBuilder
                        .Append(System.Net.WebUtility.UrlEncode(kv.Key))
                        .Append("=")
                        .Append(System.Net.WebUtility.UrlEncode(kv.Value))
                        .Append("&");

                    queryBuilder
                        .Append(System.Net.WebUtility.UrlEncode(kv.Key))
                        .Append("=")
                        .Append(System.Net.WebUtility.UrlEncode(kv.Value))
                        .Append("&");
                }
            }

            string hashData = hashDataBuilder.ToString().TrimEnd('&');
            string query = queryBuilder.ToString().TrimEnd('&');
            string secureHash = VnPayHelper.HmacSha512(_options.HashSecret, hashData);

            string paymentUrl = $"{_options.BaseUrl}?{query}&vnp_SecureHash={secureHash}";
            return Task.FromResult(paymentUrl);
        }

        public bool ValidateReturn(IQueryCollection query)
        {
            if (query == null || query.Count == 0)
                return false;

            string vnpSecureHash = query["vnp_SecureHash"].ToString();
            if (string.IsNullOrWhiteSpace(vnpSecureHash))
                return false;

            var vnpParams = new SortedList<string, string>(new VnPayCompare());

            foreach (var item in query)
            {
                string key = item.Key;
                string value = item.Value.ToString();

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    continue;

                if (key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                {
                    vnpParams[key] = value;
                }
            }

            if (vnpParams.Count == 0)
                return false;

            var hashDataBuilder = new StringBuilder();

            foreach (var kv in vnpParams)
            {
                hashDataBuilder
                    .Append(System.Net.WebUtility.UrlEncode(kv.Key))
                    .Append("=")
                    .Append(System.Net.WebUtility.UrlEncode(kv.Value))
                    .Append("&");
            }

            string hashData = hashDataBuilder.ToString().TrimEnd('&');
            string computedHash = VnPayHelper.HmacSha512(_options.HashSecret, hashData);

            return string.Equals(
                computedHash,
                vnpSecureHash,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}