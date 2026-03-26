using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.Experts;
using Services.Request.ExpertReq;
using Services.Response.ExpertResp;
using Services.Utils.File;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/expert")]
    public class ExpertController : ControllerBase
    {
        private readonly IExpertService _expertService;
        private readonly IFileService _fileService;

        public ExpertController(IExpertService expertService, IFileService fileService)
        {
            _expertService = expertService;
            _fileService = fileService;
        }

        #region Expert Logic (Registration)

        [HttpPost("register")]
        //[Authorize]
        public async Task<IActionResult> Register([FromForm] ExpertRequest request)
        {
            try
            {
                string finalUrl = request.PortfolioUrl;

                if (request.EvidenceType?.ToLower() == "pdf" && request.File != null)
                {
                    finalUrl = await _fileService.UploadAsync(request.File);
                }

                var dto = new ExpertRegistrationDto
                {
                    Style = request.Style,
                    StyleAesthetic = request.StyleAesthetic,
                    YearsOfExperience = request.YearsOfExperience,
                    Bio = request.Bio,
                    EvidenceType = request.EvidenceType,
                    PortfolioUrl = finalUrl
                };

                var result = await _expertService.RegisterExpertAsync(dto);

                if (result)
                    return Ok(new { message = "Hồ sơ chuyên gia đã được gửi và đang chờ phê duyệt." });

                return BadRequest("Không thể lưu hồ sơ.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("profile/{accountId}")]
        public async Task<IActionResult> GetProfile(int accountId)
        {
            var profile = await _expertService.GetProfileByAccountId(accountId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        #endregion

        #region Admin Logic (Moderation)
        [HttpGet("/api/experts")]
        public async Task<IActionResult> GetAllExperts()
        {
            var experts = await _expertService.GetAllExpertsAsync();
            return Ok(experts);
        }

        [HttpGet("admin/pending")]
        public async Task<IActionResult> GetPending()
        {
            var pending = await _expertService.GetPendingApplicationsAsync();
            return Ok(pending);
        }

        [HttpPost("admin/process")]
        public async Task<IActionResult> ProcessApplication(int fileId, string status, string? reason)
        {
            var result = await _expertService.ProcessApplicationAsync(fileId, status, reason);
            if (result) return Ok(new { message = $"Đã cập nhật trạng thái: {status}" });

            return BadRequest();
        }

        #endregion

        #region General Retrieval

        [HttpGet("verified")]
        public async Task<IActionResult> GetVerifiedExperts()
        {
            var list = await _expertService.GetAllVerifiedExpertsAsync();
            return Ok(list);
        }

        [HttpDelete("{profileId}")]
        public async Task<IActionResult> DeleteExpert(int profileId)
        {
            var result = await _expertService.DeleteExpertProfileAsync(profileId);
            if (result) return Ok();
            return NotFound();
        }

        [HttpGet("active-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveExperts()
        {
            try
            {
                var experts = await _expertService.GetActiveExpertsForUserAsync();
                return Ok(experts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        /// <summary>
        /// Xem chi tiết hồ sơ công khai của một chuyên gia
        /// </summary>
        [HttpGet("details/{profileId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetExpertPublicDetails(int profileId)
        {
            try
            {
                var profile = await _expertService.GetExpertPublicProfileAsync(profileId);

                if (profile == null)
                {
                    return NotFound(new { message = "Không tìm thấy chuyên gia hoặc tài khoản này đã bị khóa." });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
        #endregion
    }
}