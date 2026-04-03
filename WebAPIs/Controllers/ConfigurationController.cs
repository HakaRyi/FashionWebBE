using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Repos.SystemSettingRepos;

namespace WebAPIs.Controllers
{
    [Route("api/configurations")]
    [ApiController]
    [AllowAnonymous]
    public class ConfigurationController : ControllerBase
    {
        private readonly ISystemSettingRepository _repository;

        public ConfigurationController(ISystemSettingRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Lấy toàn bộ thông số cấu hình cần thiết để khởi tạo một sự kiện
        /// </summary>
        [HttpGet("event-creation-metadata")]
        public async Task<IActionResult> GetEventMetadata()
        {
            var settings = await _repository.GetAllAsync();

            var metadata = new
            {
                ExpertRules = new
                {
                    MinRequired = int.Parse(settings.FirstOrDefault(s => s.SettingKey == "MIN_EXPERTS_PER_EVENT")?.SettingValue ?? "2"),
                },

                FinancialRules = new
                {
                    FeePercentage = double.Parse(settings.FirstOrDefault(s => s.SettingKey == "EVENT_FEE_PERCENTAGE")?.SettingValue ?? "5"),
                    MinFee = double.Parse(settings.FirstOrDefault(s => s.SettingKey == "EVENT_MIN_FEE")?.SettingValue ?? "0"),
                    Currency = "VND"
                },
            };

            return Ok(metadata);
        }
    }
}