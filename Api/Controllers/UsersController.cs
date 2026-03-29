using System.Security.Claims;
using System.Text.Json;
using ApiModels.Users;
using AutoMapper;
using Domain.Services;
using DomainModels.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController(IUserService userService, IMapper mapper) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var user = await userService.GetProfileAsync(GetUserId(), ct);
        return Ok(mapper.Map<UserResponse>(user));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
    {
        var language = request.Language switch
        {
            "en" => Language.English,
            "hu" => Language.Hungarian,
            _ => throw new ArgumentOutOfRangeException(nameof(request.Language), request.Language, "Unsupported language.")
        };
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

    [HttpPut("me/avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarRequest request, CancellationToken ct)
    {
        using var stream = request.File.OpenReadStream();
        var user = await userService.UploadAvatarAsync(GetUserId(), stream, request.File.ContentType, ct);
        return Ok(mapper.Map<UserResponse>(user));
    }

    [HttpDelete("me/avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        await userService.DeleteAvatarAsync(GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("me/presets")]
    public async Task<IActionResult> GetPresets(CancellationToken ct)
    {
        var presets = await userService.GetPresetsAsync(GetUserId(), ct);
        return Ok(mapper.Map<List<PresetResponse>>(presets));
    }

    [HttpPost("me/presets")]
    public async Task<IActionResult> CreatePreset(SavePresetRequest request, CancellationToken ct)
    {
        var stylesJson = JsonSerializer.Serialize(request.Styles);
        var preset = await userService.CreatePresetAsync(GetUserId(), request.Name, stylesJson, ct);
        return StatusCode(StatusCodes.Status201Created, mapper.Map<PresetResponse>(preset));
    }

    [HttpPut("me/presets/{id:guid}")]
    public async Task<IActionResult> UpdatePreset(Guid id, UpdatePresetRequest request, CancellationToken ct)
    {
        var stylesJson = request.Styles != null ? JsonSerializer.Serialize(request.Styles) : null;
        var preset = await userService.UpdatePresetAsync(GetUserId(), id, request.Name, stylesJson, ct);
        return Ok(mapper.Map<PresetResponse>(preset));
    }

    [HttpDelete("me/presets/{id:guid}")]
    public async Task<IActionResult> DeletePreset(Guid id, CancellationToken ct)
    {
        await userService.DeletePresetAsync(GetUserId(), id, ct);
        return NoContent();
    }
}