using Microsoft.Extensions.Options;
using Application.Helpers;
using System.Text.Json;

namespace Application.Utils.Gateways
{
    public class ZaloPayGatewayService : IZaloPayGatewayService
    {
        private readonly ZaloPayOptions _options;
        private readonly HttpClient _httpClient;

        public ZaloPayGatewayService(
            IOptions<ZaloPayOptions> options,
            HttpClient httpClient)
        {
            _options = options.Value;
            _httpClient = httpClient;
        }

        public async Task<object> CreateOrderAsync(string appTransId, decimal amount, int accountId)
        {
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long amountLong = (long)amount;
            string embedData = JsonSerializer.Serialize(new
            {
                redirecturl = _options.RedirectUrl
            });
            string items = "[]";

            var order = new Dictionary<string, string>
            {
                { "app_id", _options.AppId },
                { "app_user", accountId.ToString() },
                { "app_trans_id", appTransId },
                { "app_time", appTime.ToString() },
                { "amount", amountLong.ToString() },
                { "embed_data", embedData },
                { "item", items },
                { "description", $"Nap tien {amountLong} VND" },
                { "callback_url", _options.CallbackUrl }
            };

            string data = $"{_options.AppId}|{appTransId}|{accountId}|{amountLong}|{appTime}|{embedData}|{items}";
            order.Add("mac", VnPayHelper.HmacSha256(_options.Key1, data));

            var response = await _httpClient.PostAsync(
                _options.CreateOrderUrl,
                new FormUrlEncodedContent(order));

            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || result.TrimStart().StartsWith("<"))
            {
                throw new Exception($"ZaloPay API Failed. StatusCode: {response.StatusCode}. Response: {result}");
            }

            return JsonSerializer.Deserialize<object>(result)!;
        }
    }
}