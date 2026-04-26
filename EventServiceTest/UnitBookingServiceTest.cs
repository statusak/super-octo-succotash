using CSCourse.Controllers;
using CSCourse.Interfaces;
using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

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

            var booking = resultCreateBooking.Value as Booking;
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

                var booking = resultCreateBooking.Value as Booking;
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

            var bookingCreate = resultCreateBooking.Value as Booking;
            Assert.NotNull(bookingCreate);
            Assert.Equal(BookingStatus.Pending, bookingCreate.Status);
            Assert.Equal(@event.Id, bookingCreate.EventId);

            var resultInfoBooking = (await _bookingsController.GetById(@event.Id)) as AcceptedAtActionResult;

            Assert.NotNull(resultCreateBooking);
            Assert.Equal(202, resultCreateBooking.StatusCode);

            var bookingInfo = resultCreateBooking.Value as Booking;
            Assert.NotNull(bookingInfo);
            Assert.Equal(bookingCreate, bookingInfo);
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

            var bookingCreate = resultCreateBooking.Value as Booking;
            Assert.NotNull(bookingCreate);
            Assert.Equal(BookingStatus.Pending, bookingCreate.Status);
            Assert.Equal(@event.Id, bookingCreate.EventId);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _backgroundService.StartAsync(cts.Token);
            await Task.Delay(3000, TestContext.Current.CancellationToken);
            await _backgroundService.StopAsync(cts.Token);

            var resultInfoBooking = (await _bookingsController.GetById(@event.Id)) as AcceptedAtActionResult;

            Assert.NotNull(resultCreateBooking);
            Assert.Equal(202, resultCreateBooking.StatusCode);

            var bookingInfo = resultCreateBooking.Value as Booking;
            Assert.NotNull(bookingInfo);
            Assert.Equal(BookingStatus.Confirmed, bookingInfo.Status);
            Assert.Equal(@event.Id, bookingCreate.EventId);
            Assert.Equal(bookingCreate.CreatedAt, bookingInfo.CreatedAt);
            Assert.True(bookingInfo.ProcessedAt < bookingCreate.CreatedAt);
        }
    }
}
