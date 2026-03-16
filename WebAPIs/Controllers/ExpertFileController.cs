using Microsoft.AspNetCore.Mvc;
using Services.Implements.ExpertsService.ExpertRequestImp;

namespace WebAPIs.Controllers
{
    [Route("api/expert-file")]
    [ApiController]
    public class ExpertFileController : ControllerBase
    {
        private readonly IExpertRequestService service;
        public ExpertFileController(IExpertRequestService service)
        {
            this.service = service;
        }
        
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await service.GetById(id);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        //    // POST api/<ExpertFileController>
        //    [HttpPost]
        //    public void Post([FromBody] string value)
        //    {
        //    }

        //    // PUT api/<ExpertFileController>/5
        //    [HttpPut("{id}")]
        //    public void Put(int id, [FromBody] string value)
        //    {
        //    }

        //    // DELETE api/<ExpertFileController>/5
        //    [HttpDelete("{id}")]
        //    public void Delete(int id)
        //    {
        //    }
    }
}
