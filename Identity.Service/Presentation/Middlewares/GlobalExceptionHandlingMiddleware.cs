using Identity.Service.Domain.Exceptions;
using CSCourse.Contracts.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Identity.Service.Middlewares;
/// <summary>
/// Глобальное middleware для централизованной обработки исключений в приложении.
/// Преобразует необработанные исключения в структурированные JSON‑ответы ProblemDetails
/// и сопоставляет типы исключений с соответствующими HTTP‑статусами.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр middleware для обработки исключений.
    /// </summary>
    /// <param name="next">Делегат <see cref="RequestDelegate"/>, представляющий следующий шаг в конвейере обработки запроса.</param>
    /// <param name="logger">Логгер <see cref="ILogger{TCategoryName}"/> для записи ошибок и диагностики.</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Асинхронно обрабатывает HTTP‑запрос, перехватывая любые исключения,
    /// возникающие в ходе выполнения конвейера. При статусе 404 искусственно
    /// выбрасывает <see cref="NotFoundException"/> для унифицированной обработки.
    /// </summary>
    /// <param name="httpContext">Контекст текущего HTTP‑запроса.</param>
    /// <returns>Задача, представляющая асинхронную операцию обработки запроса.</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
            // https://learn.microsoft.com/en-us/archive/msdn-magazine/2016/june/asp-net-use-custom-middleware-to-detect-and-fix-404s-in-asp-net-core-apps#detecting-and-recording-404-not-found-responses
            if (httpContext.Response.StatusCode == 404)
            {
                throw new NotFoundException($"path {httpContext.Request.Path} did not exists");
            }
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        _logger.LogError(
            ex,
            "Unhandled exception. Method={Method}, Path={Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var statusCode = MapStatusCode(ex);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var error = new ProblemDetails
        {
            Status = statusCode,
            Detail = ex.Message
        };

        await httpContext.Response.WriteAsJsonAsync(error);
    }

    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            NotFoundException nfe => StatusCodes.Status404NotFound,
            UserAlreadyExistsException uaee => StatusCodes.Status409Conflict,
            ValidationException ve => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };


}
