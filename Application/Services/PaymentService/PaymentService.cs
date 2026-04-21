using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Domain.Constants;
using Domain.Entities;
using Application.Helpers;
using Application.Request.PaymentReq;
using Application.Response.PaymentResp;
using Application.Utils.Gateways;
using System.Text.Json;
using Domain.Interfaces;

namespace Application.Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private const decimal MinTopUpAmount = 10000m;

        private readonly IPaymentRepository _paymentRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IVnPayGatewayService _vnPayGatewayService;
        private readonly IZaloPayGatewayService _zaloPayGatewayService;
        private readonly ITopUpPaymentProcessor _topUpPaymentProcessor;

        public PaymentService(
            IPaymentRepository paymentRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IVnPayGatewayService vnPayGatewayService,
            IZaloPayGatewayService zaloPayGatewayService,
            ITopUpPaymentProcessor topUpPaymentProcessor)
        {
            _paymentRepo = paymentRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _vnPayGatewayService = vnPayGatewayService;
            _zaloPayGatewayService = zaloPayGatewayService;
            _topUpPaymentProcessor = topUpPaymentProcessor;
        }

        public async Task<PaymentResponse> CreateTopUpVnPayAsync(CreateTopUpRequest request, string ipAddress)
        {
            if (request == null)
                throw new Exception("Invalid top-up data.");

            ValidateTopUpAmount(request.Amount);

            int accountId = _currentUserService.GetRequiredUserId();
            string orderCode = PaymentCodeGenerator.GenerateTopUpOrderCode();

            if (request.Source?.ToUpper() == "WEB")
            {
                orderCode = "W_" + orderCode;
            }

            var payment = await CreatePendingPaymentAsync(
                accountId: accountId,
                amount: request.Amount,
                provider: PaymentProvider.VnPay,
                orderCode: orderCode);

            var gatewayRequest = new CreateOrderRequest
            {
                AccountId = accountId,
                Amount = request.Amount
            };

            string paymentUrl = await _vnPayGatewayService.CreatePaymentUrlAsync(
                gatewayRequest,
                orderCode,
                ipAddress);

            return new PaymentResponse
            {
                PaymentId = payment.PaymentId,
                OrderCode = payment.OrderCode!,
                Amount = payment.Amount,
                Provider = payment.Provider!,
                Status = payment.Status!,
                Description = "Top up your wallet via VNPAY",
                CreatedAt = payment.CreatedAt ?? DateTime.UtcNow,
                PaymentUrl = paymentUrl
            };
        }

        public async Task<PaymentResponse> CreateTopUpZaloPayAsync(CreateTopUpRequest request)
        {
            if (request == null)
                throw new Exception("Invalid top-up data.");

            ValidateTopUpAmount(request.Amount);

            int accountId = _currentUserService.GetRequiredUserId();
            string orderCode = PaymentCodeGenerator.GenerateTopUpOrderCode();

            var payment = await CreatePendingPaymentAsync(
                accountId: accountId,
                amount: request.Amount,
                provider: PaymentProvider.ZaloPay,
                orderCode: orderCode);

            await _zaloPayGatewayService.CreateOrderAsync(
                orderCode,
                request.Amount,
                accountId);

            return new PaymentResponse
            {
                PaymentId = payment.PaymentId,
                OrderCode = payment.OrderCode!,
                Amount = payment.Amount,
                Provider = payment.Provider!,
                Status = payment.Status!,
                Description = "Top up your wallet via ZaloPay",
                CreatedAt = payment.CreatedAt ?? DateTime.UtcNow,
                PaymentUrl = null
            };
        }

        public async Task<bool> HandleVnPayReturnAsync(IQueryCollection query)
        {
            if (query == null || query.Count == 0)
                return false;

            bool isValidSignature = _vnPayGatewayService.ValidateReturn(query);
            if (!isValidSignature)
                return false;

            string orderCode = query["vnp_TxnRef"];
            string responseCode = query["vnp_ResponseCode"];

            if (string.IsNullOrWhiteSpace(orderCode))
                return false;

            bool isSuccess = responseCode == "00";

            return await _topUpPaymentProcessor.ProcessAsync(orderCode, isSuccess);
        }

        public async Task<bool> ProcessTopUpCallbackAsync(string orderCode, bool isSuccess)
        {
            return await _topUpPaymentProcessor.ProcessAsync(orderCode, isSuccess);
        }

        public async Task HandleZaloPayCallbackAsync(ZaloCallbackRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.data))
                throw new Exception("Callback ZaloPay không hợp lệ.");

            using var jsonData = JsonDocument.Parse(request.data);
            var root = jsonData.RootElement;

            string orderCode = root.GetProperty("app_trans_id").GetString() ?? string.Empty;
            int status = root.GetProperty("status").GetInt32();

            bool isSuccess = status == 1;

            await _topUpPaymentProcessor.ProcessAsync(orderCode, isSuccess);
        }

        private async Task<Payment> CreatePendingPaymentAsync(
            int accountId,
            decimal amount,
            string provider,
            string orderCode)
        {
            if (!PaymentProvider.IsValid(provider))
                throw new Exception("Nhà cung cấp thanh toán không hợp lệ.");

            var payment = new Payment
            {
                AccountId = accountId,
                Amount = amount,
                Provider = provider,
                OrderCode = orderCode,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return payment;
        }

        private static void ValidateTopUpAmount(decimal amount)
        {
            if (amount < MinTopUpAmount)
                throw new Exception("The minimum amount is 10,000 VND.");
        }
    }
}