using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CSCourse.Middlewares
{
    public class NotFoundException : Exception
    {
        public NotFoundException()
        {
        }

        public NotFoundException(string Path)
            : base(Path)
        {
        }

        public NotFoundException(string Path, Exception inner)
            : base(Path, inner)
        {
        }
    }


    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;


        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

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
                ValidationException ve => StatusCodes.Status400BadRequest,
                NotFoundException nfe => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };


    }
}
