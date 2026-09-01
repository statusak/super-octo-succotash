using Events.Service.Domain.Models;

namespace Events.Service.Application.Interfaces;

public interface IEventCacheRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<List<Event>> GetTopAsync(int count=10);
}