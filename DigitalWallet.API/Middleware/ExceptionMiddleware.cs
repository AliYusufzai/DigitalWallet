using System.Text.Json;
using DigitalWallet.Common.Wrappers;
using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<object>.Fail(ex.Message))
            );
        }
        catch (InsufficientFundsException ex)
        {
            _logger.LogWarning(ex.Message);
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<object>.Fail(ex.Message))
            );
        }
        catch (WalletException ex)
        {
            _logger.LogWarning(ex.Message);
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<object>.Fail(ex.Message))
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<object>.Fail("An unexpected error occurred"))
            );
        }
    }
}
