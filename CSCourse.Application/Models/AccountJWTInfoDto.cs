using CSCourse.Domain.Models;

namespace CSCourse.Application.Models;

/// <summary>
/// DTO, содержащий информацию о пользователе, необходимую для генерации JWT-токена.
/// Включает идентификатор, логин и роль — эти данные кодируются в токене в виде утверждений (claims).
/// </summary>
public class AccountJWTInfoDto
{
    /// <summary>
    /// Уникальный идентификатор пользователя в системе, используемый как основной claim (sub) в JWT-токене.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Логин пользователя, который может использоваться как дополнительный идентификатор или для отображения.
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Роль пользователя, определяющая его права доступа. Может быть закодирована в токене как отдельный claim
    /// или преобразована в набор разрешений на стороне API.
    /// </summary>
    public AccountRole Role { get; set; }
}
