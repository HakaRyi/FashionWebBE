using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IRefundRequestRepository
    {
        Task<RefundRequest> AddAsync(RefundRequest request);
        RefundRequest Update(RefundRequest request);
        Task<RefundRequest?> GetByOrderIdAsync(int orderId);
        Task<RefundRequest?> GetByIdAsync(int id);
        Task<List<RefundRequest>> GetAllAsync();
    }
}
