namespace CSCourse.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string Path)
        : base(Path)
    {
    }

    public NotFoundException(string Path, Exception inner)
        : base(Path, inner)
    {
    }
}