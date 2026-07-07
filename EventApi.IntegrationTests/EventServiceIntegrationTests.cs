using CSCourse.Controllers;
using CSCourse.DataAccess;
using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using CSCourse.Repositories;
using CSCourse.Interfaces;

namespace EventApi.IntegrationTests;
public class EventServiceIntegrationTests : IAsyncLifetime
{
    private EventService _eventService = null!;
    private EventsController _controller = null!;
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

        context.Database.Migrate();

        return context;
    }
    private void InitializeServices()
    {
        IBookingRepository bookings = new BookingRepository(_context);
        IEventRepository events = new EventRepository(_context);

        _eventService = new EventService(events);
        var bookingService = new BookingService(_eventService, bookings);
        var logger = NullLogger<EventsController>.Instance;
        _controller = new EventsController(_eventService, bookingService, logger);
    }

    private void RefreshServices()
    {
        _context = CreateContext();
        InitializeServices();
    }


    private async Task ResetDatabaseAsync()
    {
        // TODO: Если в ...Configuration.cs имя таблицы задается с большой буквы, 
        //       то через этот запрос их тут не отчистить, даже если указать Events и Bookings
        //       с большой буквы. Не знаю как исправить
        //       Пока при создании переименовал создание таблиц с маленькой буквы
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
    }

    [Fact]
    public async Task GetAll_WithFilter_ReturnsFilteredResults()
    {
        var allEvents = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "КоНфЕрЕнЦиЯ разработчиков",
                Description = "Ежегодная конференция...",
                TotalSeats = 100,
                AvailableSeats = 100,
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(8)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Встреча команды",
                Description = "Планерка",
                TotalSeats = 100,
                AvailableSeats = 100,
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2)
            }
        };

        foreach (var @event in allEvents)
        {
            _eventService.CreateEvent(@event);
            RefreshServices();
        }

        var filterDto = new FilterEventDto
        {
            Title = "кОнФеРеНцИЯ"
        };

        var actionResult = (await _controller.GetAll(filterDto, 1, 10)).Result as OkObjectResult;
        var actualResult = actionResult?.Value as PaginatedResult;

        Assert.NotNull(actionResult);
        Assert.Equal(200, actionResult.StatusCode);
        Assert.NotNull(actualResult);
        Assert.Single(actualResult.Events);
        Assert.Equal("КоНфЕрЕнЦиЯ разработчиков", actualResult.Events[0].Title);
    }

    [Fact]
    public async Task Put_UpdateExistingEvent_ReturnsNoContent()
    {
        var originalEvent = new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0).ToUniversalTime()
        };

        Guid id = _eventService.CreateEvent(originalEvent);

        var updateDto = new EventUpdateDto
        {
            Title = "Обновлённая конференция",
            Description = "Описание после обновления",
            StartAt = new DateTime(2026, 12, 2, 9, 0, 0).ToUniversalTime(),
            EndAt = new DateTime(2026, 12, 2, 17, 0, 0).ToUniversalTime()
        };

        RefreshServices();

        var actionResult = (await _controller.Put(id, updateDto)) as NoContentResult;

        Assert.NotNull(actionResult);
        Assert.Equal(204, actionResult.StatusCode);

        RefreshServices();

        var updatedEvent = _eventService.GetEventById(id);
        Assert.Equal(updateDto.Title, updatedEvent?.Title);
        Assert.Equal(updateDto.Description, updatedEvent?.Description);
        Assert.Equal(originalEvent.TotalSeats, updatedEvent?.TotalSeats);
        Assert.Equal(originalEvent.AvailableSeats, updatedEvent?.AvailableSeats);
        Assert.Equal(updateDto.StartAt, updatedEvent?.StartAt);
        Assert.Equal(updateDto.EndAt, updatedEvent?.EndAt);
    }
}
