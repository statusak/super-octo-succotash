using CSCourse.Dto;
using CSCourse.Interfaces;
using CSCourse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Net;

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
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }

        [HttpPost]
        public ActionResult Post([FromBody] EventDto @eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = new Event {
                Id = 0,
                Title = @eventDto.Title,
                Description = @eventDto.Description,
                StartAt = @eventDto.StartAt,
                EndAt = @eventDto.EndAt,
            };

            _eventService.CreateEvent(@event);
            return Created();
        }
        [HttpPut("{index:int}")]
        public ActionResult Put(int index, [FromBody] EventDto @eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = new Event
            {
                Id = 0,
                Title = @eventDto.Title,
                Description = @eventDto.Description,
                StartAt = @eventDto.StartAt,
                EndAt = @eventDto.EndAt,
            };

            try
            {
                _eventService.UpdateEvent(index, @event);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }

        [HttpDelete("{index:int}")]
        public ActionResult Delete(int index)
        {
            try
            {
                _eventService.DeleteEvent(index);
                return Ok();
            }
            catch (InvalidOperationException)
            {
                return NotFound($"Event with index {index} not found");
            }
        }
    }
}
