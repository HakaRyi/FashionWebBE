using Application.Response.OrderResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.OrderImp
{
    public interface IOrderAdminService
    {
        Task<OrderAdminListPagedResponse> GetAllOrdersAsync(int pageNumber, int pageSize, string? status = null);
        Task<OrderAdminDetailResponse> GetOrderDetailForAdminAsync(string orderCode);
    }
}
