using CSCourse.Domain.Models;

namespace CSCourse.Application.Models;

public class AccountRegisterDto
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public AccountRole Role { get; set; }
}