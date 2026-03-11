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
        public async Task<IActionResult> Register([FromForm] ExpertRequest request)
        {
            try
            {
                string finalPortfolioUrl = request.PortfolioUrl;

                if (request.EvidenceType?.ToLower() == "pdf" && request.File != null)
                {
                    finalPortfolioUrl = await _fileService.UploadAsync(request.File);
                }

                var dto = new ExpertRegistrationDto
                {
                    AccountId = request.AccountId,
                    Style = request.Style,
                    Bio = request.Bio,
                    EvidenceType = request.EvidenceType,
                    PortfolioUrl = finalPortfolioUrl
                };

                var result = await _expertService.RegisterExpertAsync(dto);
                if (result) return Ok(new { message = "Đăng ký thành công, vui lòng chờ duyệt." });

                return BadRequest();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
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

        [HttpGet("admin/pending")]
        public async Task<IActionResult> GetPending()
        {
            var pending = await _expertService.GetPendingApplicationsAsync();
            return Ok(pending);
        }

        [HttpPost("admin/process")]
        public async Task<IActionResult> ProcessApplication(int fileId, string status, string? reason)
        {
            // Valid status usually: "Approved" or "Rejected"
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
            if (result) return Ok("Xóa chuyên gia thành công.");
            return NotFound();
        }

        #endregion
    }
}