using Microsoft.AspNetCore.Mvc;
using Services.Implements.UserReportImp;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserReportController : ControllerBase
    {
        private readonly IUserReportService _userReportService;
        public UserReportController(IUserReportService userReportService)
        {
            _userReportService = userReportService;
        }
        // GET: api/<UserReportController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _userReportService.GetAll();
            if (result != null && result.Any())
            {
                return Ok(result);
            }
            else
            {
                return NotFound(new { message = "No user reports found." });
            }
        }

        // GET api/<UserReportController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var result = await _userReportService.GetById(id);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(new { message = $"User report with ID {id} not found." });
            }

        }

        //// POST api/<UserReportController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<UserReportController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<UserReportController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
