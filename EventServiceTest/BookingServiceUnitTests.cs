using CSCourse.Interfaces;
using CSCourse.Models;
using CSCourse.Services;

namespace EventServiceTest
{
    public class BookingMemoryServiceTests : BookingServiceTestsBase
    {
        protected override IBookingService CreateBookingService()
        {
            return new BookingMemoryService();
        }
    }

    public abstract class BookingServiceTestsBase
    {
        protected abstract IBookingService CreateBookingService();

        [Fact]
        public async Task CreateBookingAsync_ForExistingEvent_ReturnsBookingWithPendingStatus()
        {
            var bookingService = CreateBookingService();
            var eventId = Guid.NewGuid();

            var result = await bookingService.CreateBookingAsync(eventId);

            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllHaveUniqueIds()
        {
            var bookingService = CreateBookingService();
            var eventId = Guid.NewGuid();
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
            var bookingService = CreateBookingService();
            var eventId = Guid.NewGuid();
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
            var bookingService = CreateBookingService();
            var eventId = Guid.NewGuid();
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
            var bookingService = CreateBookingService();
            var nonExistingBookingId = Guid.Empty;

            var result = await bookingService.GetBookingByIdAsync(nonExistingBookingId);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProcessedBookingByIdAsync_NonExistingBooking_ReturnsNull()
        {
            var bookingService = CreateBookingService();
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
