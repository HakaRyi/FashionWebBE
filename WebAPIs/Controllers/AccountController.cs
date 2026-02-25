using Microsoft.AspNetCore.Mvc;
using Services.Implements.AccountService;
using Services.Request.AccountReq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService service;
        public AccountController(IAccountService service)
        {
            this.service = service;
        }
        [HttpGet("count")]
        public async Task<IActionResult> CountUser()
        {
            var count = await service.CountAccount();
            if(count == null) 
            {
                return NotFound(new { message = "ko dem dc"});
            }
            return Ok(new { count });
        }
        [HttpGet("countExpert")]
        public async Task<IActionResult> CountExpert()
        {
            var count = await service.CountExpert();
            if (count == null)
            {
                return NotFound(new { message = "ko dem dc" });
            }
            return Ok(new { count });
        }
        [HttpGet("fashionExpert")]
        public async Task<IActionResult> GetFashionExpert()
        {
            var result = await service.GetFashionExpert();
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        // GET: api/<AccountController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await service.GetListAccount();
            if(result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        // GET api/<AccountController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute]int id)
        {
            return await service.GetAccountById(id) is var account && account != null
                ? Ok(account)
                : NotFound();
        }
        [HttpGet("expertDetail/{id}")]
        public async Task<IActionResult> GetByExpertId([FromRoute] int id)
        {
            return await service.GetFashionExpertDetail(id) is var expert && expert != null
                ? Ok(expert)
                : NotFound();

        }

        // PUT api/<AccountController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateAccountRequest request)
        {
            var result = await service.updateAccountRequest(id, request);
            if (result == "Update success")
            {
                return Ok(new { message = "Update success" });
            }
            else
            {
                return BadRequest(new { message = "Update fail" });
            }
        }
    }
}
