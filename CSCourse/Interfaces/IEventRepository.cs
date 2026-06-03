using CSCourse.Models;

public interface IEventRepository
{
    Task<Guid> CreateAsync(Event @event);
}