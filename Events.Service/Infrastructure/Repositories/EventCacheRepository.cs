using System.Text.Json;
using Events.Service.Application.Interfaces;
using Events.Service.Application.Models;
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

        var idKey = $"event:{dto.Id}";
        try
        {
            await _redis.KeyDeleteAsync(idKey);
            _logger.LogDebug("Инвалидирован кэш для события с ID {Id}", dto.Id);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Не удалось инвалидировать кэш для события с ID {Id}", dto.Id);
        }

        const string top10Key = "events:top10";
        try
        {
            await _redis.KeyDeleteAsync(top10Key);
            _logger.LogDebug("Инвалидирован кэш top10 событий");
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Не удалось инвалидировать кэш top10 событий");
        }

        return true;
    }
}