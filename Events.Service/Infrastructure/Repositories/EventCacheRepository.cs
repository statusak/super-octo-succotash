using System.Text.Json;
using Events.Service.Application.Interfaces;
using Events.Service.Domain.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Events.Service.Infrastructure.Repositories;

public class EventCacheRepository : IEventCacheRepository
{
    private readonly IDatabase _redis;
    private readonly IEventRepository _repository;
    private readonly ILogger _logger;
    private static readonly TimeSpan ExpiryEventById = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ExpiryTop10Events = TimeSpan.FromMinutes(1);

    public EventCacheRepository(IConnectionMultiplexer connection, IEventRepository repository, ILogger logger)
    {
        _redis = connection.GetDatabase();
        _repository = repository;
        _logger = logger;
    }


    public async Task<Event?> GetByIdAsync(Guid id)
    {
        var key = $"event:{id}";

        var cached = await _redis.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<Event>(cached.ToString());

        var @event = await _repository.GetByIdAsync(id);
        if (@event is null)
            return null;

        var serialized = JsonSerializer.Serialize(@event);


        try{
            await _redis.StringSetAsync(key, serialized, ExpiryEventById);
        } catch (RedisException ex)
        {
            _logger.LogError(ex, $"Ошибка установки ключа {key} в Redis");
        }
        return @event;
    }

    public async Task<List<Event>> GetTop10Async()
    {
        const string cacheKey = "events:top10";

        try
        {
            var cached = await _redis.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                return JsonSerializer.Deserialize<List<Event>>(cached.ToString())!;
            }
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, $"Ошибка получения ключа {cacheKey} из Redis при получении top10");
        }

        var events = await _repository.GetTop10Async();

        try
        {
            await _redis.StringSetAsync(
                cacheKey,
                JsonSerializer.Serialize(events),
                ExpiryTop10Events
            );
        } catch (RedisException ex)
        {
            _logger.LogError(ex, $"Ошибка установки ключа {cacheKey} в Redis");
        }

        return events;
    }
}