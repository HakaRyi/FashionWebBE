using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Jobs
{
    /// <summary>
    /// Job xử lý tự động chốt điểm, kết thúc sự kiện và trao tiền thưởng (Escrow -> Ví người thắng).
    /// </summary>
    [DisallowConcurrentExecution]
    public class FinalizeEventJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FinalizeEventJob> _logger;

        public FinalizeEventJob(IServiceScopeFactory scopeFactory, ILogger<FinalizeEventJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // 1. Lấy EventId từ dữ liệu truyền vào
            int eventId = context.MergedJobDataMap.GetInt("EventId");

            _logger.LogInformation(">>> [QUARTZ] Bắt đầu tự động KẾT THÚC & TRAO GIẢI Event ID: {EventId} vào lúc {Time}", eventId, DateTime.Now);

            // 2. Tạo Scope mới để lấy DbContext và Services an toàn
            using var scope = _scopeFactory.CreateScope();
            var eventAwardingService = scope.ServiceProvider.GetRequiredService<IEventAwardingService>();

            try
            {
                // 3. Thực thi nghiệp vụ chốt điểm và trao thưởng
                await eventAwardingService.FinalizeAndAwardEventAsync(eventId);

                _logger.LogInformation(">>> [QUARTZ] KẾT THÚC & TRAO GIẢI Event ID: {EventId} HOÀN TẤT thành công.", eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> [QUARTZ] LỖI NGHIÊM TRỌNG khi trao giải Event ID: {EventId}. Nội dung: {Message}", eventId, ex.Message);

                // Ném lỗi ra cho Quartz biết. refireImmediately = false để Admin có thời gian check lỗi DB/Ví 
                // thay vì để Quartz spam chạy lại liên tục gây kẹt DB.
                throw new JobExecutionException(
                    msg: $"Lỗi trao giải Event {eventId}",
                    cause: ex,
                    refireImmediately: false);
            }
        }
    }
}