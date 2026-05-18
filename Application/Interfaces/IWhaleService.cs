using Application.Response.TransactionResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IWhaleService
    {
        Task<List<WhaleDashboardDto>> GetTopWhalesAsync(DateTime fromDate, DateTime toDate, string? viewMode, string? searchQuery);
        Task<WhaleHistoryDto?> GetWhaleHistoryAsync(int walletId);

    }
}
