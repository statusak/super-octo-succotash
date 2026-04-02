using CSCourse.Models;

namespace CSCourse.Interfaces
{
    public interface IEventService
    {
        List<Event> GetAll();
        Event GetEventById(int id);
        void CreateEvent(Event @event);
        void UpdateEvent(int id, Event @event);
        void DeleteEvent(int id);
    }
}
