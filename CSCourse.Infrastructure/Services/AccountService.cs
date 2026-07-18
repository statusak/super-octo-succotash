using CSCourse.Application.Interfaces;
using CSCourse.Application.Models;
using CSCourse.Domain.Exceptions;
using CSCourse.Domain.Models;
using CSCourse.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace CSCourse.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _context;
    private readonly ISecurityService _securityService;

    public AccountService(AppDbContext context, ISecurityService securityService)
    {
        _context = context;
        _securityService = securityService;
    }

    // Имеет место быть состояние гонки. Не приводит к серьезным последствиям, но будет выбрасываться ошибка
    public async Task<bool> Register(AccountRegisterDto accountRegisterDto)
    {
        var existing = await _context.Accounts.FirstOrDefaultAsync(a => a.Login == accountRegisterDto.Login);

        if(existing != null)
        {
            throw new UserAlreadyExistsException();
        }

        var hash = _securityService.HashPassword(accountRegisterDto.Password);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Login = accountRegisterDto.Login,
            HashPassword = hash,
            Role = accountRegisterDto.Role
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<string?> SignIn(AccountSignInDto accountSignInDto)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Login == accountSignInDto.Login);

        if(account == null)
        {
            return null;
        }

        bool isValid = _securityService.VerifyPassword(accountSignInDto.Password, account.HashPassword);
        if (!isValid)
        {
            return null;
        }

        var jwtInfo = new AccountJWTInfoDto
        {
            Id = account.Id,
            Login = account.Login,
            Role = account.Role
        };

        return _securityService.CreateJwtToken(jwtInfo);
    }
}
