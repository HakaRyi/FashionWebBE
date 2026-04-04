using Microsoft.AspNetCore.Http;
using Services.Request.PaymentReq;

namespace Services.Utils.Gateways
{
    public interface IVnPayGatewayService
    {
        Task<string> CreatePaymentUrlAsync(CreateOrderRequest request, string orderCode, string ipAddress);
        bool ValidateReturn(IQueryCollection query);
    }
}