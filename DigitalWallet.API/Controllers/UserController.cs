using System.Security.Claims;
using DigitalWallet.Application.DTOs.User;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Common.Wrappers;
using DigitalWallet.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.API.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    private int GetUserId()
    {
        string? userId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in token");

        return int.Parse(userId);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        int userId = GetUserId();
        UserProfileDto result = await _userService.GetProfileAsync(userId);
        return Ok(ApiResponse<UserProfileDto>.Ok(result));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(
        [FromBody] UpdateProfileDto dto
    )
    {
        int userId = GetUserId();
        UserProfileDto result = await _userService.UpdateProfileAsync(userId, dto);
        return Ok(ApiResponse<UserProfileDto>.Ok(result, "Profile Updated Successfully"));
    }

    [HttpPut("password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordDto dto
    )
    {
        int userId = GetUserId();
        await _userService.ChangePasswordAsync(userId, dto);
        return Ok(ApiResponse<object>.Ok("Password changed successfully"));
    }
}
