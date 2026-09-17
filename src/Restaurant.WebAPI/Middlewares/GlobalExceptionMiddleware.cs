using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Restaurant.WebAPI.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict occurred.");
            await HandleExceptionAsync(context, HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT", "Dữ liệu đã bị thay đổi bởi thao tác khác, vui lòng tải lại trang và thử lại.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation occurred.");
            await HandleExceptionAsync(context, HttpStatusCode.BadRequest, "INVALID_OPERATION", ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argument exception occurred.");
            await HandleExceptionAsync(context, HttpStatusCode.BadRequest, "BAD_REQUEST", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", "Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.");
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string errorCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var result = JsonSerializer.Serialize(new
        {
            success = false,
            data = (object?)null,
            error = new
            {
                code = errorCode,
                message = message
            },
            timestamp = DateTime.UtcNow
        });

        return context.Response.WriteAsync(result);
    }
}
