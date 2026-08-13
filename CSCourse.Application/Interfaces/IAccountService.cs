using CSCourse.Application.Models;

namespace CSCourse.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса учётных записей, определяющий базовые операции по управлению пользователями:
/// регистрация новых пользователей и аутентификация с получением токена доступа.
/// Реализуется в инфраструктурном слое (Infrastructure) с использованием механизмов хеширования паролей
/// и генерации JWT-токенов.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Регистрирует нового пользователя на основе предоставленных данных. Выполняет необходимые проверки
    /// (например, уникальность логина/email) и сохраняет учётную запись в хранилище.
    /// </summary>
    /// <param name="accountRegisterDto">Объект <see cref="AccountRegisterDto"/> с данными для регистрации:
    /// логин, пароль (в открытом виде) и роль пользователя.</param>
    /// <returns>Асинхронная операция, возвращающая <see langword="true"/> при успешной регистрации.</returns>
    /// <exception cref="UserAlreadyExistsException">Выбрасывается, если пользователь с таким логином или email
    /// уже присутствует в системе.</exception>
    Task<bool> Register(AccountRegisterDto accountRegisterDto);

    /// <summary>
    /// Аутентифицирует пользователя по логину и паролю. Проверяет корректность учётных данных,
    /// а при успехе формирует JWT-токен, содержащий информацию о пользователе и его правах.
    /// </summary>
    /// <param name="accountSignInDto">Объект <see cref="AccountSignInDto"/> с учётными данными:
    /// логин и пароль для проверки.</param>
    /// <returns>
    /// Асинхронная операция, возвращающая JWT-токен в виде строки при успешной аутентификации.
    /// Если учётные данные неверны, возвращается <see langword="null"/>.
    /// </returns>
    Task<string?> SignIn(AccountSignInDto accountSignInDto);
}
