using CSCourse.Interfaces;
using CSCourse.Models;
using CSCourse.Services;

namespace EventServiceTest
{
    public class BookingMemoryServiceTests : BookingServiceTestsBase
    {
        
        protected override IBookingService CreateBookingService(IEventService eventService)
        {
            return new BookingMemoryService(eventService);
        }
    }

    public abstract class BookingServiceTestsBase
    {
        private readonly EventMemoryService _eventService;
        public BookingServiceTestsBase()
        {
            _eventService = new EventMemoryService();
        }
        protected abstract IBookingService CreateBookingService(IEventService eventService);

        [Fact]
        public async Task CreateBookingAsync_ForExistingEvent_ReturnsBookingWithPendingStatus()
        {
            var bookingService = CreateBookingService(_eventService);
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
            var bookingService = CreateBookingService(_eventService);

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
            var bookingService = CreateBookingService(_eventService);

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
            var bookingService = CreateBookingService(_eventService);

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
            var bookingService = CreateBookingService(_eventService);
            var nonExistingBookingId = Guid.Empty;

            var result = await bookingService.GetBookingByIdAsync(nonExistingBookingId);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProcessedBookingByIdAsync_NonExistingBooking_ReturnsNull()
        {
            var bookingService = CreateBookingService(_eventService);
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

            Assert.Null(result);
        }
    }
}
