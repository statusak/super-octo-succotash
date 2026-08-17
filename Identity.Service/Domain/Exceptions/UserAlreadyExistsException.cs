namespace Identity.Service.Domain.Exceptions;

public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(){}
    public UserAlreadyExistsException(string message) : base(message) { }
}
