using Services.Request.AdminReq;
using Services.Request.NotificationReq;
using Services.Response.AccountRep;
using Services.Response.AdminResp;
using Services.Response.TransactionResp;

namespace Services.Implements.AdminImp
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
