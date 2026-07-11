using CSCourse.Domain.Models;
using CSCourse.Application.Interfaces;
using CSCourse.Application.Models;
using CSCourse.Infrastructure.Repositories;
using CSCourse.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;
public class BookingRepositoryIntegrationTest : IAsyncLifetime
{
    private IBookingRepository _repo = null!;
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
        _repo = new BookingRepository(_context);
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

    private static Booking CreateTestBooking(
        Guid eventId,
        BookingStatus status = BookingStatus.Pending,
        DateTime? createdAt = null,
        DateTime? processedAt = null)
    {
        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ProcessedAt = processedAt
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
    public async Task CreateAsync_SavesBooking_WithCorrectValues()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var eventId = Guid.NewGuid();
        
        await _context.Events.AddAsync(new Event
        {
            Id = eventId,
            Title = "Test Event",
            TotalSeats = 10,
            AvailableSeats = 10,
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(2)
        }, ct);
        await _context.SaveChangesAsync(ct);

        var createdAt = DateTime.UtcNow.AddMilliseconds(123);
        var bookingDto = new BookingRepositoryCreateDto
        {
            EventId = eventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = createdAt,
            ProcessedAt = createdAt.AddMinutes(5)
        };

        var bookingId = await _repo.CreateAsync(bookingDto);

        var saved = await _context.Bookings.FirstAsync(b => b.Id == bookingId, ct);

        Assert.Equal(eventId, saved.EventId);
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
        AssertDateTimeAlmostEqual(createdAt, saved.CreatedAt);
        Assert.NotNull(saved.ProcessedAt);
        AssertDateTimeAlmostEqual(bookingDto.ProcessedAt.Value, saved.ProcessedAt.Value);
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyPendingBookings()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var eventId = Guid.NewGuid();
        await _context.Events.AddAsync(new Event
        {
            Id = eventId,
            Title = "Test Event",
            TotalSeats = 10,
            AvailableSeats = 10,
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(2)
        }, ct);
        await _context.SaveChangesAsync(ct);

        var b1 = CreateTestBooking(eventId, BookingStatus.Pending);
        var b2 = CreateTestBooking(eventId, BookingStatus.Confirmed);
        var b3 = CreateTestBooking(eventId, BookingStatus.Pending);

        await _context.Bookings.AddRangeAsync(b1, b2, b3);
        await _context.SaveChangesAsync(ct);

        var pending = await _repo.GetPendingAsync();
        var pendingList = pending.ToList();

        Assert.Equal(2, pendingList.Count);
        Assert.Contains(pendingList, b => b.Id == b1.Id);
        Assert.Contains(pendingList, b => b.Id == b3.Id);
        Assert.DoesNotContain(pendingList, b => b.Id == b2.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectBooking_OrNull()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var eventId = Guid.NewGuid();
        await _context.Events.AddAsync(new Event
        {
            Id = eventId,
            Title = "Test Event",
            TotalSeats = 10,
            AvailableSeats = 10,
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(2)
        }, ct);
        await _context.SaveChangesAsync(ct);

        var booking = CreateTestBooking(eventId, BookingStatus.Pending);
        await _context.Bookings.AddAsync(booking, ct);
        await _context.SaveChangesAsync(ct);

        var found = await _repo.GetByIdAsync(booking.Id);
        Assert.NotNull(found);
        Assert.Equal(booking.Id, found.Id);
        Assert.Equal(booking.EventId, found.EventId);

        var notFound = await _repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(notFound);
    }

    [Fact]
    public async Task UpdateAsync_CorrectlyUpdatesStatusAndProcessedAt()
    {
        RefreshServices();
        var ct = TestContext.Current.CancellationToken;

        var eventId = Guid.NewGuid();
        await _context.Events.AddAsync(new Event
        {
            Id = eventId,
            Title = "Test Event",
            TotalSeats = 10,
            AvailableSeats = 10,
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(2)
        }, ct);
        await _context.SaveChangesAsync(ct);

        var originalBooking = CreateTestBooking(eventId, BookingStatus.Pending);
        await _context.Bookings.AddAsync(originalBooking, ct);
        await _context.SaveChangesAsync(ct);

        var newStatus = BookingStatus.Confirmed;
        var newProcessedAt = DateTime.UtcNow.AddMinutes(10);

        var updateDto = new BookingRepositoryUpdateDto
        {
            Id = originalBooking.Id,
            Status = newStatus,
            ProcessedAt = newProcessedAt
        };

        var updated = await _repo.UpdateAsync(updateDto);
        Assert.True(updated);

        _context.Entry(originalBooking).State = EntityState.Detached;
        var refreshed = await _context.Bookings.FirstAsync(b => b.Id == originalBooking.Id, ct);
        Assert.Equal(newStatus, refreshed.Status);

        Assert.NotNull(refreshed.ProcessedAt);
        AssertDateTimeAlmostEqual(newProcessedAt, refreshed.ProcessedAt.Value);

        Assert.Equal(originalBooking.EventId, refreshed.EventId);
        AssertDateTimeAlmostEqual(originalBooking.CreatedAt, refreshed.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenNoRowAffected()
    {
        RefreshServices();
        
        var nonExistingId = Guid.NewGuid();

        var updateDto = new BookingRepositoryUpdateDto
        {
            Id = nonExistingId,
            Status = BookingStatus.Confirmed,
            ProcessedAt = DateTime.UtcNow
        };

        var updated = await _repo.UpdateAsync(updateDto);
        Assert.False(updated);
    }
}
