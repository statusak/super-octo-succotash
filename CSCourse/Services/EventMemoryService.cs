using CSCourse.Models;

namespace CSCourse.Services
{
    public class EventMemoryService : IEventService
    {
        private static int _ID = 0;
        private static List<Event> Events { get; set; } = [];

        public PaginatedResult GetAll(int page, int pageSize)
        {
            return new PaginatedResult
            {
                CountEvents = Events.Count,
                Events = Events.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            }; 
        }

        public PaginatedResult GetAll(FilterEvent @filterEvent, int page, int pageSize)
        {
            var filteredEvents = Events.Where(e => e.Title.ToLower().Contains(@filterEvent.Title));

            if (@filterEvent.StartAt != null)
            {
                filteredEvents = filteredEvents.Where(e => @filterEvent.StartAt >= e.StartAt);
            }

            if (@filterEvent.EndAt != null)
            {
                filteredEvents = filteredEvents.Where(e => @filterEvent.EndAt <= e.EndAt);
            }

            return new PaginatedResult
            {
                CountEvents = Events.Count,
                Events = filteredEvents.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        public Event? GetEventById(int id)
        {
            return Events.First(x => x.Id == id);
        }
        public int CreateEvent(Event @event)
        {
            @event.Id = _ID++;
            Events.Add(@event);
            return @event.Id;
        }

        public void UpdateEvent(int id, Event @event)
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
        public void DeleteEvent(int id)
        {
            var @event = Events.First(x => x.Id == id);
            if (@event != null) {
                Events.Remove(@event);
            }
        }
    }
}
