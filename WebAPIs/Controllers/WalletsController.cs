using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.WalletImp;
using Services.Request.WalletReq;
using Services.Response.WalletResp;

namespace WebAPIs.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/wallets")]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletsController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyWallet()
        {
            var result = await _walletService.GetMyWalletAsync();
            return Ok(result);
        }

        [HttpPost("top-up")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Truyền thẳng DTO xuống Service
            var success = await _walletService.ProcessTopUpAsync(request);
            return Ok(new { message = "Giao dịch thành công!" });
        }

        [HttpGet("me/transactions")]
        public async Task<IActionResult> GetMyTransactions()
        {
            var result = await _walletService.GetMyTransactionHistoryAsync();
            return Ok(result);
        }
    }
}