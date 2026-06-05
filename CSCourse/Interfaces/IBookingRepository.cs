using CSCourse.Models;

public interface IBookingRepository
{
    Guid Create(BookingRepositoryCreateDto booking);
    Task<Guid> CreateAsync(BookingRepositoryCreateDto booking);
}