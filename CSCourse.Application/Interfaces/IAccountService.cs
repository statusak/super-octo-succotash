using CSCourse.Domain.Models;

namespace CSCourse.Application.Interfaces;

public interface IAccountService
{
    Task<bool> Register(AccountRegisterDto accountRegisterDto);
    Task<string?> SignIn(AccountSignInDto accountSignInDto);
} 