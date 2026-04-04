using Application.Request.AdminReq;
using Application.Request.NotificationReq;
using Application.Response.AccountRep;
using Application.Response.AdminResp;
using Application.Response.TransactionResp;

namespace Application.Services.AdminImp
{
    public interface IDashboardService
    {
        Task<DashboardViewDto> GetDashboardInformation(DashboardRequest request);
        Task<List<AccountResponse>> Get3NewestUser();
        Task<List<TransactionResponse>> GetTransactionList(DashboardRequest request);
        Task<PagedNotificationResponse> GetAdminNotifications(int pageIndex, int pageSize);
        Task<PagedAdminEventResponse> GetEvents(int pageIndex, int pageSize);
        Task AdminCheckEvent(int eventId, AdminCheckRequest request);

    }
}
