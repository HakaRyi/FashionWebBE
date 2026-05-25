using Application.Response.ItemResp;
using Application.Services.Items;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/items-analysis")]
    public class ItemsAnalysisController : ControllerBase
    {
        private readonly IItemAnalysisService _itemAnalysis;
        public ItemsAnalysisController(IItemAnalysisService itemAnalysis)
        {
            _itemAnalysis = itemAnalysis;
        }

        [HttpGet("market-throughput")]
        public async Task<ActionResult<FashionIntelligenceResp>> GetMarketThroughput(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? filterType = null,
            [FromQuery] string? filterValue = null,
            [FromQuery] string viewMode = "date")
        {
            var result = await _itemAnalysis.GetMarketThroughputAsync(
                startDate,
                endDate,
                filterType,
                filterValue,
                viewMode
            );

            return Ok(result);
        }
    }
}
