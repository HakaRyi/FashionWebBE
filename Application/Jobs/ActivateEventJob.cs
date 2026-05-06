using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Jobs
{
    /// <summary>
    /// Job xử lý kích hoạt sự kiện tự động: Kiểm tra số lượng Expert và thực hiện ký quỹ.
    /// </summary>
    [DisallowConcurrentExecution] // Đảm bảo một EventId không bị chạy chồng chéo nhiều lần
    public class ActivateEventJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ActivateEventJob> _logger;

        public ActivateEventJob(IServiceScopeFactory scopeFactory, ILogger<ActivateEventJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // 1. Lấy EventId từ dữ liệu truyền vào
            int eventId = context.MergedJobDataMap.GetInt("EventId");

            _logger.LogInformation(">>> [QUARTZ] Bắt đầu xử lý kích hoạt Event ID: {EventId} vào lúc {Time}", eventId, DateTime.Now);

            // 2. Tạo Scope để lấy Scoped Services (IEventService, DBContext, UnitOfWork, v.v.)
            using var scope = _scopeFactory.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
            var eventCreationService = scope.ServiceProvider.GetRequiredService<IEventCreationService>();

            try
            {
                // 3. Thực thi nghiệp vụ chính
                // Hàm này bên trong đã có check số lượng Expert Accepted và trừ tiền ví
                await eventCreationService.ActivateEventWithEscrowAsync(eventId);

                _logger.LogInformation(">>> [QUARTZ] Kích hoạt Event ID: {EventId} HOÀN TẤT thành công.", eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> [QUARTZ] LỖI khi chạy Job cho Event ID: {EventId}. Nội dung: {Message}", eventId, ex.Message);

                // Ném lỗi để Quartz biết Job thất bại. 
                // refireImmediately = false: Không chạy lại ngay lập tức để tránh nghẽn hệ thống nếu ví vẫn chưa đủ tiền.
                throw new JobExecutionException(
                    msg: $"Lỗi kích hoạt Event {eventId}",
                    cause: ex,
                    refireImmediately: false);
            }
        }
    }
}