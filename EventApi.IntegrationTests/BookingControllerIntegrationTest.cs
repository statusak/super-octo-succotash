using CSCourse.Controllers;
using CSCourse.DataAccess;
using CSCourse.Interfaces;
using CSCourse.Models;
using CSCourse.Repositories;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;
public class BookingControllerIntegrationTest : IAsyncLifetime
{
    const int _backgroundServiceProcessingDelaySec = 2;

    private IEventService _eventService = null!;
    private EventsController _eventsController = null!;
    private BookingsController _bookingsController = null!;
    private BookingBackgroundService _backgroundService = null!;
    private IServiceProvider _serviceProvider = null!;
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        InitializeServices();
        await ResetDatabaseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext CreateContext()
    {
        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();

    }
    private void InitializeServices()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        _eventService = _serviceProvider.GetRequiredService<IEventService>();
        var bookingService = _serviceProvider.GetRequiredService<IBookingService>();

        var logger = NullLogger<EventsController>.Instance;
        _eventsController = new EventsController(_eventService, bookingService, logger);
        _bookingsController = new BookingsController(bookingService);

        var backgroundLogger = NullLogger<BookingBackgroundService>.Instance;
        _backgroundService = new BookingBackgroundService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            backgroundLogger,
            TimeSpan.FromSeconds(_backgroundServiceProcessingDelaySec)
        );
}


    private void RefreshServices()
    {
        InitializeServices();
    }


    private async Task ResetDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
    }

    [Fact]
    public async Task BookingController_CreateBooking_Success()
    {
        var validDto = new EventCreateDto
        {
            Title = "Тестовая конференция",
            Description = "Описание мероприятия",
            TotalSeats = 100,
            StartAt = DateTime.Now.AddHours(1).ToUniversalTime(),
            EndAt = DateTime.Now.AddHours(2).ToUniversalTime()
        };

        var actionResult = await _eventsController.Post(validDto);
        var resultCreateEvent = actionResult.Result as CreatedAtActionResult;

        Assert.NotNull(resultCreateEvent);
        Assert.Equal(201, resultCreateEvent.StatusCode);

        var @event = resultCreateEvent.Value as Event;
        Assert.NotNull(@event);

        RefreshServices();
        var resultCreateBooking = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;

        Assert.NotNull(resultCreateBooking);
        Assert.Equal(202, resultCreateBooking.StatusCode);

        var booking = resultCreateBooking.Value as BookingResponseDto;
        Assert.NotNull(booking);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(@event.Id, booking.EventId);
    }

    [Fact]
    public async Task BookingController_CreateMultiplyBooking_Success()
    {
        var validDto = new EventCreateDto
        {
            Title = "Тестовая конференция",
            Description = "Описание мероприятия",
            TotalSeats = 100,
            StartAt = DateTime.Now.AddHours(1).ToUniversalTime(),
            EndAt = DateTime.Now.AddHours(2).ToUniversalTime()
        };

        var resultCreateEvent = (await _eventsController.Post(validDto)).Result as CreatedAtActionResult;

        Assert.NotNull(resultCreateEvent);
        Assert.Equal(201, resultCreateEvent.StatusCode);

        var @event = resultCreateEvent.Value as Event;
        Assert.NotNull(@event);

        List<Guid> CreatedBookings = [];

        for (int i = 0; i < 10; i++)
        {
            RefreshServices();
            var resultCreateBooking = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;

            Assert.NotNull(resultCreateBooking);
            Assert.Equal(202, resultCreateBooking.StatusCode);

            var booking = resultCreateBooking.Value as BookingResponseDto;
            Assert.NotNull(booking);
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.Equal(@event.Id, booking.EventId);
            Assert.DoesNotContain(booking.Id, CreatedBookings);
            CreatedBookings.Add(booking.Id);
        }
    }

    [Fact]
    public async Task BookingController_CheckInfoBooking_Success()
    {
        var validDto = new EventCreateDto
        {
            Title = "Тестовая конференция",
            Description = "Описание мероприятия",
            TotalSeats = 100,
            StartAt = DateTime.Now.AddHours(1).ToUniversalTime(),
            EndAt = DateTime.Now.AddHours(2).ToUniversalTime()
        };

        var resultCreateEvent = (await _eventsController.Post(validDto)).Result as CreatedAtActionResult;

        Assert.NotNull(resultCreateEvent);
        Assert.Equal(201, resultCreateEvent.StatusCode);

        var @event = resultCreateEvent.Value as Event;
        Assert.NotNull(@event);

        RefreshServices();
        var resultCreateBooking = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;

        Assert.NotNull(resultCreateBooking);
        Assert.Equal(202, resultCreateBooking.StatusCode);

        var bookingCreate = resultCreateBooking.Value as BookingResponseDto;
        Assert.NotNull(bookingCreate);
        Assert.Equal(BookingStatus.Pending, bookingCreate.Status);
        Assert.Equal(@event.Id, bookingCreate.EventId);

        RefreshServices();
        var resultInfoBooking = (await _bookingsController.GetById(bookingCreate.Id)) as OkObjectResult;

        Assert.NotNull(resultInfoBooking);
        Assert.Equal(200, resultInfoBooking.StatusCode);

        var bookingInfo = resultInfoBooking.Value as BookingResponseDto;
        Assert.NotNull(bookingInfo);
        Assert.Equal(bookingCreate.Id, bookingInfo.Id);
        Assert.Equal(bookingCreate.EventId, bookingInfo.EventId);
        Assert.Equal(bookingCreate.Status, bookingInfo.Status);
        // WARN: Особенность в сравнении времени
        Assert.True(Math.Abs((bookingCreate.CreatedAt - bookingInfo.CreatedAt).TotalMilliseconds) < 1);
    }

    [Fact]
    public async Task BookingController_CheckInfoAfterProcessingBooking_Success()
    {
        var validDto = new EventCreateDto
        {
            Title = "Тестовая конференция",
            Description = "Описание мероприятия",
            TotalSeats = 100,
            StartAt = DateTime.Now.AddHours(1).ToUniversalTime(),
            EndAt = DateTime.Now.AddHours(2).ToUniversalTime()
        };

        var resultCreateEvent = (await _eventsController.Post(validDto)).Result as CreatedAtActionResult;

        Assert.NotNull(resultCreateEvent);
        Assert.Equal(201, resultCreateEvent.StatusCode);

        var @event = resultCreateEvent.Value as Event;
        Assert.NotNull(@event);
        
        RefreshServices();
        var resultCreateBooking = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;

        Assert.NotNull(resultCreateBooking);
        Assert.Equal(202, resultCreateBooking.StatusCode);

        var bookingCreate = resultCreateBooking.Value as BookingResponseDto;
        Assert.NotNull(bookingCreate);
        Assert.Equal(BookingStatus.Pending, bookingCreate.Status);
        Assert.Equal(@event.Id, bookingCreate.EventId);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        // TODO: это корректно???
        RefreshServices();
        await _backgroundService.StartAsync(cts.Token);
        await Task.Delay(5000, TestContext.Current.CancellationToken);
        await _backgroundService.StopAsync(cts.Token);


        // Принудительно обновляем состояние брони в контексте контроллера
        // var bookingEntity = _context.Bookings.Find(bookingCreate.Id);
        // Assert.NotNull(bookingEntity);
        // _context.Entry(bookingEntity).Reload();
        RefreshServices();

        var resultInfoBooking = (await _bookingsController.GetById(bookingCreate.Id)) as OkObjectResult;

        Assert.NotNull(resultInfoBooking);
        Assert.Equal(200, resultInfoBooking.StatusCode);

        var bookingInfo = resultInfoBooking.Value as BookingResponseDto;
        Assert.NotNull(bookingInfo);
        Assert.Equal(BookingStatus.Confirmed, bookingInfo.Status);
        Assert.Equal(@event.Id, bookingCreate.EventId);
        // WARN: Особенность в сравнении времени
        Assert.True(Math.Abs((bookingCreate.CreatedAt - bookingInfo.CreatedAt).TotalMilliseconds) < 1);
        Assert.True(bookingInfo.ProcessedAt >= bookingCreate.CreatedAt);
    }

    [Fact]
    public async Task BookingController_CreateBookingForNotExistsEvent_ReturnsNotFound()
    {
        var actionResult = (await _eventsController.CreateBooking(Guid.Empty)) as NotFoundObjectResult;

        Assert.NotNull(actionResult);
        Assert.Equal(404, actionResult.StatusCode);

        Assert.NotNull(actionResult.Value);
        Assert.Contains($"Event with index {Guid.Empty} not found", actionResult.Value.ToString());
    }

    [Fact]
    public async Task BookingController_CreateBookingForDeletedEvent_ReturnsNotFound()
    {
        var validDto = new EventCreateDto
        {
            Title = "Тестовая конференция",
            Description = "Описание мероприятия",
            TotalSeats = 100,
            StartAt = DateTime.Now.AddHours(1).ToUniversalTime(),
            EndAt = DateTime.Now.AddHours(2).ToUniversalTime()
        };

        var resultCreateEvent = (await _eventsController.Post(validDto)).Result as CreatedAtActionResult;

        Assert.NotNull(resultCreateEvent);
        Assert.Equal(201, resultCreateEvent.StatusCode);

        var @event = resultCreateEvent.Value as Event;
        Assert.NotNull(@event);

        RefreshServices();
        var actionResult = (await _eventsController.Delete(@event.Id)) as OkResult;

        Assert.NotNull(actionResult);
        Assert.Equal(200, actionResult.StatusCode);

        RefreshServices();
        var allEvents = _eventService.GetAll(1, int.MaxValue).Events;
        Assert.Empty(allEvents);

        RefreshServices();
        var actionResultCreateBooking = (await _eventsController.CreateBooking(Guid.Empty)) as NotFoundObjectResult;

        Assert.NotNull(actionResultCreateBooking);
        Assert.Equal(404, actionResultCreateBooking.StatusCode);

        Assert.NotNull(actionResultCreateBooking.Value);
        Assert.Contains($"Event with index {Guid.Empty} not found", actionResultCreateBooking.Value.ToString());
    }

    [Fact]
    public async Task BookingController_CheckInfoDontExistsBooking_ReturnsNotFound()
    {
        var actionResult = (await _bookingsController.GetById(Guid.Empty)) as NotFoundObjectResult;

        Assert.NotNull(actionResult);
        Assert.Equal(404, actionResult.StatusCode);

        Assert.NotNull(actionResult.Value);
        Assert.Contains($"Booking with index {Guid.Empty} not found", actionResult.Value.ToString());
    }
}
