using System.Text.Json;
using Events.Service.Application.Interfaces;
using Events.Service.Application.Models;
using Events.Service.Domain.Models;
using Events.Service.Infrastructure.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Events.Service.Infrastructure.Repositories;

public class EventCacheRepository : IEventCacheRepository
{
    private readonly IDatabase _redis;
    private readonly IEventRepository _repository;
    private readonly ILogger _logger;
    private readonly TimeSpan _expiryEventById = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _expiryTop10Events = TimeSpan.FromMinutes(1);

    public EventCacheRepository(
        IConnectionMultiplexer connection,
        IEventRepository repository,
        IOptions<RedisSettings> redisSettings,
        ILogger<EventCacheRepository> logger)
    {
        _redis = connection.GetDatabase();
        _repository = repository;
        _logger = logger;

        var settings = redisSettings.Value;
        _expiryEventById = TimeSpan.FromMinutes(settings.ExpiryEventByIdMinutes);
        _expiryTop10Events = TimeSpan.FromMinutes(settings.ExpiryTop10EventsMinutes);

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
            await _redis.StringSetAsync(key, serialized, _expiryEventById);
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
                _expiryTop10Events
            );
        } catch (RedisException ex)
        {
            _logger.LogError(ex, $"Ошибка установки ключа {cacheKey} в Redis");
        }

        return events;
    }

    public async Task<bool> DeleteValueByIdAsync(Guid id)
    {
        var key = $"event:{id}";

        try
        {
            var deleted = await _redis.KeyDeleteAsync(key);
            if (deleted)
            {
                _logger.LogInformation("Ключ {Key} удалён из Redis", key);
            }
            return deleted;
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Ошибка удаления ключа {Key} из Redis", key);
            return false;
        }
    }

    public async Task<bool> DeleteValueTop10Async()
    {
        const string cacheKey = "events:top10";

        try
        {
            var deleted = await _redis.KeyDeleteAsync(cacheKey);
            if (deleted)
            {
                _logger.LogInformation("Ключ {Key} (top10) удалён из Redis", cacheKey);
            }
            return deleted;
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Ошибка удаления ключа {Key} (top10) из Redis", cacheKey);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(EventRepositoryUpdateDto dto)
    {
        var updated = await _repository.UpdateAsync(dto);
        if (!updated)
        {
            return false;
        }

        await DeleteValueByIdAsync(dto.Id);
        await DeleteValueTop10Async();

        return true;
    }
}