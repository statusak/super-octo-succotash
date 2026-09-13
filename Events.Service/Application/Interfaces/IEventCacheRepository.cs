using Events.Service.Application.Models;
using Events.Service.Domain.Models;

namespace Events.Service.Application.Interfaces;

public interface IEventCacheRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<bool> DeleteValueByIdAsync(Guid id);
    Task<List<Event>> GetTop10Async();
    Task<bool> DeleteValueTop10Async();
    Task<bool> UpdateAsync(EventRepositoryUpdateDto @event);

}