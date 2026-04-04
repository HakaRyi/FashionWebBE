using Microsoft.AspNetCore.Http;
using Application.Request.PaymentReq;

namespace Application.Utils.Gateways
{
    public interface IVnPayGatewayService
    {
        Task<string> CreatePaymentUrlAsync(CreateOrderRequest request, string orderCode, string ipAddress);
        bool ValidateReturn(IQueryCollection query);
    }
}