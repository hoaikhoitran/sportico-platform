using System.Text.Json;
using System.Text.Json.Serialization;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;
using SporticoApp.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace SporticoApp.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,

            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            // Do not pass `ex` directly to the logger — rendering Exception.StackTrace via
            // ConsoleLogger throws BadImageFormatException when the exception originates in an
            // assembly with invalid or missing debug metadata.
            try
            {
                _logger.LogWarning(
                    "Handled application exception. Type={ExceptionType} Code={Code} Message={Message} Path={Path}",
                    ex.GetType().FullName,
                    ex.Code,
                    ex.Message,
                    context.Request.Path.Value);
            }
            catch
            {
                // Swallow logging failure — the HTTP response is written regardless.
            }

            context.Response.ContentType = "application/json";

            context.Response.StatusCode = ex.Type switch
            {
                ErrorType.Validation => 400,
                ErrorType.Unauthorized => 401,
                ErrorType.Forbidden => 403,
                ErrorType.NotFound => 404,
                ErrorType.Conflict => 409,
                _ => 500
            };

            var response = new Result<object>
            {
                IsSuccess = false,
                Error = new Error
                {
                    Code = ex.Code,
                    Message = ex.Message,
                    Type = ex.Type,
                    Details = ex.Details
                }
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    response,
                    _jsonOptions));
        }
        catch (Exception ex)
        {
            // Do not pass `ex` directly to the logger — rendering Exception.StackTrace via
            // ConsoleLogger throws BadImageFormatException (0x80131192) when the exception
            // originates in an assembly with invalid or missing debug metadata.
            try
            {
                _logger.LogError(
                    "Unhandled exception. Type={ExceptionType} Message={ExceptionMessage} Path={Path}",
                    ex.GetType().FullName,
                    ex.Message,
                    context.Request.Path.Value);
            }
            catch
            {
                // Swallow logging failure — the HTTP response is written regardless.
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;

            List<string>? details = null;
            if (_environment.IsDevelopment())
            {
                details = new List<string> { ex.Message };

                // ex.StackTrace can itself throw BadImageFormatException when the exception
                // originates in an assembly with invalid debug metadata — guard it.
                string? safeStackTrace = null;
                try
                {
                    safeStackTrace = ex.StackTrace;
                }
                catch
                {
                    safeStackTrace = "StackTrace unavailable";
                }

                if (!string.IsNullOrWhiteSpace(safeStackTrace))
                {
                    details.Add(safeStackTrace);
                }
            }

            var response = new Result<object>
            {
                IsSuccess = false,
                Error = new Error
                {
                    Code = ErrorCodes.InternalServerError,
                    Message = "Something went wrong",
                    Type = ErrorType.Failure,
                    Details = details
                }
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    response,
                    _jsonOptions));
        }
    }
}