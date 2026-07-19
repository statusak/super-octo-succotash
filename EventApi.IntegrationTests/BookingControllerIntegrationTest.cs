using CSCourse.Domain.Models;
using CSCourse.Application.Interfaces;
using CSCourse.Application.Models;
using CSCourse.Application.Services;
using CSCourse.Infrastructure.Repositories;
using CSCourse.Infrastructure.DataAccess;
using CSCourse.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using CSCourse.Infrastructure.Models;
using Microsoft.Extensions.Options;
using CSCourse.Infrastructure.Services;

namespace EventApi.IntegrationTests;
public class BookingControllerIntegrationTest : IAsyncLifetime
{
    const int _backgroundServiceProcessingDelaySec = 2;

    private IEventService _eventService = null!;
    private EventsController _eventsController = null!;
    private BookingsController _bookingsController = null!;
    private BookingBackgroundService _backgroundService = null!;
    private IAccountService _accountService = null!;
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


        JwtSettings jwtSettings = new JwtSettings
        {
            Secret = "1234567890123456789012",
            Issuer = "https://example.com",
            Audience = "https://example.com",
            ExpirationMinutes = 10,
        };

        services.AddSingleton(jwtSettings);     
        services.AddSingleton<IOptions<JwtSettings>>(
            Options.Create(jwtSettings)
        );

        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IAccountService, AccountService>();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.Migrate();

        _eventService = _serviceProvider.GetRequiredService<IEventService>();
        var bookingService = _serviceProvider.GetRequiredService<IBookingService>();

