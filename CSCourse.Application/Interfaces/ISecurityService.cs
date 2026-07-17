using CSCourse.Application.Models;

namespace CSCourse.Application.Interfaces;

public interface ISecurityService
{
    string GenerateHashFromPassword(string password);
    bool CheckPasswordAndHash(string password, string hash);

    string GenerateJWTToken(AccountJWTInfoDto accountJWTInfoDto);
}