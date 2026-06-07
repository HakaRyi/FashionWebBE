using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IOrderStatusHistoryRepository
    {
        Task AddAsync(OrderStatusHistory history);

        Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId);

        Task<OrderStatusHistory?> GetLatestByOrderIdAsync(int orderId);
    }
}
