using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Repos.SystemSettingRepos;
using Services.Implements.SystemSettingImp;

namespace WebAPIs.Controllers
{
    [Route("api/system-settings")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingService _settingService;
        private readonly ISystemSettingRepository _repository;

        public SystemSettingsController(ISystemSettingService settingService, ISystemSettingRepository repository)
        {
            _settingService = settingService;
            _repository = repository;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách cấu hình hệ thống
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllSettings()
        {
            var settings = await _repository.GetAllAsync();
            return Ok(settings);
        }

        /// <summary>
        /// Lấy một cấu hình cụ thể theo Key
        /// </summary>
        [HttpGet("{key}")]
        public async Task<IActionResult> GetSetting(string key)
        {
            var setting = await _repository.GetByKeyAsync(key);
            if (setting == null) return NotFound($"Không tìm thấy cấu hình với key: {key}");
            return Ok(setting);
        }

        /// <summary>
        /// Cập nhật giá trị cho một cấu hình (Dùng cho mọi loại key)
        /// </summary>
        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] string newValue)
        {
            if (string.IsNullOrEmpty(newValue)) return BadRequest("Giá trị không được để trống");

            var setting = await _repository.GetByKeyAsync(key);
            if (setting == null) return NotFound("Cấu hình không tồn tại");

            await _settingService.UpdateSettingAsync(key, newValue);
            return Ok(new { Message = $"Cập nhật {key} thành công", NewValue = newValue });
        }

        /// <summary>
        /// xem mức phí hiện tại trước khi tạo sự kiện
        /// </summary>
        [HttpGet("public/event-fees")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeesForExpert()
        {
            var config = await _settingService.GetEventFeeConfigAsync();

            return Ok(new
            {
                FeePercentage = config.Percentage,
                MinFee = config.MinFee,
            });
        }
    }
}
