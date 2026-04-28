using CSCourse.Controllers;
using CSCourse.Interfaces;
using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventServiceTest
{
    public class UnitBookingServiceTest
    {
        private readonly EventMemoryService _eventService;
        private readonly EventsController _eventsController;
        private readonly BookingsController _bookingsController;
        private readonly BookingBackgroundService _backgroundService;

        public UnitBookingServiceTest()
        {
            _eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var bookingTaskQueue = new InMemoryBookingTaskQueue();
            var logger = NullLogger<EventsController>.Instance;
            _eventsController = new EventsController(_eventService, bookingService, bookingTaskQueue, logger);
            _bookingsController = new BookingsController(bookingService);

            var backgroundLogger = NullLogger<BookingBackgroundService>.Instance;
            _backgroundService = new BookingBackgroundService(
                bookingService,
                bookingTaskQueue,
                backgroundLogger
            );
        }

        [Fact]
        public async Task BookingService_CreateBooking_Success()
        {
            var validDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var resultCreateEvent = _eventsController.Post(validDto).Result as CreatedAtActionResult;
            
            Assert.NotNull(resultCreateEvent);
            Assert.Equal(201, resultCreateEvent.StatusCode);

            var @event = resultCreateEvent.Value as Event;
            Assert.NotNull(@event);

            var resultCreateBooking = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;

            Assert.NotNull(resultCreateBooking);
            Assert.Equal(202, resultCreateBooking.StatusCode);

            var booking = resultCreateBooking.Value as BookingResponseDto;
            Assert.NotNull(booking);
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.Equal(@event.Id, booking.EventId);
        }

        [Fact]
        public async Task BookingService_CreateMultiplyBooking_Success()
        {
            var validDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var resultCreateEvent = _eventsController.Post(validDto).Result as CreatedAtActionResult;

            Assert.NotNull(resultCreateEvent);
            Assert.Equal(201, resultCreateEvent.StatusCode);

            var @event = resultCreateEvent.Value as Event;
            Assert.NotNull(@event);

            List<Guid> CreatedBookings = [];

            for (int i = 0; i < 10; i++)
            {
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
        public async Task BookingService_CheckInfoBooking_Success()
        {
            var validDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var resultCreateEvent = _eventsController.Post(validDto).Result as CreatedAtActionResult;

            Assert.NotNull(resultCreateEvent);
            Assert.Equal(201, resultCreateEvent.StatusCode);

            var @event = resultCreateEvent.Value as Event;
            Assert.NotNull(@event);

            var resultCreateBooking = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;

            Assert.NotNull(resultCreateBooking);
            Assert.Equal(202, resultCreateBooking.StatusCode);

            var bookingCreate = resultCreateBooking.Value as BookingResponseDto;
            Assert.NotNull(bookingCreate);
            Assert.Equal(BookingStatus.Pending, bookingCreate.Status);
            Assert.Equal(@event.Id, bookingCreate.EventId);

            var resultInfoBooking = (await _bookingsController.GetById(bookingCreate.Id)) as OkObjectResult;

            Assert.NotNull(resultInfoBooking);
            Assert.Equal(200, resultInfoBooking.StatusCode);

            var bookingInfo = resultInfoBooking.Value as BookingResponseDto;
            Assert.NotNull(bookingInfo);
            Assert.Equal(bookingCreate.Id, bookingInfo.Id);
            Assert.Equal(bookingCreate.EventId, bookingInfo.EventId);
            Assert.Equal(bookingCreate.Status, bookingInfo.Status);
            Assert.Equal(bookingCreate.CreatedAt, bookingInfo.CreatedAt);
        }

        [Fact]
        public async Task BookingService_CheckInfoAfterProcessingBooking_Success()
        {
            var validDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var resultCreateEvent = _eventsController.Post(validDto).Result as CreatedAtActionResult;

            Assert.NotNull(resultCreateEvent);
            Assert.Equal(201, resultCreateEvent.StatusCode);

            var @event = resultCreateEvent.Value as Event;
            Assert.NotNull(@event);

            var resultCreateBooking = (await _eventsController.CreateBooking(@event.Id)) as AcceptedAtActionResult;

            Assert.NotNull(resultCreateBooking);
            Assert.Equal(202, resultCreateBooking.StatusCode);

            var bookingCreate = resultCreateBooking.Value as BookingResponseDto;
            Assert.NotNull(bookingCreate);
            Assert.Equal(BookingStatus.Pending, bookingCreate.Status);
            Assert.Equal(@event.Id, bookingCreate.EventId);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _backgroundService.StartAsync(cts.Token);
            await Task.Delay(3000, TestContext.Current.CancellationToken);
            await _backgroundService.StopAsync(cts.Token);

            var resultInfoBooking = (await _bookingsController.GetById(bookingCreate.Id)) as OkObjectResult;

            Assert.NotNull(resultInfoBooking);
            Assert.Equal(200, resultInfoBooking.StatusCode);

            var bookingInfo = resultInfoBooking.Value as BookingResponseDto;
            Assert.NotNull(bookingInfo);
            Assert.Equal(BookingStatus.Confirmed, bookingInfo.Status);
            Assert.Equal(@event.Id, bookingCreate.EventId);
            Assert.Equal(bookingCreate.CreatedAt, bookingInfo.CreatedAt);
            Assert.True(bookingInfo.ProcessedAt >= bookingCreate.CreatedAt);
        }

        [Fact]
        public async Task BookingService_CreateBookingForNotExistsEvent_ReturnsNotFound()
        {
            var actionResult = (await _eventsController.CreateBooking(Guid.Empty)) as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains($"Event with index {Guid.Empty} not found", actionResult.Value.ToString());
        }

        [Fact]
        public async Task BookingService_CreateBookingForDeletedEvent_ReturnsNotFound()
        {
            var validDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var resultCreateEvent = _eventsController.Post(validDto).Result as CreatedAtActionResult;

            Assert.NotNull(resultCreateEvent);
            Assert.Equal(201, resultCreateEvent.StatusCode);

            var @event = resultCreateEvent.Value as Event;
            Assert.NotNull(@event);

            var actionResult = _eventsController.Delete(@event.Id) as OkResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);

            var allEvents = _eventService.GetAll(1, int.MaxValue).Events;
            Assert.Empty(allEvents);

            var actionResultCreateBooking = (await _eventsController.CreateBooking(Guid.Empty)) as NotFoundObjectResult;

            Assert.NotNull(actionResultCreateBooking);
            Assert.Equal(404, actionResultCreateBooking.StatusCode);

            Assert.NotNull(actionResultCreateBooking.Value);
            Assert.Contains($"Event with index {Guid.Empty} not found", actionResultCreateBooking.Value.ToString());
        }

        [Fact]
        public async Task BookingService_CheckInfoDontExistsBooking_ReturnsNotFound()
        {
            var actionResult = (await _bookingsController.GetById(Guid.Empty)) as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains($"Booking with index {Guid.Empty} not found", actionResult.Value.ToString());
        }
    }
}
