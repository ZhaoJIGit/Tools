using Microsoft.AspNetCore.Mvc;

namespace MicroSoftware_Demo1.Controllers
{
    [ApiController]
    [Route("demo1/api/[controller]/[action]")]
    public class HomeController : Controller
    {
        [HttpGet]
        public dynamic Index()
        {
            return Ok("demo1-0");
        }
    }
}
