using Application.Services.OrderImp;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Jobs
{
    [DisallowConcurrentExecution]
    public class AutoRefundReturnDeliveredOrdersJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoRefundReturnDeliveredOrdersJob> _logger;
        private const int AutoRefundAfterHours = 24;

        public AutoRefundReturnDeliveredOrdersJob(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoRefundReturnDeliveredOrdersJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation(
                ">>> [QUARTZ] Start auto refunding return-delivered orders at {Time}",
                DateTime.UtcNow);

            using var scope = _scopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            try
            {
                var deadline = DateTime.UtcNow.AddHours(-AutoRefundAfterHours);
                var orders = await orderRepository.GetReturnDeliveredOrdersBeforeAsync(deadline);

                _logger.LogInformation(
                    ">>> [QUARTZ] Found {Count} return-delivered orders ready to auto refund.",
                    orders.Count);

                foreach (var order in orders)
                {
                    try
                    {
                        await orderService.AutoRefundReturnDeliveredAsync(order.OrderId);

                        _logger.LogInformation(
                            ">>> [QUARTZ] Auto refunded order ID: {OrderId}",
                            order.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            ">>> [QUARTZ] Failed to auto refund order ID: {OrderId}. Message: {Message}",
                            order.OrderId,
                            ex.Message);
                    }
                }

                _logger.LogInformation(
                    ">>> [QUARTZ] Auto refund return-delivered orders job completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    ">>> [QUARTZ] Auto refund return-delivered orders job failed. Message: {Message}",
                    ex.Message);

                throw new JobExecutionException(
                    msg: "Auto refund return-delivered orders job failed.",
                    cause: ex,
                    refireImmediately: false);
            }
        }
    }
}