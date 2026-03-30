using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.SearchImp;
using Services.Request.SearchReq;
using Services.Utils;
using System.Security.Claims;

namespace WebAPIs.Controllers
{
    [Route("api/searchs")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet("top-influencers")]
        public async Task<IActionResult> GetTopInfluencers()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _searchService.GetTopInfluencersAsync(userId);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetSearchHistory()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var result = await _searchService.GetSearchHistoryAsync(userId);
            return Ok(result);
        }

        [HttpPost("history")]
        public async Task<IActionResult> AddSearchHistory([FromBody] AddSearchHistoryRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Keyword)) return BadRequest("Keyword is required");

            await _searchService.AddSearchHistoryAsync(userId, request.Keyword);
            return Ok();
        }

        [HttpDelete("history")]
        public async Task<IActionResult> ClearSearchHistory()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            await _searchService.ClearSearchHistoryAsync(userId);
            return Ok();
        }

        [HttpGet("users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();
            if (string.IsNullOrWhiteSpace(q)) return BadRequest("Search query is required");

            var result = await _searchService.SearchUsersAsync(userId, q);
            return Ok(result);
        }
    }
}
