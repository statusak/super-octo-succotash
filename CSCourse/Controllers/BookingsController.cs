using Microsoft.AspNetCore.Mvc;

namespace CSCourse.Controllers
{

    [ApiController]
    [Route("/[controller]")]
    public class BookingsController : ControllerBase
    {
        public ActionResult Index()
        {
            return Ok();
        }
    }
}
