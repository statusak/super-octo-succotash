using CSCourse.Application.Models;

namespace CSCourse.Application.Interfaces;

public interface ISecurityService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);

    string CreateJwtToken(AccountJWTInfoDto accountJWTInfoDto);
}