        _accountService = _serviceProvider.GetRequiredService<IAccountService>();

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
            "TRUNCATE TABLE events, bookings, accounts RESTART IDENTITY CASCADE");
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

        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();

        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);  

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "LoginTest");

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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

        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();

        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);  

        for (int i = 0; i < 10; i++)
        {
            RefreshServices();
             var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "LoginTest");

            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _eventsController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
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

        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();

        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);  

         var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "LoginTest");

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var resultCreateBooking = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;

        Assert.NotNull(resultCreateBooking);
        Assert.Equal(202, resultCreateBooking.StatusCode);

        var bookingCreate = resultCreateBooking.Value as BookingResponseDto;
        Assert.NotNull(bookingCreate);
        Assert.Equal(BookingStatus.Pending, bookingCreate.Status);
        Assert.Equal(@event.Id, bookingCreate.EventId);

        RefreshServices();

        _bookingsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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
        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();

        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);  

         var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "LoginTest");

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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

        _bookingsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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
        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();

        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);  

         var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "LoginTest");

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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
        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();

        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);  

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "LoginTest");

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var actionResultCreateBooking = (await _eventsController.CreateBooking(Guid.Empty)) as NotFoundObjectResult;

        Assert.NotNull(actionResultCreateBooking);
        Assert.Equal(404, actionResultCreateBooking.StatusCode);

        Assert.NotNull(actionResultCreateBooking.Value);
        Assert.Contains($"Event with index {Guid.Empty} not found", actionResultCreateBooking.Value.ToString());
    }

    [Fact]
    public async Task BookingController_CheckInfoDontExistsBooking_ReturnsNotFound()
    {
        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();

        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);  

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "LoginTest");

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _bookingsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var actionResult = (await _bookingsController.GetById(Guid.Empty)) as NotFoundObjectResult;

        Assert.NotNull(actionResult);
        Assert.Equal(404, actionResult.StatusCode);

        Assert.NotNull(actionResult.Value);
        Assert.Contains($"Booking with index {Guid.Empty} not found", actionResult.Value.ToString());
    }

    [Fact]
    public async Task BookingController_CreateBookingForPastEvent_ReturnsConflict()
    {
        // Arrange
        var pastEventDto = new EventCreateDto
        {
            Title = "Прошедшее мероприятие",
            Description = "Уже закончилось",
            TotalSeats = 100,
            StartAt = DateTime.UtcNow.AddDays(-2),
            EndAt = DateTime.UtcNow.AddHours(-1)
        };

        var resultCreateEvent = (await _eventsController.Post(pastEventDto)).Result as CreatedAtActionResult;
        Assert.NotNull(resultCreateEvent);
        Assert.Equal(201, resultCreateEvent.StatusCode);

        var @event = resultCreateEvent.Value as Event;
        Assert.NotNull(@event);

        RefreshServices();
        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();
        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "LoginTest");

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var actionResult = (await _eventsController.CreateBooking(@event.Id)) as ObjectResult;

        // Assert
        Assert.NotNull(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, actionResult.StatusCode);

        Assert.NotNull(actionResult.Value);
        Assert.Contains($"cannot reserve seats after start event: {@event.Id}", actionResult.Value.ToString());
    }

    [Fact]
    public async Task BookingController_CreateBooking_LimitExceeded_ReturnsConflict()
    {
        // Arrange
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
        await _accountService.Register(new AccountRegisterDto
        {
            Login = "LoginTest",
            Password = "PasswordTest",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();
        Guid userId = await context.Accounts
            .Where(a => a.Login == "LoginTest")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "LoginTest");

        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        const int limit = 10;

        for (int i = 0; i < limit; i++)
        {
            _eventsController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            var bookingResult = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;
            Assert.NotNull(bookingResult);
            Assert.Equal(202, bookingResult.StatusCode);
            RefreshServices();
        }

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var overLimitResult = (await _eventsController.CreateBooking(@event.Id)) as ObjectResult;

        // Assert
        Assert.NotNull(overLimitResult);
        Assert.Equal(StatusCodes.Status409Conflict, overLimitResult.StatusCode);

        Assert.NotNull(overLimitResult.Value);
        Assert.Contains($"Get limit booking for user on event: {@event.Id}", overLimitResult.Value.ToString());

        var bookingsCount = await context.Bookings
            .CountAsync(b => b.EventId == @event.Id && b.UserId == userId,
                TestContext.Current.CancellationToken);
        Assert.Equal(limit, bookingsCount);
    }

    [Fact]
    public async Task BookingController_CreateBooking_LimitsArePerUser_NotGlobal()
    {
        var validDto = new EventCreateDto
        {
            Title = "Тестовая конференция",
            Description = "Описание мероприятия",
            TotalSeats = 200,
            StartAt = DateTime.Now.AddHours(1).ToUniversalTime(),
            EndAt = DateTime.Now.AddHours(2).ToUniversalTime()
        };

        var resultCreateEvent = (await _eventsController.Post(validDto)).Result as CreatedAtActionResult;
        Assert.NotNull(resultCreateEvent);
        Assert.Equal(StatusCodes.Status201Created, resultCreateEvent.StatusCode);

        var @event = resultCreateEvent.Value as Event;
        Assert.NotNull(@event);

        RefreshServices();

        // Регистрируем первого пользователя
        await _accountService.Register(new AccountRegisterDto
        {
            Login = "UserA",
            Password = "PasswordA",
            Role = AccountRole.User
        });
        RefreshServices();

        var context = _serviceProvider.GetRequiredService<AppDbContext>();
        Guid userAId = await context.Accounts
            .Where(a => a.Login == "UserA")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        // Регистрируем второго пользователя
        await _accountService.Register(new AccountRegisterDto
        {
            Login = "UserB",
            Password = "PasswordB",
            Role = AccountRole.User
        });
        RefreshServices();

        Guid userBId = await context.Accounts
            .Where(a => a.Login == "UserB")
            .Select(a => a.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        const int limit = 10;

        async Task CreateBookingsForUser(Guid userId, string login)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, login);

            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            for (int i = 0; i < limit; i++)
            {
                _eventsController.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };
                var result = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;
                Assert.NotNull(result);
                Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
                RefreshServices();
            }
        }

        await CreateBookingsForUser(userAId, "UserA");
        await CreateBookingsForUser(userBId, "UserB");

        var countA = await context.Bookings
            .CountAsync(b => b.EventId == @event.Id && b.UserId == userAId,
                TestContext.Current.CancellationToken);
        var countB = await context.Bookings
            .CountAsync(b => b.EventId == @event.Id && b.UserId == userBId,
                TestContext.Current.CancellationToken);

        Assert.Equal(limit, countA);
        Assert.Equal(limit, countB);

        var identityA = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userAId.ToString())
        }, "UserA");

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identityA) }
        };

        var overLimitA = (await _eventsController.CreateBooking(@event.Id)) as ObjectResult;
        Assert.NotNull(overLimitA);
        Assert.Equal(StatusCodes.Status409Conflict, overLimitA.StatusCode);

        var identityB = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userBId.ToString())
        }, "UserB");

        _eventsController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identityB) }
        };

        var overLimitB = (await _eventsController.CreateBooking(@event.Id)) as ObjectResult;
        Assert.NotNull(overLimitB);
        Assert.Equal(StatusCodes.Status409Conflict, overLimitB.StatusCode);

        countA = await context.Bookings
            .CountAsync(b => b.EventId == @event.Id && b.UserId == userAId,
                TestContext.Current.CancellationToken);
        countB = await context.Bookings
            .CountAsync(b => b.EventId == @event.Id && b.UserId == userBId,
                TestContext.Current.CancellationToken);

        Assert.Equal(limit, countA);
        Assert.Equal(limit, countB);
    }
}
