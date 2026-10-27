using System.ComponentModel.DataAnnotations;
using DigitalWallet.Application.DTOs.Reports;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Common.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : ControllerBase
{
    [HttpGet("daily-volume")]
    public async Task<ActionResult<ApiResponse<List<DailyVolumeDto>>>> GetDailyVolume(
        [FromQuery, Required] DateOnly? from,
        [FromQuery, Required] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
            return Unauthorized();
        var report = await _reportService.GetDailyVolumeAsync(
            userId, from!.Value, to!.Value, cancellationToken);
        return Ok(ApiResponse<List<DailyVolumeDto>>.Ok(report));
    }

    [HttpGet("monthly-statement")]
    public async Task<ActionResult<ApiResponse<MonthlyStatementDto>>> GetMonthlyStatement(
        [FromQuery, Required] int? year,
        [FromQuery, Required] int? month,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
            return Unauthorized();
        var report = await _reportService.GetMonthlyStatementAsync(
            userId, year!.Value, month!.Value, cancellationToken);
        return Ok(ApiResponse<MonthlyStatementDto>.Ok(report));
    }

    private readonly IReportService _reportService;
    private readonly ICurrentUser _currentUser;

    public ReportController(IReportService reportService, ICurrentUser currentUser)
    {
        _reportService = reportService;
        _currentUser = currentUser;
    }

    [HttpGet("spending-by-type")]
    public async Task<ActionResult<ApiResponse<List<SpendingByTypeDto>>>> GetSpendingByType(
        [FromQuery, Required] DateOnly? from,
        [FromQuery, Required] DateOnly? to,
        CancellationToken cancellationToken
    )
    {
        int? userId = _currentUser.UserId;

        if (userId == null)
        {
            return Unauthorized();
        }

        var report = await _reportService.GetSpendingByTypeAsync(
            userId.Value,
            from!.Value,
            to!.Value,
            cancellationToken
        );

        return Ok(ApiResponse<List<SpendingByTypeDto>>.Ok(report));
    }
}
