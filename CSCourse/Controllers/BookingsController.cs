using Microsoft.AspNetCore.Mvc;

namespace CSCourse.Controllers
{

    [ApiController]
    [Route("/[controller]")]
    public class BookingsController : ControllerBase
    {
        [HttpGet("{index:Guid}")]
        public ActionResult GetById(Guid index)
        {
            return Ok();
        }
    }
}
