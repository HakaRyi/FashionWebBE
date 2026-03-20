using Microsoft.AspNetCore.Http;
using Services.Request.PaymentReq;
using Services.Response.PaymentResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.PaymentService
{
    public interface IPaymentService
    {
        Task<PaymentResponse?> CreatePackagePaymentAsync(PaymentRequest request);

        Task<PaymentResponse?> CreateTopUpPaymentAsync(decimal amount);

        Task<bool> ProcessPaymentCallbackAsync(string orderCode, bool isSuccess);

        Task<object> CreateOrderAsync(CreateOrderRequest request);
        Task HandleCallbackAsync(ZaloCallbackRequest request);

        Task<object> CreateVnPayOrderAsync(CreateOrderRequest request, string ipAddress);

        Task<bool> ProcessPaymentReturn(IQueryCollection vnpayData);
    }
}
