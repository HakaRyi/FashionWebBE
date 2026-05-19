using Application.Interfaces;
using Application.Request.AdminReq;
using Application.Services.AdminImp;
using Microsoft.AspNetCore.Authorization;
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

    #region OLD DASHBOARD

    [HttpGet("dashboard-information")]
    public async Task<IActionResult> GetAdminDashboard(
        [FromQuery] DashboardRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _dashboardService
            .GetDashboardInformation(request);

        return Ok(data);
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

    #endregion

    #region SOCIAL DASHBOARD

    [HttpGet("dashboard/overview")]
    public async Task<IActionResult> GetOverview(
        CancellationToken cancellationToken)
    {
        var result = await _adminDashboardService
            .GetOverviewAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("dashboard/charts")]
    public async Task<IActionResult> GetCharts(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var fromDate =
            startDate?.Date ??
            DateTime.UtcNow.AddDays(-30);

        var toDate =
            endDate?.Date ??
            DateTime.UtcNow;

        var result = await _adminDashboardService
            .GetChartsAsync(
                fromDate,
                toDate,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("dashboard/analytics")]
    public async Task<IActionResult> GetAnalytics(
        CancellationToken cancellationToken)
    {
        var result = await _adminDashboardService
            .GetAnalyticsAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("dashboard/recent")]
    public async Task<IActionResult> GetRecent(
        CancellationToken cancellationToken)
    {
        var result = await _adminDashboardService
            .GetRecentAsync(cancellationToken);

        return Ok(result);
    }

    #endregion

    #region USER MANAGEMENT

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

    #endregion

    #region EVENT MANAGEMENT

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

    #endregion
}