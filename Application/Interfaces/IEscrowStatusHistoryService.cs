using Application.Request.EscrowStatusHistoryReq;
using Application.Response.EscrowStatusHistoryResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEscrowStatusHistoryService
    {
        Task<EscrowStatusHistoryResponse?> GetByIdAsync(int id);
        Task<IEnumerable<EscrowStatusHistoryResponse>> GetByEscrowSessionIdAsync(int escrowSessionId);
        Task<EscrowStatusHistoryResponse> CreateHistoryAsync(CreateEscrowStatusHistoryRequest request);
    }
}
