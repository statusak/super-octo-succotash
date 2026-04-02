using CSCourse.Interfaces;
using CSCourse.Models;
using Microsoft.AspNetCore.Mvc;

namespace CSCourse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController(IEventService _eventService) : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<Event>> GetAll()
        {
            return Ok(_eventService.GetAll());
        }

        [HttpGet("{index:int}")]
        public ActionResult<Event> GetById(int index)
        {
            try
            {
                return Ok(_eventService.GetEventById(index));
            }
            catch (ArgumentOutOfRangeException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }

        [HttpPost]
        public ActionResult Post([FromBody] Event @event)
        {
            _eventService.CreateEvent(@event);
            return Created();
        }
        //[HttpPut("{index: int}")]
        //public ActionResult Put(int index, [FromBody] Event @event)
        //{
        //    try
        //    {
        //        _eventService.UpdateEvent(index, @event);
        //        return NoContent();
        //    }
        //    catch (ArgumentOutOfRangeException)
        //    {
        //        return NotFound($"Event with index {index} not found");
        //    }
        //}

        [HttpDelete("{index:int}")]
        public ActionResult Delete(int index)
        {
            try
            {
                _eventService.DeleteEvent(index);
                return Ok();
            }
            catch (ArgumentOutOfRangeException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }
    }
}
