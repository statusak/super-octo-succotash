using CSCourse.Domain.Models;

namespace CSCourse.Application.Models;

/// <summary>
/// DTO для передачи данных при регистрации нового пользователя. Содержит минимально необходимые поля:
/// логин, пароль в открытом виде (для передачи по защищённому каналу) и предполагаемую роль.
/// На стороне сервиса пароль будет захеширован перед сохранением.
/// </summary>
public class AccountRegisterDto
{
    /// <summary>
    /// Уникальный логин пользователя, используемый для аутентификации. Должен быть уникальным в пределах системы.
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль пользователя в открытом виде
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Роль пользователя в системе (например, администратор, модератор, обычный пользователь).
    /// Определяет набор доступных операций и уровень доступа к ресурсам.
    /// </summary>
    public AccountRole Role { get; set; }
}
