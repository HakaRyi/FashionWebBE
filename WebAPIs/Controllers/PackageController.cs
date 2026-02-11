using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.PackageCoinImp;
using Services.Request.PackageReq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly IPackageCoinService _packageCoinService;
        public PackageController(IPackageCoinService packageCoinService)
        {
            _packageCoinService = packageCoinService;
        }
        // GET: api/<PackageController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result =  await _packageCoinService.GetAll();
            if(result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new {
                    message = "No packages found"
                });
            }
        }

        // GET api/<PackageController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var result = await _packageCoinService.GetById(id);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "No package found"
                });
            }
        }

        // POST api/<PackageController>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] PackageRequest request)
        {
            var userId = User.FindFirst("AccountID")?.Value;
            var package = await _packageCoinService.CreateAsync(request,int.Parse(userId));
            if (package >0)
            {
                return Ok(new {
                    message = "Package created successfully"
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Package creation failed"
                });
            }
        }

        // PUT api/<PackageController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] PackageRequest request)
        {
            var updated = await _packageCoinService.UpdateAsync(request,id);
            if (updated>0)
            {
                return Ok(new
                {
                    message = "Package updated successfully"
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Package update failed"
                });
            }
        }

        [HttpPut("inactivePackage/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _packageCoinService.DeleteAsync(id);
            if (deleted == "success")
            {
                return Ok(new
                {
                    message = "Package inactivated successfully"
                });
            }
            else if (deleted == "ko thay")
            {
                return StatusCode(404, new
                {
                    message = "Package not found"
                });
            }
            else 
            {
                return StatusCode(500, new
                {
                    message = "Package inactivation failed"
                });
            }
        }
    }
}
