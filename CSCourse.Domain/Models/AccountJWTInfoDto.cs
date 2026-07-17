namespace CSCourse.Domain.Models;

public class AccountJWTInfoDto
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public AccountRole Role { get; set; }

}