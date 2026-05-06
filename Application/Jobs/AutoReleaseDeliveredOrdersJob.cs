using Application.Interfaces;
using Application.Services.OrderImp;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Jobs
{
    /// <summary>
    /// This job automatically completes delivered orders after the waiting time.
    /// </summary>
    [DisallowConcurrentExecution]
    public class AutoReleaseDeliveredOrdersJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoReleaseDeliveredOrdersJob> _logger;

        private const int AutoCompleteAfterDays = 3;

        public AutoReleaseDeliveredOrdersJob(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoReleaseDeliveredOrdersJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation(
                ">>> [QUARTZ] Start auto completing delivered orders at {Time}",
                DateTime.UtcNow);

            using var scope = _scopeFactory.CreateScope();

            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            try
            {
                var deadline = DateTime.UtcNow.AddDays(-AutoCompleteAfterDays);
                var orders = await orderRepository.GetDeliveredOrdersBeforeAsync(deadline);

                _logger.LogInformation(
                    ">>> [QUARTZ] Found {Count} delivered orders ready to complete.",
                    orders.Count);

                foreach (var order in orders)
                {
                    try
                    {
                        await orderService.AutoCompleteDeliveredOrderAsync(order.OrderId);

                        _logger.LogInformation(
                            ">>> [QUARTZ] Auto completed order ID: {OrderId}",
                            order.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            ">>> [QUARTZ] Failed to auto complete order ID: {OrderId}. Message: {Message}",
                            order.OrderId,
                            ex.Message);
                    }
                }

                _logger.LogInformation(
                    ">>> [QUARTZ] Auto complete delivered orders job completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    ">>> [QUARTZ] Auto complete delivered orders job failed. Message: {Message}",
                    ex.Message);

                throw new JobExecutionException(
                    msg: "Auto complete delivered orders job failed.",
                    cause: ex,
                    refireImmediately: false);
            }
        }
    }
}