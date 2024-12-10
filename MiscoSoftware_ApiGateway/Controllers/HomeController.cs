using Microsoft.AspNetCore.Mvc;

namespace MiscoSoftware_ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class HomeController : Controller
    {
        [HttpGet]
        public dynamic Index()
        {
            return Ok("0");
        }
    }
}
