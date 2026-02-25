using Microsoft.AspNetCore.Mvc;
using Services.Implements.TransactionImp;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }
        // GET: api/<TransactionController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _transactionService.GetTransactions();
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "No transactions found"
                });
            }

        }

        // GET api/<TransactionController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var result = await _transactionService.GetById(id);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "Transaction not found"
                });
            }
        }

        // POST api/<TransactionController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<TransactionController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<TransactionController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
