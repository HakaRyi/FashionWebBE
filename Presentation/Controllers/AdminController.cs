using Application.Request.AdminReq;
using Application.Services.AdminImp;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Route("api/admin")]
[ApiController]
//[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    private readonly IAdminSocialDashboardService
        _adminDashboardService;

    public AdminController(
        IDashboardService dashboardService,
        IAdminSocialDashboardService adminDashboardService)
    {
        _dashboardService = dashboardService;

        _adminDashboardService = adminDashboardService;
    }

    [HttpGet("dashboard-information")]
    public async Task<IActionResult> GetAdminDashboard(
        [FromQuery] DashboardRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _dashboardService
            .GetDashboardInformation(request);

        return Ok(data);
    }

    [HttpGet("social-dashboard/users")]
    public async Task<IActionResult> GetUserDashboard(
        CancellationToken cancellationToken)
    {
        var result = await _adminDashboardService
            .GetUserDashboardAsync();

        return Ok(result);
    }

    [HttpGet("social-dashboard/posts")]
    public async Task<IActionResult> GetPostDashboard(
    CancellationToken cancellationToken)
    {
        var result = await _adminDashboardService
            .GetPostDashboardAsync();

        return Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] DashboardRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _dashboardService
            .GetTransactionList(request);

        return Ok(data);
    }

    [HttpGet("recent-users")]
    public async Task<IActionResult> GetNewestUsers(
        CancellationToken cancellationToken)
    {
        var data = await _dashboardService
            .Get3NewestUser();

        return Ok(data);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService
            .GetAdminNotifications(pageIndex, pageSize);

        return Ok(result);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService
            .GetEvents(pageIndex, pageSize);

        return Ok(result);
    }

    [HttpPut("users/{accountId}/ban")]
    public async Task<IActionResult> BanUser(
        [FromRoute] int accountId,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService
            .AdminBanUser(accountId);

        return Ok(new
        {
            Result = result
        });
    }

    [HttpPut("users/{accountId}/unban")]
    public async Task<IActionResult> UnbanUser(
        [FromRoute] int accountId,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService
            .AdminUnBanUser(accountId);

        return Ok(new
        {
            Result = result
        });
    }

    [HttpPut("events/{eventId}/check")]
    public async Task<IActionResult> CheckEvent(
        [FromRoute] int eventId,
        [FromBody] AdminCheckRequest request,
        CancellationToken cancellationToken)
    {
        await _dashboardService
            .AdminCheckEvent(eventId, request);

        return Ok(new
        {
            Message = "Admin checked event successfully."
        });
    }
}
