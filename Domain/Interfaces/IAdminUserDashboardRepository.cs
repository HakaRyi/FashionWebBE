using Domain.Contracts.Admin;

namespace Domain.Interfaces;

public interface IAdminUserDashboardRepository
{
    Task<AdminUserDashboardDto> GetUserDashboardAsync();
}
