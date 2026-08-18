using Identity.Service.Application.Interfaces;
using Identity.Service.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Identity.Service.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Service.Controllers;

/// <summary>
/// Контроллер для работы с бронированиями.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public AuthController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    /// <summary>
    /// Регистрирует нового пользователя в системе. Создаёт учётную запись на основе переданных данных,
    /// включая логин, пароль и роль. Перед выполнением бизнес-операции выполняется валидация входных DTO-объектов.
    /// При успешной регистрации возвращается статус 204 NoContent.
    /// </summary>
    /// <remarks>
    /// В случае, если пользователь с указанным логином или email уже существует, операция завершается ошибкой 409 Conflict.
    /// Ошибки валидации модели (например, некорректный формат полей) приводят к ответу 400 Bad Request.
    /// </remarks>
    /// <param name="accountRegisterDto">Объект <see cref="AccountRegisterDto"/>, содержащий данные для регистрации:
    /// логин, пароль и предполагаемую роль пользователя.</param>
    /// <returns>
    /// Возвращает <see cref="ActionResult"/> со статусом 204, что означает успешную регистрацию.
    /// При ошибках валидации возвращает 400 Bad Request с деталями ошибок.
    /// При конфликте (пользователь уже существует) возвращает 409 Conflict с поясняющим сообщением.
    /// </returns>
    /// <response code="204">Пользователь успешно зарегистрирован, учётная запись создана.</response>
    /// <response code="400">Ошибка валидации входных данных либо общая ошибка при обработке запроса.</response>
    /// <response code="409">Пользователь с указанным логином или email уже зарегистрирован в системе.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Register([FromBody] AccountRegisterDto accountRegisterDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Validation failed", errors = ModelState });
        }

        try
        {
            await _identityService.Register(accountRegisterDto);
            return NoContent();
        }
        catch (UserAlreadyExistsException)
        {
            return Conflict(new { message = "User with this login or email already exists" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Выполняет аутентификацию пользователя и возвращает JWT-токен доступа для последующих авторизованных запросов.
    /// Принимает учётные данные (логин и пароль), проверяет их корректность и при успехе генерирует токен.
    /// </summary>
    /// <remarks>
    /// Если предоставленные учётные данные не соответствуют ни одной из существующих учётных записей,
    /// операция завершается с ответом 404 NotFound. Перед проверкой учётных данных выполняется валидация
    /// структуры входящего DTO-объекта.
    /// </remarks>
    /// <param name="accountSignInDto">Объект <see cref="AccountSignInDto"/>, содержащий логин и пароль пользователя
    /// для проверки подлинности.</param>
    /// <returns>
    /// Возвращает <see cref="ActionResult{T}"/> со строкой JWT-токена при успешной аутентификации (статус 200 OK).
    /// В случае неверных учётных данных возвращает 404 Not Found с сообщением об ошибке.
    /// При ошибках валидации входных данных — 400 Bad Request.
    /// </returns>
    /// <response code="200">Аутентификация успешна, в теле ответа содержится JWT-токен для доступа к защищённым ресурсам.</response>
    /// <response code="404">Неверные учётные данные: логин или пароль не соответствуют существующей учётной записи.</response>
    /// <response code="400">Ошибка валидации структуры входных данных.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> Login([FromBody] AccountSignInDto accountSignInDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Validation failed", errors = ModelState });
        }

        string? jwtString = await _identityService.SignIn(accountSignInDto);

        if (string.IsNullOrEmpty(jwtString))
        {
            return NotFound(new { message = "Invalid credentials" });
        }

        return Ok(jwtString);
    }
}