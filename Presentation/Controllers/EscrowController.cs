using Application.Interfaces;
using Application.Request.EscrowStatusHistoryReq;
using Application.Response.EscrowStatusHistoryResp;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/escrow")]
    [ApiController]
    public class EscrowController : ControllerBase
    {
        private readonly IEscrowStatusHistoryService _escrowStatusHistoryService;
        public EscrowController(IEscrowStatusHistoryService escrowStatusHistoryService)
        {
            _escrowStatusHistoryService = escrowStatusHistoryService;
        }

        [HttpGet("history/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EscrowStatusHistoryResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistoryById(int id)
        {
            var result = await _escrowStatusHistoryService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"No margin trading history found with ID: {id}." });
            }
            return Ok(result);
        }

        [HttpGet("session/{sessionId:int}/history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EscrowStatusHistoryResponse>))]
        public async Task<IActionResult> GetHistoryBySessionId(int sessionId)
        {
            var result = await _escrowStatusHistoryService.GetByEscrowSessionIdAsync(sessionId);
            return Ok(result);
        }

        [HttpPost("history")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EscrowStatusHistoryResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateHistory([FromBody] CreateEscrowStatusHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdHistory = await _escrowStatusHistoryService.CreateHistoryAsync(request);

            return CreatedAtAction(nameof(GetHistoryById), new { id = createdHistory.EscrowStatusHistoryId }, createdHistory);
        }
    }
}
