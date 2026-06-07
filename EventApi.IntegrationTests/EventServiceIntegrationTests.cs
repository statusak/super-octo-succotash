using CSCourse.Controllers;
using CSCourse.DataAccess;
using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using CSCourse.Repositories;

namespace EventApi.IntegrationTests;
public class EventServiceIntegrationTests : IAsyncLifetime
{
    private readonly EventService _eventService;
    private readonly EventsController _controller;
    private readonly AppDbContext _context;

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

    public EventServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
                CreateContext());

        var serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<AppDbContext>();
        IBookingRepository bookings = new BookingRepository(_context); 
        IEventRepository events = new EventRepository(_context); 

        _eventService = new EventService(events);
        var bookingService = new BookingService(_eventService, bookings);
        var logger = NullLogger<EventsController>.Instance;
        _controller = new EventsController(_eventService, bookingService, logger);
    }

    [Fact]
    public async Task GetAll_WithFilter_ReturnsFilteredResults()
    {
        await ResetDatabaseAsync();

        var allEvents = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "КоНфЕрЕнЦиЯ разработчиков",
                Description = "Ежегодная конференция...",
                TotalSeats = 100,
                AvailableSeats = 100,
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(8)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Встреча команды",
                Description = "Планерка",
                TotalSeats = 100,
                AvailableSeats = 100,
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(1).AddHours(2)
            }
        };

        foreach (var @event in allEvents)
        {
            _eventService.CreateEvent(@event);
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
        await ResetDatabaseAsync();

        var originalEvent = new Event
        {
            Id = Guid.Empty,
            Title = "Конференция разработчиков",
            Description = "Ежегодная конференция...",
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
        };

        Guid id = _eventService.CreateEvent(originalEvent);

        var updateDto = new EventUpdateDto
        {
            Title = "Обновлённая конференция",
            Description = "Описание после обновления",
            StartAt = new DateTime(2026, 12, 2, 9, 0, 0),
            EndAt = new DateTime(2026, 12, 2, 17, 0, 0)
        };

        var actionResult = (await _controller.Put(id, updateDto)) as NotFoundResult;

        Assert.NotNull(actionResult);
        Assert.Equal(204, actionResult.StatusCode);

        var updatedEvent = _eventService.GetEventById(id);
        Assert.Equal(updateDto.Title, updatedEvent?.Title);
        Assert.Equal(updateDto.Description, updatedEvent?.Description);
        Assert.Equal(originalEvent.TotalSeats, updatedEvent?.TotalSeats);
        Assert.Equal(originalEvent.AvailableSeats, updatedEvent?.AvailableSeats);
        Assert.Equal(updateDto.StartAt, updatedEvent?.StartAt);
        Assert.Equal(updateDto.EndAt, updatedEvent?.EndAt);
    }
}
