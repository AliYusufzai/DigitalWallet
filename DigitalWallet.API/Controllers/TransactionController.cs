using System.Security.Claims;
using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Common.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private int GetUserId()
    {
        string? userId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in token");

        return int.Parse(userId);
    }

    // GET api/transactions
    [HttpGet]
    public async Task<
        ActionResult<ApiResponse<PagedResult<TransactionResponseDto>>>
    > GetMyTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        int userId = GetUserId();
        PagedResult<TransactionResponseDto> result =
            await _transactionService.GetMyTransactionsAsync(userId, page, pageSize);
        return Ok(ApiResponse<PagedResult<TransactionResponseDto>>.Ok(result));
    }

    // GET api/transactions/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TransactionResponseDto>>> GetById(int id)
    {
        TransactionResponseDto? result = await _transactionService.GetByIdAsync(id);

        if (result == null)
            return NotFound(
                ApiResponse<TransactionResponseDto>.Fail($"Transaction {id} not found")
            );

        return Ok(ApiResponse<TransactionResponseDto>.Ok(result));
    }
}
