using Application.Interfaces;
using Application.Services.PostImp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Jobs
{
    /// <summary>
    /// Recompute trending hashtags/topics every 1 hour
    /// </summary>
    [DisallowConcurrentExecution]
    public class RecomputeTrendingTopicJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger<RecomputeTrendingTopicJob> _logger;

        public RecomputeTrendingTopicJob(
            IServiceScopeFactory scopeFactory,
            ILogger<RecomputeTrendingTopicJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(
            IJobExecutionContext context)
        {
            _logger.LogInformation(
                ">>> [TRENDING TOPIC JOB] Start recomputing trending topics at {Time}",
                DateTime.UtcNow);

            using var scope =
                _scopeFactory.CreateScope();

            var trendingTopicService =
                scope.ServiceProvider
                    .GetRequiredService<ITrendingTopicService>();

            try
            {
                await trendingTopicService
                    .RecomputeTrendingTopicsAsync();

                _logger.LogInformation(
                    ">>> [TRENDING TOPIC JOB] Recompute trending topics completed successfully at {Time}",
                    DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    ">>> [TRENDING TOPIC JOB] Failed to recompute trending topics. Message: {Message}",
                    ex.Message);

                throw new JobExecutionException(
                    msg: "Recompute trending topics job failed.",
                    cause: ex,
                    refireImmediately: false);
            }
        }
    }
}