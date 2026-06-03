using CSCourse.Models;

public interface IEventRepository
{
    Task<Guid> CreateAsync(Event @event);
    List<Event> GetPage(int page, int pageSize);
    Task<List<Event>> GetPageAsync(int page, int pageSize);
    int Count();
    Task<int> CountAsync();
}