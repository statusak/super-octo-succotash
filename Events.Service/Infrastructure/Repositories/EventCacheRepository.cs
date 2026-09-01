using System.Text.Json;
using Events.Service.Application.Interfaces;
using Events.Service.Domain.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Events.Service.Infrastructure.Repositories;

public class EventCacheRepository : IEventCacheRepository
{
    private readonly IDatabase _redis;
    private readonly IEventRepository _events;
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);

    public EventCacheRepository(IConnectionMultiplexer connection, IEventRepository events)
    {
        _redis = connection.GetDatabase();
        _events = events;
    }


    public async Task<Event?> GetByIdAsync(Guid id)
    {
        var key = $"event:{id}";

        var cached = await _redis.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<Event>(cached!);

        var @event = await _events.GetByIdAsync(id);
        if (@event is null)
            return null;

        var serialized = JsonSerializer.Serialize(@event);
        await _redis.StringSetAsync(key, serialized, Expiry);

        return @event;
    }

    public async Task<List<Event>> GetTopAsync(int count = 10)
    {
        return null;
    }
}