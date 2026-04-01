using Microsoft.AspNetCore.Http;
using Repositories.Constants;
using Repositories.Entities;
using Repositories.Repos.PaymentsRespo;
using Repositories.UnitOfWork;
using Services.Helpers;
using Services.Implements.Auth;
using Services.Request.PaymentReq;
using Services.Response.PaymentResp;
using Services.Utils.Gateways;
using System.Text.Json;

namespace Services.Implements.PaymentService
{
    public class PaymentService : IPaymentService
    {
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
            string orderCode = PaymentCodeGenerator.GenerateTopUpOrderCode();

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

        public async Task<object> CreateOrderAsync(CreateOrderRequest request)
        {
            string appTransId = PaymentCodeGenerator.GenerateVnPayOrderCode();

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

            return await _zaloPayGatewayService.CreateOrderAsync(appTransId, request.Amount, request.AccountId);
        }

        public async Task<object> CreateVnPayOrderAsync(CreateOrderRequest request, string ipAddress)
        {
            string orderCode = PaymentCodeGenerator.GenerateVnPayOrderCode();

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

            string paymentUrl = await _vnPayGatewayService.CreatePaymentUrlAsync(request, orderCode, ipAddress);
            return new { order_url = paymentUrl };
        }

        public async Task HandleCallbackAsync(ZaloCallbackRequest request)
        {
            using var jsonData = JsonDocument.Parse(request.data);
            var root = jsonData.RootElement;

            string orderCode = root.GetProperty("app_trans_id").GetString()!;
            int status = root.GetProperty("status").GetInt32();

            bool isSuccess = status == 1;
            await _topUpPaymentProcessor.ProcessAsync(orderCode, isSuccess);
        }

        public async Task<bool> ProcessPaymentCallbackAsync(string orderCode, bool isSuccess)
        {
            return await _topUpPaymentProcessor.ProcessAsync(orderCode, isSuccess);
        }

        public async Task<bool> ProcessPaymentReturn(IQueryCollection query)
        {
            string responseCode = query["vnp_ResponseCode"];
            string orderCode = query["vnp_TxnRef"];
            bool isSuccess = responseCode == "00";

            return await _topUpPaymentProcessor.ProcessAsync(orderCode, isSuccess);
        }
    }
}