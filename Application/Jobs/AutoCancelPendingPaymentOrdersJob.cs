using Application.Services.OrderImp;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Jobs
{
    /// <summary>
    /// This job automatically cancels orders that stay in pending payment too long.
    /// </summary>
    [DisallowConcurrentExecution]
    public class AutoCancelPendingPaymentOrdersJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoCancelPendingPaymentOrdersJob> _logger;

        private const int AutoCancelAfterMinutes = 30;

        public AutoCancelPendingPaymentOrdersJob(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoCancelPendingPaymentOrdersJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation(
                ">>> [QUARTZ] Start auto cancelling pending payment orders at {Time}",
                DateTime.UtcNow);

            using var scope = _scopeFactory.CreateScope();

            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            try
            {
                var deadline = DateTime.UtcNow.AddMinutes(-AutoCancelAfterMinutes);
                var orders = await orderRepository.GetPendingPaymentOrdersBeforeAsync(deadline);

                _logger.LogInformation(
                    ">>> [QUARTZ] Found {Count} pending payment orders ready to cancel.",
                    orders.Count);

                foreach (var order in orders)
                {
                    try
                    {
                        await orderService.AutoCancelPendingPaymentOrderAsync(order.OrderId);

                        _logger.LogInformation(
                            ">>> [QUARTZ] Auto cancelled pending payment order ID: {OrderId}",
                            order.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            ">>> [QUARTZ] Failed to auto cancel pending payment order ID: {OrderId}. Message: {Message}",
                            order.OrderId,
                            ex.Message);
                    }
                }

                _logger.LogInformation(
                    ">>> [QUARTZ] Auto cancel pending payment orders job completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    ">>> [QUARTZ] Auto cancel pending payment orders job failed. Message: {Message}",
                    ex.Message);

                throw new JobExecutionException(
                    msg: "Auto cancel pending payment orders job failed.",
                    cause: ex,
                    refireImmediately: false);
            }
        }
    }
}