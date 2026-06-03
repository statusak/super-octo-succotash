using CSCourse.Models;

public interface IEventRepository
{
    Guid Create(Event @event);
    Event GetById(Guid id);
    List<Event> GetFilteredPage(FilterRepositoryEventDto filterEvent, int page, int pageSize);
    List<Event> GetPage(int page, int pageSize);
    bool IsExists(Guid id);
    bool TryReserveSeats(Guid id, int count);

    int Count();

    Task<Guid> CreateAsync(Event @event);
    Task<Event> GetByIdAsync(Guid id);
    Task<List<Event>> GetFilteredPageAsync(FilterRepositoryEventDto filterEvent, int page, int pageSize);
    Task<List<Event>> GetPageAsync(int page, int pageSize);
    Task<bool> IsExistsAsync(Guid id);
    Task<bool> TryReserveSeatsAsync(Guid id, int count);

    Task<int> CountAsync();
}