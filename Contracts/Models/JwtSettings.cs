namespace CSCourse.Contracts.Models;

/// <summary>
/// Настройки JWT‑токенов для аутентификации в проекте CSCourse.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Секретный ключ, используемый для подписи JWT‑токенов.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Издатель (Issuer) JWT‑токена — обычно URL или идентификатор сервиса.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Аудитория (Audience) JWT‑токена — получатель токена (например, API).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Время жизни токена в минутах.
    /// </summary>
    public int ExpirationMinutes { get; set; }
}
