using Microsoft.AspNetCore.Mvc;

namespace CSCourse.Controllers
{

    [ApiController]
    [Route("/[controller]")]
    public class BookingsController : ControllerBase
    {
        [HttpGet]
        public ActionResult Index()
        {
            return Ok();
        }
    }
}
