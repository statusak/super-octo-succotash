using CSCourse.Interfaces;
using CSCourse.Models;

namespace CSCourse.Services
{
    public class EventMemoryService : IEventService
    {
        private readonly List<Event> Events = [];
        private readonly object _lockCreateEvent = new object();

        public PaginatedResult GetAll(int page, int pageSize)
        {
            return new PaginatedResult
            {
                CountEvents = Events.Count,
                Events = Events.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            }; 
        }

        public PaginatedResult GetAll(FilterEvent filterEvent, int page, int pageSize)
        {
            var filteredEvents = Events.Where(e => e.Title.Contains(filterEvent.Title, StringComparison.CurrentCultureIgnoreCase));

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
                CountEvents = Events.Count,
                Events = filteredEvents.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        public Event? GetEventById(Guid id)
        {
            return Events.First(x => x.Id == id);
        }
        public Guid CreateEvent(Event @event)
        {
            Guid eventId;

            lock (_lockCreateEvent)
            {
                do
                {
                    eventId = Guid.NewGuid();
                }
                while (Events.Any(e => e.Id == eventId));

                @event.Id = eventId;
                Events.Add(@event);
            }

            return eventId;
        }

        public void UpdateEvent(Guid id, Event @event)
        {
            var @event_old = Events.First(x => x.Id == id);
            if (@event_old != null)
            {
                @event_old.Title = @event.Title;
                @event_old.Description = @event.Description;
                @event_old.StartAt = @event.StartAt;
                @event_old.EndAt = @event.EndAt;
            }
        }
        public void DeleteEvent(Guid id)
        {
            var @event = Events.First(x => x.Id == id);
            if (@event != null) {
                Events.Remove(@event);
            }
        }
    }
}
