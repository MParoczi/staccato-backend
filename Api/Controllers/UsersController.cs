using System.Security.Claims;
using ApiModels.Users;
using AutoMapper;
using Domain.Services;
using DomainModels.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController(IUserService userService, IMapper mapper) : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var user = await userService.GetProfileAsync(GetUserId(), ct);
        return Ok(mapper.Map<UserResponse>(user));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
    {
        var language = Enum.Parse<Language>(request.Language);
        var defaultPageSize = request.DefaultPageSize != null
            ? Enum.Parse<PageSize>(request.DefaultPageSize)
            : (PageSize?)null;

        var user = await userService.UpdateProfileAsync(
            GetUserId(),
            request.FirstName,
            request.LastName,
            language,
            defaultPageSize,
            request.DefaultInstrumentId,
            ct);

        return Ok(mapper.Map<UserResponse>(user));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> ScheduleDeletion(CancellationToken ct)
    {
        await userService.ScheduleDeletionAsync(GetUserId(), ct);
        return NoContent();
    }

    [HttpPost("me/cancel-deletion")]
    public async Task<IActionResult> CancelDeletion(CancellationToken ct)
    {
        await userService.CancelDeletionAsync(GetUserId(), ct);
        return NoContent();
    }
}
