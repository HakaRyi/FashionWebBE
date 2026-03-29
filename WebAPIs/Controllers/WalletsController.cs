using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.WalletImp;
using Services.Request.WalletReq;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/wallets")]
    [Authorize]
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
            try
            {
                var result = await _walletService.GetMyWalletAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("top-up")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _walletService.ProcessTopUpAsync(request);
                return Ok(new { message = "Giao dịch thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("me/transactions")]
        public async Task<IActionResult> GetMyTransactions()
        {
            try
            {
                var result = await _walletService.GetMyTransactionHistoryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var data = await _walletService.GetWalletDashboardAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}