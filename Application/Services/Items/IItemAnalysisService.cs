using Application.Response.ItemResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Items
{
    public interface IItemAnalysisService
    {
        Task<FashionIntelligenceResp> GetMarketThroughputAsync(DateTime? startDate, DateTime? endDate, string? filterType = null, string? filterValue = null, string viewMode = "date");
    }
}
