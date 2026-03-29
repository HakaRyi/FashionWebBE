using Microsoft.AspNetCore.Http;
using Services.Request.PaymentReq;
using Services.Response.PaymentResp;

namespace Services.Implements.PaymentService
{
    public interface IPaymentService
    {
        Task<PaymentResponse?> CreatePackagePaymentAsync(PaymentRequest request);
        Task<PaymentResponse?> CreateTopUpPaymentAsync(decimal amount);
        Task<object> CreateOrderAsync(CreateOrderRequest request);
        Task<object> CreateVnPayOrderAsync(CreateOrderRequest request, string ipAddress);
        Task HandleCallbackAsync(ZaloCallbackRequest request);
        Task<bool> ProcessPaymentCallbackAsync(string orderCode, bool isSuccess);
        Task<bool> ProcessPaymentReturn(IQueryCollection query);
    }
}