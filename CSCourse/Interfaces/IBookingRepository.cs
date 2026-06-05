using CSCourse.Models;

public interface IBookingRepository
{
    Guid Create(BookingRepositoryCreateDto booking);
    IEnumerable<Booking> GetPending();

    Task<Guid> CreateAsync(BookingRepositoryCreateDto booking);
    Task<IEnumerable<Booking>> GetPendingAsync();

    Task<Booking?> GetByIdAsync(Guid id);

}