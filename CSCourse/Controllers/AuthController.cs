using CSCourse.Application.Interfaces;
using CSCourse.Application.Models;
using Microsoft.AspNetCore.Mvc;
using CSCourse.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace CSCourse.Controllers;

/// <summary>
/// Контроллер для работы с бронированиями.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AuthController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Регистрация нового пользователя.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
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
            await _accountService.Register(accountRegisterDto);
            return CreatedAtAction(nameof(Register), null, new { message = "User registered successfully" });
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
    /// Авторизация пользователя и получение JWT-токена.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<string>> Login([FromBody] AccountSignInDto accountSignInDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Validation failed", errors = ModelState });
        }

        string? jwtString = await _accountService.SignIn(accountSignInDto);

        if (string.IsNullOrEmpty(jwtString))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(jwtString);
    }
}