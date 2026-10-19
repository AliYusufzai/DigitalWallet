using System.Security.Claims;
using DigitalWallet.Application.DTOs.Beneficiary;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Common.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.API.Controllers;

[ApiController]
[Route("api/beneficiaries")]
[Authorize]
public class BeneficiaryController : ControllerBase
{
    private readonly IBeneficiaryService _beneficiaryService;

    public BeneficiaryController(IBeneficiaryService beneficiaryService)
    {
        _beneficiaryService = beneficiaryService;
    }

    private int GetUserId()
    {
        string? userId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in token");

        return int.Parse(userId);
    }

    // GET api/beneficiaries
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BeneficiaryResponseDto>>>> GetAll()
    {
        int userId = GetUserId();
        List<BeneficiaryResponseDto> result = await _beneficiaryService.GetAllAsync(userId);
        return Ok(ApiResponse<List<BeneficiaryResponseDto>>.Ok(result));
    }

    // POST api/beneficiaries
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BeneficiaryResponseDto>>> Add(
        [FromBody] AddBeneficiaryDto dto
    )
    {
        int userId = GetUserId();
        BeneficiaryResponseDto result = await _beneficiaryService.AddAsync(userId, dto);
        return Ok(ApiResponse<BeneficiaryResponseDto>.Ok(result, "Beneficiary added successfully"));
    }

    // DELETE api/beneficiaries/5
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        int userId = GetUserId();
        await _beneficiaryService.DeleteAsync(userId, id);
        return Ok(ApiResponse<object>.Ok("Beneficiary deleted successfully"));
    }
}
