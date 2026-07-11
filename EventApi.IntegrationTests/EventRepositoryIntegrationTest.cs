using CSCourse.Domain.Models;
using CSCourse.Application.Interfaces;
using CSCourse.Application.Models;
using CSCourse.Infrastructure.Repositories;
using CSCourse.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;

public class EventRepositoryIntegrationTest : IAsyncLifetime
{
    private IEventRepository _repo = null!;
    private AppDbContext _context = null!;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync(TestContext.Current.CancellationToken);
        _context = CreateContext();
        InitializeServices();
        await ResetDatabaseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.Migrate();
        return context;
    }

    private void InitializeServices()
    {
        _repo = new EventRepository(_context);
    }

    private void RefreshServices()
    {
        _context = CreateContext();
        InitializeServices();
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE",
            cancellationToken: TestContext.Current.CancellationToken);
    }

    
    private static Event CreateTestEvent(
        string title = "Test Event",
        string? description = null,
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 100,
        int availableSeats = 100)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description ?? $"Description for {title}",
            StartAt = startAt ?? DateTime.UtcNow,
            EndAt = endAt ?? DateTime.UtcNow.AddHours(2),
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats
        };
    }

    private static void AssertDateTimeAlmostEqual(DateTime expected, DateTime actual, int maxMillisecondsDiff = 10)
    {
        var diff = Math.Abs((expected - actual).TotalMilliseconds);
        Assert.True(
            diff <= maxMillisecondsDiff,
            $"DateTime mismatch: expected {expected:o}, actual {actual:o}, diff={diff} ms");
    }

    [Fact]
    public async Task GetFilteredPageAsync_FiltersByStartAt_ReturnsCorrectEvents()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var baseTime = DateTime.UtcNow;
        var e1 = CreateTestEvent("Early", startAt: baseTime.AddDays(-2));
        var e2 = CreateTestEvent("Middle", startAt: baseTime.AddDays(-1));
        var e3 = CreateTestEvent("Late", startAt: baseTime);

        await _context.Events.AddRangeAsync([e1, e2, e3], ct);
        await _context.SaveChangesAsync(ct);

        var filter = new FilterRepositoryEventDto
        {
            Title = "", 
            StartAt = baseTime.AddDays(-1.5)
        };

        var result = await _repo.GetFilteredPageAsync(filter, page: 1, pageSize: 10);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == e2.Id);
        Assert.Contains(result, e => e.Id == e3.Id);
    }

    [Fact]
    public async Task GetFilteredPageAsync_FiltersByEndAt_ReturnsCorrectEvents()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var baseTime = DateTime.UtcNow;
        var e1 = CreateTestEvent("Short", endAt: baseTime.AddHours(-2));
        var e2 = CreateTestEvent("Medium", endAt: baseTime.AddHours(-1));
        var e3 = CreateTestEvent("Long", endAt: baseTime.AddHours(3));

        await _context.Events.AddRangeAsync([e1, e2, e3], ct);
        await _context.SaveChangesAsync(ct);

        var filter = new FilterRepositoryEventDto
        {
            Title = "",
            EndAt = baseTime
        };

        var result = await _repo.GetFilteredPageAsync(filter, page: 1, pageSize: 10);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == e1.Id);
        Assert.Contains(result, e => e.Id == e2.Id);
    }

    [Fact]
    public async Task GetFilteredPageAsync_CombinesTitleAndDateFilters()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var baseTime = DateTime.UtcNow;
        var e1 = CreateTestEvent("Foo Event", startAt: baseTime.AddDays(-1));
        var e2 = CreateTestEvent("Bar Event", startAt: baseTime.AddDays(1));
        var e3 = CreateTestEvent("Foo Bar", startAt: baseTime);

        await _context.Events.AddRangeAsync([e1, e2, e3], ct);
        await _context.SaveChangesAsync(ct);

        var filter = new FilterRepositoryEventDto
        {
            Title = "foo",
            StartAt = baseTime.AddDays(-0.5)
        };

        var result = await _repo.GetFilteredPageAsync(filter, page: 1, pageSize: 10);

        Assert.Single(result);
        Assert.Equal(e3.Id, result[0].Id);
    }

    [Fact]
    public async Task GetPageAsync_PaginationWorksCorrectly()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        const int total = 25;
        const int pageSize = 10;

        var events = Enumerable.Range(0, total)
            .Select(i => CreateTestEvent($"Event {i}", startAt: DateTime.UtcNow.AddDays(i)))
            .ToList();

        await _context.Events.AddRangeAsync(events, ct);
        await _context.SaveChangesAsync(ct);

        var page1 = await _repo.GetPageAsync(page: 1, pageSize);
        var page2 = await _repo.GetPageAsync(page: 2, pageSize);
        var page3 = await _repo.GetPageAsync(page: 3, pageSize);

        Assert.Equal(10, page1.Count);
        Assert.Equal(10, page2.Count);
        Assert.Equal(5, page3.Count);

        var allIds = page1.Concat(page2).Concat(page3).Select(e => e.Id).ToList();
        Assert.Equal(total, allIds.Count);
        Assert.Equal(total, allIds.Distinct().Count());
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectTotal()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var e1 = CreateTestEvent();
        var e2 = CreateTestEvent();
        var e3 = CreateTestEvent();

        await _context.Events.AddRangeAsync([e1, e2, e3], ct);
        await _context.SaveChangesAsync(ct);

        var count = await _repo.CountAsync();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task IsExistsAsync_ReturnsTrue_WhenEventExists()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var evt = CreateTestEvent();
        await _context.Events.AddAsync(evt, ct);
        await _context.SaveChangesAsync(ct);

        var exists = await _repo.IsExistsAsync(evt.Id);

        Assert.True(exists);
    }

    [Fact]
    public async Task IsExistsAsync_ReturnsFalse_WhenEventDoesNotExist()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var nonExistingId = Guid.NewGuid();
        var exists = await _repo.IsExistsAsync(nonExistingId);

        Assert.False(exists);
    }

    [Fact]
    public async Task CreateAsync_SavesEventWithCorrectDateTime_Precision()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var startAt = DateTime.UtcNow.AddMilliseconds(123);
        var endAt = startAt.AddMinutes(30);
        var evt = CreateTestEvent("Precision Test", startAt: startAt, endAt: endAt);

        var createdId = await _repo.CreateAsync(evt);

        var saved = await _context.Events.FirstAsync(e => e.Id == createdId, ct);

        AssertDateTimeAlmostEqual(startAt, saved.StartAt);
        AssertDateTimeAlmostEqual(endAt, saved.EndAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEvent_AndCountDecreases()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var evt = CreateTestEvent();
        await _context.Events.AddAsync(evt, ct);
        await _context.SaveChangesAsync(ct);

        var initialCount = await _repo.CountAsync();

        var deleted = await _repo.DeleteAsync(evt.Id);

        Assert.True(deleted);

        var finalCount = await _repo.CountAsync();
        Assert.Equal(initialCount - 1, finalCount);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_IfEventNotFound()
    {
        RefreshServices();
        
        var nonExistingId = Guid.NewGuid();
        var deleted = await _repo.DeleteAsync(nonExistingId);

        Assert.False(deleted);
    }

    [Fact]
    public async Task UpdateAsync_CorrectlyUpdatesFields()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var original = CreateTestEvent("Original Title", "Original Desc");
        await _context.Events.AddAsync(original, ct);
        await _context.SaveChangesAsync(ct);

        var updateDto = new EventRepositoryUpdateDto
        {
            Id = original.Id,
            Title = "Updated Title",
            Description = "Updated Desc",
            StartAt = original.StartAt.AddHours(1),
            EndAt = original.EndAt.AddHours(1)
        };

        var updated = await _repo.UpdateAsync(updateDto);

        Assert.True(updated);

        _context.Entry(original).State = EntityState.Detached;
        var refreshed = await _context.Events.FirstAsync(e => e.Id == original.Id, ct);
        Assert.Equal("Updated Title", refreshed.Title);
        Assert.Equal("Updated Desc", refreshed.Description);
        AssertDateTimeAlmostEqual(updateDto.StartAt, refreshed.StartAt);
        AssertDateTimeAlmostEqual(updateDto.EndAt, refreshed.EndAt);
    }

    [Fact]
    public async Task TryReserveSeatsAsync_Succeeds_WhenEnoughSeats()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var evt = CreateTestEvent("Seats Test", totalSeats: 10, availableSeats: 10);
        await _context.Events.AddAsync(evt, ct);
        await _context.SaveChangesAsync(ct);

        var reserved = await _repo.TryReserveSeatsAsync(evt.Id, 3);

        Assert.True(reserved);

        var refreshed = await _context.Events.FirstAsync(e => e.Id == evt.Id, ct);
        Assert.Equal(7, refreshed.AvailableSeats);
    }

    [Fact]
    public async Task TryReserveSeatsAsync_Fails_WhenNotEnoughSeats()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var evt = CreateTestEvent("Seats Test", totalSeats: 5, availableSeats: 2);
        await _context.Events.AddAsync(evt, ct);
        await _context.SaveChangesAsync(ct);

        var reserved = await _repo.TryReserveSeatsAsync(evt.Id, 5);

        Assert.False(reserved);

        var refreshed = await _context.Events.FirstAsync(e => e.Id == evt.Id, ct);
        Assert.Equal(2, refreshed.AvailableSeats);
    }

    [Fact]
    public async Task TryReleaseSeatsAsync_Succeeds_WithinTotalSeats()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var evt = CreateTestEvent("Release Test", totalSeats: 10, availableSeats: 5);
        await _context.Events.AddAsync(evt, ct);
        await _context.SaveChangesAsync(ct);

        var released = await _repo.TryReleaseSeatsAsync(evt.Id, 3);

        Assert.True(released);

        var refreshed = await _context.Events.FirstAsync(e => e.Id == evt.Id, ct);
        Assert.Equal(8, refreshed.AvailableSeats);
    }

    [Fact]
    public async Task TryReleaseSeatsAsync_Fails_IfExceedsTotalSeats()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var evt = CreateTestEvent("Release Test", totalSeats: 10, availableSeats: 8);
        await _context.Events.AddAsync(evt, ct);
        await _context.SaveChangesAsync(ct);

        var released = await _repo.TryReleaseSeatsAsync(evt.Id, 5); // 8+5=13 > 10

        Assert.False(released);

        var refreshed = await _context.Events.FirstAsync(e => e.Id == evt.Id, ct);
        Assert.Equal(8, refreshed.AvailableSeats);
    }
}
