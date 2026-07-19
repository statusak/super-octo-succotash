namespace CSCourse.Application.Models;

/// <summary>
/// DTO для передачи учётных данных при попытке входа в систему. Содержит логин и пароль,
/// используемые для проверки подлинности пользователя.
/// </summary>
public class AccountSignInDto
{
    /// <summary>
    /// Логин пользователя, используемый для аутентификации.
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль пользователя в открытом виде. Передаётся по защищённому соединению и проверяется
    /// путём сравнения с сохранённым хешем.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}