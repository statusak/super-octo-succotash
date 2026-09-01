using System.Text.Json;
using Events.Service.Application.Interfaces;
using Events.Service.Application.Models;
using Events.Service.Domain.Models;
using Events.Service.Infrastructure.Repositories;
using Events.Service.Infrastructure.Config;
using Events.Service.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;

namespace Tests.Unit;

public class CacheUnitTests
{
    private readonly IDatabase _redis;
    private readonly IEventRepository _repository;
    private readonly EventCacheRepository _sut;

    public CacheUnitTests()
    {
        _redis = Substitute.For<IDatabase>();

        var connection = Substitute.For<IConnectionMultiplexer>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_redis);

        _repository = Substitute.For<IEventRepository>();

        var settings = new RedisSettings
        {
            Servers = "localhost:6379",
            ExpiryEventByIdMinutes = 10,
            ExpiryTop10EventsMinutes = 1
        };
        var options = Options.Create(settings);

        var logger = Substitute.For<ILogger<EventCacheRepository>>();

        _sut = new EventCacheRepository(connection, _repository, options, logger);
    }

    // ── GetByIdAsync: попадание в кеш ──

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsFromCacheAndDoesNotCallRepository()
    {
        var id = Guid.NewGuid();
        var cachedEvent = new Event { Id = id, Title = "Cached" };
        var serialized = JsonSerializer.Serialize(cachedEvent);

        _redis.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)serialized);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    // ── GetByIdAsync: промах ──

    [Fact]
    public async Task GetByIdAsync_CacheMiss_GetsFromRepositoryAndSavesToCache()
    {
        var id = Guid.NewGuid();
        var repoEvent = new Event { Id = id, Title = "From Repo" };

        _redis.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        _repository.GetByIdAsync(id).Returns(repoEvent);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        await _repository.Received(1).GetByIdAsync(id);
        await _redis.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == $"event:{id}"),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task GetByIdAsync_CacheMissRepoReturnsNull_DoesNotSaveToCache()
    {
        var id = Guid.NewGuid();

        _redis.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        _repository.GetByIdAsync(id).Returns((Event?)null);

        var result = await _sut.GetByIdAsync(id);

        Assert.Null(result);
        await _redis.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());
    }

    // ── GetTop10Async: попадание в кеш ──

    [Fact]
    public async Task GetTop10Async_CacheHit_ReturnsFromCacheAndDoesNotCallRepository()
    {
        var cached = new List<Event>
        {
            new() { Id = Guid.NewGuid(), Title = "E1" },
            new() { Id = Guid.NewGuid(), Title = "E2" }
        };
        var serialized = JsonSerializer.Serialize(cached);

        _redis.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)serialized);

        var result = await _sut.GetTop10Async();

        Assert.Equal(2, result.Count);
        await _repository.DidNotReceive().GetTop10Async();
    }

    // ── GetTop10Async: промах ──

    [Fact]
    public async Task GetTop10Async_CacheMiss_GetsFromRepositoryAndSavesToCache()
    {
        var repoEvents = new List<Event>
        {
            new() { Id = Guid.NewGuid(), Title = "E1" },
            new() { Id = Guid.NewGuid(), Title = "E2" }
        };

        _redis.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        _repository.GetTop10Async().Returns(repoEvents);

        var result = await _sut.GetTop10Async();

        Assert.Equal(2, result.Count);
        await _repository.Received(1).GetTop10Async();
        await _redis.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "events:top10"),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());
    }

    // ── DeleteValueByIdAsync ──

    [Fact]
    public async Task DeleteValueByIdAsync_CallsRedisKeyDeleteWithCorrectKey()
    {
        var id = Guid.NewGuid();
        _redis.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var result = await _sut.DeleteValueByIdAsync(id);

        Assert.True(result);
        await _redis.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == $"event:{id}"),
            Arg.Any<CommandFlags>());
    }

    // ── DeleteValueTop10Async ──

    [Fact]
    public async Task DeleteValueTop10Async_CallsRedisKeyDeleteWithCorrectKey()
    {
        _redis.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var result = await _sut.DeleteValueTop10Async();

        Assert.True(result);
        await _redis.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "events:top10"),
            Arg.Any<CommandFlags>());
    }

    // ── UpdateAsync: репозиторий обновил → кеш инвалидируется ──

    [Fact]
    public async Task UpdateAsync_RepoSucceeds_InvalidatesBothByIdAndTop10Cache()
    {
        var id = Guid.NewGuid();
        var dto = new EventRepositoryUpdateDto { Id = id };

        _repository.UpdateAsync(dto).Returns(true);
        _redis.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var result = await _sut.UpdateAsync(dto);

        Assert.True(result);
        await _repository.Received(1).UpdateAsync(dto);
        await _redis.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == $"event:{id}"),
            Arg.Any<CommandFlags>());
        await _redis.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "events:top10"),
            Arg.Any<CommandFlags>());
    }

    // ── UpdateAsync: репозиторий не обновил → кеш не трогается ──

    [Fact]
    public async Task UpdateAsync_RepoFails_DoesNotInvalidateCache()
    {
        var dto = new EventRepositoryUpdateDto { Id = Guid.NewGuid() };

        _repository.UpdateAsync(dto).Returns(false);

        var result = await _sut.UpdateAsync(dto);

        Assert.False(result);
        await _repository.Received(1).UpdateAsync(dto);
        await _redis.DidNotReceive().KeyDeleteAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<CommandFlags>());
    }
}
