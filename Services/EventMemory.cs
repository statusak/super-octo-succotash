using CSCourse.Interfaces;
using CSCourse.Models;

namespace CSCourse.Services
{
    public class EventMemory : IEventService
    {
        private static int _ID = 0;
        public static List<Event> Events { get; set; } = [];

        public List<Event> GetAll()
        {
            return Events;
        }

        public Event? GetEventById(int id)
        {
            return Events.First(x => x.Id == id);
        }
        public void CreateEvent(Event @event)
        {
            @event.Id = _ID++;
            Events.Add(@event);
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
