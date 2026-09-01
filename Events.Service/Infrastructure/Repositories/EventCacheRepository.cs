using System.Text.Json;
using Events.Service.Application.Interfaces;
using Events.Service.Domain.Models;
using StackExchange.Redis;

namespace Events.Service.Infrastructure.Repositories;

public class EventCacheRepository : IEventCacheRepository
{
    private readonly IDatabase _redis;
    private readonly IEventRepository _repository;
    private static readonly TimeSpan ExpiryEventById = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ExpiryTop10Events = TimeSpan.FromMinutes(1);

    public EventCacheRepository(IConnectionMultiplexer connection, IEventRepository repository)
    {
        _redis = connection.GetDatabase();
        _repository = repository;
    }


    public async Task<Event?> GetByIdAsync(Guid id)
    {
        var key = $"event:{id}";

        var cached = await _redis.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<Event>(cached!);

        var @event = await _repository.GetByIdAsync(id);
        if (@event is null)
            return null;

        var serialized = JsonSerializer.Serialize(@event);
        await _redis.StringSetAsync(key, serialized, ExpiryEventById);

        return @event;
    }

    public async Task<List<Event>> GetTop10Async()
    {
        const string cacheKey = "events:top10";

        var cached = await _redis.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<List<Event>>(cached!)!;
        }

        var events = await _repository.GetTop10Async();

        await _redis.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(events),
            ExpiryTop10Events
        );

        return events;
    }
}