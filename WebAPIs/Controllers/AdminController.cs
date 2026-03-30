using Microsoft.AspNetCore.Mvc;
using Services.Implements.AdminImp;
using Services.Request.AdminReq;

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        public AdminController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("dashboard-information")]
        public async Task<IActionResult> GetAdminDashboard([FromQuery] DashboardRequest request)
        {
            var data = await _dashboardService.GetDashboardInformation(request);
            return Ok(data);
        }
        [HttpGet("get-transaction-list")]
        public async Task<IActionResult> GetTranssactionsList([FromQuery] DashboardRequest request)
        {
            var data = await _dashboardService.GetTransactionList(request);
            return Ok(data);
        }
        [HttpGet("new-created-recently")]
        public async Task<IActionResult> Get3NewestUser()
        {
            var data = await _dashboardService.Get3NewestUser();
            return Ok(data);
        }
        [HttpGet("admin-notifications")]
        public async Task<IActionResult> GetNotifications([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 5)
        {
            var result = await _dashboardService.GetAdminNotifications(pageIndex, pageSize);
            return Ok(result);
        }
        [HttpGet("list-events")]
        public async Task<IActionResult> GetEvents([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 5)
        {
            var result = await _dashboardService.GetEvents(pageIndex, pageSize);
            return Ok(result);
        }
        [HttpPut("check-event/{eventId}")]
        public async Task<IActionResult> CheckEvent([FromRoute] int eventId, [FromBody] AdminCheckRequest request)
        {
            await _dashboardService.AdminCheckEvent(eventId, request);
            return Ok(new
            {
                message = "Admin da check"
            });
        }
    }
}
