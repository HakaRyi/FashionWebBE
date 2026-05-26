using Application.Interfaces;
using Application.Services.PostImp;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Jobs
{
    /// <summary>
    /// Recompute trending posts every 1 hour
    /// </summary>
    [DisallowConcurrentExecution]
    public class RecomputePostTrendJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecomputePostTrendJob> _logger;

        public RecomputePostTrendJob(
            IServiceScopeFactory scopeFactory,
            ILogger<RecomputePostTrendJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation(
                ">>> [TREND JOB] Start recomputing post trends at {Time}",
                DateTime.UtcNow);

            using var scope = _scopeFactory.CreateScope();

            var postTrendService = scope.ServiceProvider.GetRequiredService<IPostTrendService>();

            try
            {
                await postTrendService.RecomputeTrendingAsync();

                _logger.LogInformation(
                    ">>> [TREND JOB] Recompute post trends completed successfully at {Time}",
                    DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    ">>> [TREND JOB] Failed to recompute post trends. Message: {Message}",
                    ex.Message);

                throw new JobExecutionException(
                    msg: "Recompute post trends job failed.",
                    cause: ex,
                    refireImmediately: false);
            }
        }
    }
}