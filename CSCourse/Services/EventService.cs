using CSCourse.DataAccess;
using CSCourse.Interfaces;
using CSCourse.Models;
using System.ComponentModel.DataAnnotations;

namespace CSCourse.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;
        private readonly object _lockCreateEvent = new object();

        public EventService(AppDbContext context)
        {
            _context = context;
        }

        public PaginatedResult GetAll(int page, int pageSize)
        {
            return new PaginatedResult
            {
                CountEvents = _context.Events.Count(),
                Events = _context.Events.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            }; 
        }

        public PaginatedResult GetAll(FilterEvent filterEvent, int page, int pageSize)
        {
            var filteredEvents = _context.Events.Where(e => e.Title.Contains(filterEvent.Title, StringComparison.CurrentCultureIgnoreCase));

            if (filterEvent.StartAt != null)
            {
                filteredEvents = filteredEvents.Where(e => e.StartAt >= filterEvent.StartAt);
            }

            if (filterEvent.EndAt != null)
            {
                filteredEvents = filteredEvents.Where(e => e.EndAt <= filterEvent.EndAt);
            }

            return new PaginatedResult
            {
                CountEvents = _context.Events.Count(),
                Events = filteredEvents.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        public Event? GetEventById(Guid id)
        {
            return _context.Events.First(x => x.Id == id);
        }

        public bool IsEventExists(Guid id)
        {
            return _context.Events.Any(x => x.Id == id);
        }

        public bool TryReserveSeats(Guid id, int count = 1)
        {
            lock (_lockCreateEvent)
            {
                var @event = _context.Events.First(x => x.Id == id);
                if (@event.AvailableSeats - count < 0) {
                    return false;
                }
                @event.AvailableSeats -= count;
                return true;
            }
        }

        public bool ReleaseSeats(Guid id, int count = 1)
        {
            lock (_lockCreateEvent)
            {
                var @event = _context.Events.First(x => x.Id == id);
                if (@event.AvailableSeats + count > @event.TotalSeats)
                {
                    return false;
                }
                @event.AvailableSeats += count;
                return true;
            }
        }
        public Guid CreateEvent(Event @event)
        {
            if (@event.TotalSeats <= 0)
            {
                throw new ValidationException("@event.TotalSeats <= 0");
            }

            Guid eventId;

            lock (_lockCreateEvent)
            {
                do
                {
                    eventId = Guid.NewGuid();
                }
                while (_context.Events.Any(e => e.Id == eventId));

                @event.Id = eventId;
                _context.Events.Add(@event);
            }

            return eventId;
        }

        public void UpdateEvent(Guid id, Event @event)
        {
            var @event_old = _context.Events.First(x => x.Id == id);
            if (@event_old != null)
            {
                @event_old.Title = @event.Title;
                @event_old.Description = @event.Description;
                @event_old.StartAt = @event.StartAt;
                @event_old.EndAt = @event.EndAt;
            }
        }

        public void UpdateEvent(Guid id, string Title, string? Description, DateTime StartAt, DateTime EndAt)
        {
            var @event_old = _context.Events.First(x => x.Id == id);
            if (@event_old != null)
            {
                @event_old.Title = Title;
                @event_old.Description = Description;
                @event_old.StartAt = StartAt;
                @event_old.EndAt = EndAt;
            }
        }
        public void DeleteEvent(Guid id)
        {
            var @event = _context.Events.First(x => x.Id == id);
            if (@event != null) {
                _context.Events.Remove(@event);
            }
        }
    }
}
