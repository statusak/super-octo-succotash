using CSCourse.Interfaces;
using CSCourse.Models;
using Microsoft.AspNetCore.Mvc;

namespace CSCourse.Controllers
{

    [ApiController]
    [Route("/[controller]")]
    public class BookingsController(IBookingService _bookingService) : ControllerBase
    {
        [HttpGet("{index:Guid}")]
        public async Task<ActionResult> GetById(Guid index)
        {
            Booking? booking = await _bookingService.GetBookingByIdAsync(index);
            if (booking != null) 
            {
                return Ok(booking);
            }
            return NotFound($"Booking with index {index} not found");
        }
    }
}
