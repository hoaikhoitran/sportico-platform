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
            _logger.LogWarning(ex, "Handled application exception");

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
            _logger.LogError(ex, "Unhandled exception");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;

            List<string>? details = null;
            if (_environment.IsDevelopment())
            {
                details = new List<string> { ex.Message };
                if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                {
                    details.Add(ex.StackTrace);
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