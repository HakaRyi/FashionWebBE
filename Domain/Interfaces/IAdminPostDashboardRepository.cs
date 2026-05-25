using Domain.Contracts.Admin;

namespace Domain.Interfaces;

public interface IAdminPostDashboardRepository
{
    Task<AdminPostDashboardDto> GetPostDashboardAsync();
}