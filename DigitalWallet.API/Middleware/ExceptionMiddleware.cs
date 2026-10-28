using System.Globalization;
using System.Text.Json;
using DigitalWallet.Common.Wrappers;
using DigitalWallet.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
        catch (Exception ex) when (IsConflict(ex))
        {
            var message =
                ex is ConflictException
                    ? ex.Message
                    : "The operation conflicted with another request. Retry with the same idempotency key.";
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, message);
        }
        catch (TooManyRequestsException ex)
        {
            context.Response.Headers.RetryAfter = ex.RetryAfterSeconds.ToString(
                CultureInfo.InvariantCulture
            );
            await WriteErrorAsync(context, StatusCodes.Status429TooManyRequests, ex.Message);
        }
        catch (EmailDeliveryException ex)
        {
            _logger.LogError(ex, "Email delivery could not be confirmed");
            await WriteErrorAsync(context, StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (InsufficientFundsException ex)
        {
            _logger.LogWarning(ex, "Insufficient funds: {Message}", ex.Message);
            await WriteErrorAsync(context, StatusCodes.Status422UnprocessableEntity, ex.Message);
        }
        catch (WalletException ex)
        {
            _logger.LogWarning(ex, "Wallet error: {Message}", ex.Message);
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred"
            );
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
    }

    private static bool IsConflict(Exception ex) =>
        ex is ConflictException
        || ex is DbUpdateConcurrencyException
        || ex is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } };
}
