namespace CSCourse.Domain.Exceptions;

public class BookingAlreadyCancelledException : Exception
{
    public BookingAlreadyCancelledException(){}
    public BookingAlreadyCancelledException(string message) : base(message) { }
}
