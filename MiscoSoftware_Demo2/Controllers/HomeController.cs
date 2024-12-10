using Microsoft.AspNetCore.Mvc;

namespace MiscoSoftware_Demo2.Controllers
{
    [ApiController]
    [Route("demo2/api/[controller]/[action]")]
    public class HomeController : Controller
    {
        [HttpGet]
        public dynamic Index()
        {
            return Ok("demo2-0");
        }
    }
}
