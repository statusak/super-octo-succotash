using CSCourse.Models;

public interface IEventRepository
{
    Task<Guid> CreateAsync(Event @event);

    Event GetById(Guid id);
    Task<Event> GetByIdAsync(Guid id);

    List<Event> GetFilteredPage(FilterRepositoryEventDto filterEvent, int page, int pageSize);
    Task<List<Event>> GetFilteredPageAsync(FilterRepositoryEventDto filterEvent, int page, int pageSize);
    List<Event> GetPage(int page, int pageSize);
    Task<List<Event>> GetPageAsync(int page, int pageSize);
    
    bool IsExists(Guid id);
    Task<bool> IsExistsAsync(Guid id);

    int Count();
    Task<int> CountAsync();
}