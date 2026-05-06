using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/configurations")]
    [ApiController]
    [AllowAnonymous]
    public class ConfigurationController : ControllerBase
    {
        private readonly ISystemSettingService _settingService;

        public ConfigurationController(ISystemSettingService settingService)
        {
            _settingService = settingService;
        }

        /// <summary>
        /// Lấy toàn bộ thông số cấu hình cần thiết để khởi tạo một sự kiện
        /// </summary>
        [HttpGet("event-creation-metadata")]
        public async Task<IActionResult> GetEventMetadata()
        {
            // Gọi service xử lý logic
            var metadata = await _settingService.GetEventCreationMetadataAsync();

            return Ok(metadata);
        }
    }
}