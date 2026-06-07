using CSCourse.DataAccess;
using CSCourse.Interfaces;
using CSCourse.Middlewares;
using CSCourse.Models;
using CSCourse.Repositories;
using CSCourse.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;

public class BookingMemoryServiceTests : BookingServiceTestsBase
{
    
    protected override IBookingService CreateBookingService(IEventService eventService, IBookingRepository bookings)
    {
        return new BookingService(eventService, bookings);
    }
}

public abstract class BookingServiceTestsBase : IAsyncLifetime
{
    private readonly EventService _eventService;
    private readonly IBookingRepository _bookings;
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
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
        context.Database.EnsureCreated();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
    }

    // TODO: Может await ResetDatabaseAsync(); использовать в деструкторе?
    public BookingServiceTestsBase()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
                CreateContext());

        var serviceProvider = services.BuildServiceProvider();
        AppDbContext _context = serviceProvider.GetRequiredService<AppDbContext>();
        _bookings = new BookingRepository(_context); 
        IEventRepository events = new EventRepository(_context); 

        _eventService = new EventService(events);
    }
    protected abstract IBookingService CreateBookingService(IEventService eventService, IBookingRepository bookings);

    [Fact]
    public async Task CreateBookingAsync_ForExistingEvent_ReturnsBookingWithPendingStatus()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        });

        var result = await bookingService.CreateBookingAsync(eventId);

        Assert.NotNull(result);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllHaveUniqueIds()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);

        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        });
        
        var createdBookingIds = new HashSet<Guid>();

        for (int i = 0; i < 10; i++)
        {
            var result = await bookingService.CreateBookingAsync(eventId);

            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.DoesNotContain(result.Id, createdBookingIds);
            createdBookingIds.Add(result.Id);
        }
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsCorrectInformation()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);

        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        });

        var expectedBooking = await bookingService.CreateBookingAsync(eventId);

        var result = await bookingService.GetBookingByIdAsync(expectedBooking.Id);

        Assert.NotNull(result);
        Assert.Equal(expectedBooking.Id, result.Id);
        Assert.Equal(expectedBooking.EventId, result.EventId);
        Assert.Equal(expectedBooking.Status, result.Status);
        Assert.Equal(expectedBooking.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public async Task UpdateProcessedBookingByIdAsync_AfterProcessing_StatusReflectsInGetBooking()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);

        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        });

        var booking = await bookingService.CreateBookingAsync(eventId);

        var processedDto = new BookingProcessedDto
        {
            Status = BookingStatus.Confirmed,
            ProcessedAt = DateTime.UtcNow.AddSeconds(1)
        };

        await bookingService.UpdateProcessedBookingByIdAsync(booking.Id, processedDto);
        var updatedBooking = await bookingService.GetBookingByIdAsync(booking.Id);

        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
        Assert.Equal(processedDto.ProcessedAt, updatedBooking.ProcessedAt);
        Assert.Equal(booking.CreatedAt, updatedBooking.CreatedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExistingBookingId_ReturnsNull()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);
        var nonExistingBookingId = Guid.Empty;

        var result = await bookingService.GetBookingByIdAsync(nonExistingBookingId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProcessedBookingByIdAsync_NonExistingBooking_ReturnsNull()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);
        var nonExistingBookingId = Guid.NewGuid();
        var processedDto = new BookingProcessedDto
        {
            Status = BookingStatus.Rejected,
            ProcessedAt = DateTime.UtcNow
        };

        var result = await bookingService.UpdateProcessedBookingByIdAsync(
            nonExistingBookingId,
            processedDto
        );

        Assert.True(result);
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeatsByOne()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);

        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        });

        var initialEvent = _eventService.GetEventById(eventId);
        Assert.NotNull(initialEvent);
        Assert.Equal(100, initialEvent.AvailableSeats);

        var result = await bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(result);

        var updatedEvent = _eventService.GetEventById(eventId);
        Assert.NotNull(updatedEvent);
        Assert.Equal(99, updatedEvent.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsUntilLimit_AllSucceedWithUniqueIds()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);

        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Вебинар",
            Description = "Онлайн-мероприятие",
            TotalSeats = 5,
            AvailableSeats = 5,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        });

        var createdBookingIds = new HashSet<Guid>();
        var eventState = _eventService.GetEventById(eventId);
        Assert.NotNull(eventState);
        var totalSeats = eventState.TotalSeats;

        for (int i = 0; i < totalSeats; i++)
        {
            var result = await bookingService.CreateBookingAsync(eventId);

            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.DoesNotContain(result.Id, createdBookingIds);
            createdBookingIds.Add(result.Id);

            var updatedEvent = _eventService.GetEventById(eventId);
            Assert.NotNull(updatedEvent);
            Assert.Equal(totalSeats - i - 1, updatedEvent.AvailableSeats);
        }

        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            async () => await bookingService.CreateBookingAsync(eventId)
        );
    }

    [Fact]
    public async Task CreateBookingAsync_AfterSeatsExhausted_ThrowsNoAvailableSeatsException()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);

        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Мастер-класс",
            Description = "Практическое занятие",
            TotalSeats = 1,
            AvailableSeats = 1,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        });
        await bookingService.CreateBookingAsync(eventId);

        var updatedEvent = _eventService.GetEventById(eventId);
        Assert.NotNull(updatedEvent);
        Assert.Equal(0, updatedEvent.AvailableSeats);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            async () => await bookingService.CreateBookingAsync(eventId)
        );
    }

    [Fact]
    public async Task CreateBookingAsync_ForNonExistingEvent_ThrowsNotFoundException()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);
        var nonExistingEventId = Guid.NewGuid();

        await Assert.ThrowsAsync <NotFoundException> (
            async () => await bookingService.CreateBookingAsync(nonExistingEventId)
        );
    }

    [Fact]
    public async Task RejectBooking_ThenReleaseSeats_AllowsNewBooking()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);

        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Семинар",
            Description = "Обучающее мероприятие",
            TotalSeats = 1,
            AvailableSeats = 1,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        });

        var firstBooking = await bookingService.CreateBookingAsync(eventId);
        var eventState = _eventService.GetEventById(eventId);
        Assert.NotNull(eventState);
        Assert.Equal(0, eventState.AvailableSeats);

        var processedDto = new BookingProcessedDto
        {
            Status = BookingStatus.Rejected,
            ProcessedAt = DateTime.UtcNow
        };
        await bookingService.UpdateProcessedBookingByIdAsync(firstBooking.Id, processedDto);
        _eventService.ReleaseSeats(firstBooking.EventId);

        var eventAfterRelease = _eventService.GetEventById(eventId);
        Assert.NotNull(eventAfterRelease);
        Assert.Equal(1, eventAfterRelease.AvailableSeats);

        var secondBooking = await bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(secondBooking);
        Assert.Equal(eventId, secondBooking.EventId);
        Assert.Equal(BookingStatus.Pending, secondBooking.Status);

        var finalEventState = _eventService.GetEventById(eventId);
        Assert.NotNull(finalEventState);
        Assert.Equal(0, finalEventState.AvailableSeats);
    }

    private Guid CreateTestEvent(int totalSeats)
    {
        return _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Тестирование конкурентности",
            Description = "Событие для проверки конкурентных запросов",
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats,
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2)
        });
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_PreventsOverbooking()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);
        var eventId = CreateTestEvent(5);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var result = await bookingService.CreateBookingAsync(eventId);
                    (bool Success, Booking? Booking, Exception? Exception) successResult =
                        (true, result, null);
                    return successResult;
                }
                catch (NoAvailableSeatsException ex)
                {
                    (bool Success, Booking? Booking, Exception? Exception) errorResult =
                        (false, null, ex);
                    return errorResult;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var successfulBookings = results.Where(r => r.Success).ToList();
        var failedBookings = results.Where(r => !r.Success).ToList();

        Assert.Equal(5, successfulBookings.Count);
        Assert.Equal(15, failedBookings.Count);

        var eventState = _eventService.GetEventById(eventId);
        Assert.NotNull(eventState);
        Assert.Equal(0, eventState.AvailableSeats);

        var bookingIds = successfulBookings.Select(r => r.Booking?.Id).ToList();
        Assert.Equal(bookingIds.Count, bookingIds.Distinct().Count());
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllHaveUniqueIds()
    {
        await ResetDatabaseAsync();

        var bookingService = CreateBookingService(_eventService, _bookings);
        var eventId = CreateTestEvent(10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => bookingService.CreateBookingAsync(eventId)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(10, results.Length);

        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);
        }

        var bookingIds = results.Select(r => r.Id).ToList();
        Assert.Equal(bookingIds.Count, bookingIds.Distinct().Count());

        var eventState = _eventService.GetEventById(eventId);
        Assert.NotNull(eventState);
        Assert.Equal(0, eventState.AvailableSeats);
    }
}