using CSCourse.DataAccess;
using CSCourse.Interfaces;
using CSCourse.Middlewares;
using CSCourse.Models;
using CSCourse.Repositories;
using CSCourse.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;
public class BookingServiceIntegrationTests : IAsyncLifetime
{
    private EventService _eventService = null!;
    private BookingService _bookingService = null!;
    private AppDbContext _context = null!;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
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
        context.Database.EnsureCreated();
        return context;
    }
    private void InitializeServices()
    {
        IBookingRepository bookings = new BookingRepository(_context);
        IEventRepository events = new EventRepository(_context);

        _eventService = new EventService(events);
        _bookingService = new BookingService(_eventService, bookings);
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
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
    }
    [Fact]
    public async Task CreateBookingAsync_ForExistingEvent_ReturnsBookingWithPendingStatus()
    {
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime(),
        });

        RefreshServices();
        var result = await _bookingService.CreateBookingAsync(eventId);

        Assert.NotNull(result);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllHaveUniqueIds()
    {
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime(),
        });
        
        var createdBookingIds = new HashSet<Guid>();

        for (int i = 0; i < 10; i++)
        {
            RefreshServices();

            var result = await _bookingService.CreateBookingAsync(eventId);

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
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime(),
        });

        RefreshServices();
        // TODO: Для корректного значения времени, во всех методах можно
        //       возвращать не только GUID, а полный ответ от БД 
        //       (Сейчас от БД возвращается только ID, а остальные поля дозаполняются
        //        уже в методах-обертках, как в данном случае в CreateBookingAsync) 
        var expectedBooking = await _bookingService.CreateBookingAsync(eventId);

        RefreshServices();
        var result = await _bookingService.GetBookingByIdAsync(expectedBooking.Id);

        Assert.NotNull(result);
        Assert.Equal(expectedBooking.Id, result.Id);
        Assert.Equal(expectedBooking.EventId, result.EventId);
        Assert.Equal(expectedBooking.Status, result.Status);
        Assert.True(Math.Abs((expectedBooking.CreatedAt - result.CreatedAt).TotalMilliseconds) < 1);
    }

    [Fact]
    public async Task UpdateProcessedBookingByIdAsync_AfterProcessing_StatusReflectsInGetBooking()
    {
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime(),
        });


        RefreshServices();
        var booking = await _bookingService.CreateBookingAsync(eventId);

        var processedDto = new BookingProcessedDto
        {
            Status = BookingStatus.Confirmed,
            ProcessedAt = DateTime.UtcNow.AddSeconds(1).ToUniversalTime(),
        };

        RefreshServices();
        await _bookingService.UpdateProcessedBookingByIdAsync(booking.Id, processedDto);
        
        RefreshServices();
        var updatedBooking = await _bookingService.GetBookingByIdAsync(booking.Id);

        Assert.NotNull(updatedBooking);
        // TODO: При добалении в БД последняя цифра с погрешностью, из-за чего не проходят тесты
        //       Т.е. логика корректна, но сама delta в 0.000001 не дает пройти тест
        //       Надо думать...
        //       Как вариант - в БД создать менее точное время
        Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
        
        Assert.NotNull(updatedBooking.ProcessedAt);
        DateTime updatedBookingProcessedAt = updatedBooking.ProcessedAt.Value;
        Assert.True(Math.Abs((processedDto.ProcessedAt - updatedBookingProcessedAt).TotalMilliseconds) < 1);
        Assert.True(Math.Abs((booking.CreatedAt - updatedBooking.CreatedAt).TotalMilliseconds) < 1);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExistingBookingId_ReturnsNull()
    {
        var nonExistingBookingId = Guid.Empty;

        var result = await _bookingService.GetBookingByIdAsync(nonExistingBookingId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProcessedBookingByIdAsync_NonExistingBooking_ReturnsNull()
    {
        var nonExistingBookingId = Guid.NewGuid();
        var processedDto = new BookingProcessedDto
        {
            Status = BookingStatus.Rejected,
            ProcessedAt = DateTime.UtcNow
        };

        var result = await _bookingService.UpdateProcessedBookingByIdAsync(
            nonExistingBookingId,
            processedDto
        );

        Assert.False(result);
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeatsByOne()
    {
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime(),
        });

        RefreshServices();
        var initialEvent = _eventService.GetEventById(eventId);
        Assert.NotNull(initialEvent);
        Assert.Equal(100, initialEvent.AvailableSeats);

        RefreshServices();
        var result = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(result);

        RefreshServices();
        var updatedEvent = _eventService.GetEventById(eventId);
        Assert.NotNull(updatedEvent);
        Assert.Equal(99, updatedEvent.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsUntilLimit_AllSucceedWithUniqueIds()
    {
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Вебинар",
            Description = "Онлайн-мероприятие",
            TotalSeats = 5,
            AvailableSeats = 5,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime(),
        });

        var createdBookingIds = new HashSet<Guid>();

        RefreshServices();
        var eventState = _eventService.GetEventById(eventId);
        Assert.NotNull(eventState);
        var totalSeats = eventState.TotalSeats;

        for (int i = 0; i < totalSeats; i++)
        {
            RefreshServices();
            var result = await _bookingService.CreateBookingAsync(eventId);

            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.DoesNotContain(result.Id, createdBookingIds);
            createdBookingIds.Add(result.Id);

            var updatedEvent = _eventService.GetEventById(eventId);
            Assert.NotNull(updatedEvent);
            Assert.Equal(totalSeats - i - 1, updatedEvent.AvailableSeats);
        }

        RefreshServices();
        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            async () => await _bookingService.CreateBookingAsync(eventId)
        );
    }

    [Fact]
    public async Task CreateBookingAsync_AfterSeatsExhausted_ThrowsNoAvailableSeatsException()
    {
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Мастер-класс",
            Description = "Практическое занятие",
            TotalSeats = 1,
            AvailableSeats = 1,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime(),
        });

        RefreshServices();
        await _bookingService.CreateBookingAsync(eventId);

        RefreshServices();
        var updatedEvent = _eventService.GetEventById(eventId);
        Assert.NotNull(updatedEvent);
        Assert.Equal(0, updatedEvent.AvailableSeats);

        RefreshServices();
        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            async () => await _bookingService.CreateBookingAsync(eventId)
        );
    }

    [Fact]
    public async Task CreateBookingAsync_ForNonExistingEvent_ThrowsNotFoundException()
    {
        var nonExistingEventId = Guid.NewGuid();

        await Assert.ThrowsAsync <NotFoundException> (
            async () => await _bookingService.CreateBookingAsync(nonExistingEventId)
        );
    }

    [Fact]
    public async Task RejectBooking_ThenReleaseSeats_AllowsNewBooking()
    {
        var eventId = _eventService.CreateEvent(new Event
        {
            Id = Guid.Empty,
            Title = "Семинар",
            Description = "Обучающее мероприятие",
            TotalSeats = 1,
            AvailableSeats = 1,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime(),
        });

        RefreshServices();
        var firstBooking = await _bookingService.CreateBookingAsync(eventId);
        
        RefreshServices();
        var eventState = _eventService.GetEventById(eventId);
        Assert.NotNull(eventState);
        Assert.Equal(0, eventState.AvailableSeats);

        var processedDto = new BookingProcessedDto
        {
            Status = BookingStatus.Rejected,
            ProcessedAt = DateTime.UtcNow
        };

        RefreshServices();
        await _bookingService.UpdateProcessedBookingByIdAsync(firstBooking.Id, processedDto);
        
        RefreshServices();
        _eventService.ReleaseSeats(firstBooking.EventId);

        RefreshServices();
        var eventAfterRelease = _eventService.GetEventById(eventId);
        Assert.NotNull(eventAfterRelease);
        Assert.Equal(1, eventAfterRelease.AvailableSeats);

        RefreshServices();
        var secondBooking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(secondBooking);
        Assert.Equal(eventId, secondBooking.EventId);
        Assert.Equal(BookingStatus.Pending, secondBooking.Status);

        RefreshServices();
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
            StartAt = DateTime.UtcNow.AddDays(1).ToUniversalTime(),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2).ToUniversalTime(),
        });
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_PreventsOverbooking()
    {
        var eventId = CreateTestEvent(5);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var result = await _bookingService.CreateBookingAsync(eventId);
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

        RefreshServices();
        var eventState = _eventService.GetEventById(eventId);
        Assert.NotNull(eventState);
        Assert.Equal(0, eventState.AvailableSeats);

        var bookingIds = successfulBookings.Select(r => r.Booking?.Id).ToList();
        Assert.Equal(bookingIds.Count, bookingIds.Distinct().Count());
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllHaveUniqueIds()
    {   
        var eventId = CreateTestEvent(10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(eventId)))
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
        
        RefreshServices();
        var eventState = _eventService.GetEventById(eventId);
        Assert.NotNull(eventState);
        Assert.Equal(0, eventState.AvailableSeats);
    }
}