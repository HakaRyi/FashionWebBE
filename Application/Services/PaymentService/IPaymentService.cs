using Microsoft.AspNetCore.Http;
using Application.Request.PaymentReq;
using Application.Response.PaymentResp;

namespace Application.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<PaymentResponse> CreateTopUpVnPayAsync(CreateTopUpRequest request, string ipAddress);
        Task<PaymentResponse> CreateTopUpZaloPayAsync(CreateTopUpRequest request);

        Task<bool> HandleVnPayReturnAsync(IQueryCollection query);
        Task<bool> ProcessTopUpCallbackAsync(string orderCode, bool isSuccess);

        Task HandleZaloPayCallbackAsync(ZaloCallbackRequest request);
    }
}