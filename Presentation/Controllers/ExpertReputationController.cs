using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/expert-reputation")]
    public class ExpertReputationController : ControllerBase
    {
        private readonly IReputationHistoryService _reputationService;


        public ExpertReputationController(IReputationHistoryService reputationService)
        {
            _reputationService = reputationService;
        }

        //[Authorize(Roles = "Expert")]
        [HttpGet("my-reputation")]
        public async Task<IActionResult> GetMyReputation()
        {
            try
            {
                var result = await _reputationService.GetReputationDashboardAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
