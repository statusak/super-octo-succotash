namespace CSCourse.Domain.Models;

/// <summary>
/// Роль учётной записи в системе.
/// </summary>
public enum AccountRole
{
    /// <summary>
    /// Обычный пользователь.
    /// </summary>
    User,

    /// <summary>
    /// Администратор системы.
    /// </summary>
    Admin
}

/// <summary>
/// Учётная запись пользователя (аккаунт) в системе.
/// Содержит идентификаторы, учётные данные и роль доступа.
/// </summary>
public class Account
{
    /// <summary>
    /// Уникальный идентификатор учётной записи (GUID).
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Логин пользователя, используемый для аутентификации.
    /// Должен быть уникальным.
    /// </summary>
    public required string Login { get; set; }

    /// <summary>
    /// Хеш пароля пользователя.
    /// Хранится в безопасном формате, никогда не возвращается в ответах API.
    /// </summary>
    public required string HashPassword { get; set; }

    /// <summary>
    /// Роль пользователя в системе (User или Admin).
    /// Определяет уровень доступа к функциям API.
    /// </summary>
    public required AccountRole Role { get; set; }
}
