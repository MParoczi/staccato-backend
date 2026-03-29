using System.Security.Claims;
using System.Text.Json;
using ApiModels.Modules;
using AutoMapper;
using Domain.Services;
using DomainModels.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class ModulesController(IModuleService moduleService, IMapper mapper) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // ── GET /pages/{pageId}/modules ─────────────────────────────────────

    [HttpGet("/pages/{pageId:guid}/modules")]
    public async Task<IActionResult> GetModules(Guid pageId, CancellationToken ct)
    {
        var modules = await moduleService.GetModulesByPageIdAsync(pageId, GetUserId(), ct);
        return Ok(mapper.Map<List<ModuleResponse>>(modules));
    }

    // ── POST /pages/{pageId}/modules ────────────────────────────────────

    [HttpPost("/pages/{pageId:guid}/modules")]
    public async Task<IActionResult> CreateModule(
        Guid pageId, [FromBody] CreateModuleRequest request, CancellationToken ct)
    {
        var moduleType = Enum.Parse<ModuleType>(request.ModuleType, true);
        var contentJson = request.Content.GetRawText();

        var module = await moduleService.CreateModuleAsync(
            pageId, moduleType,
            request.GridX, request.GridY, request.GridWidth, request.GridHeight, request.ZIndex,
            contentJson, GetUserId(), ct);

        return StatusCode(StatusCodes.Status201Created, mapper.Map<ModuleResponse>(module));
    }

    // ── PUT /modules/{moduleId} ─────────────────────────────────────────

    [HttpPut("/modules/{moduleId:guid}")]
    public async Task<IActionResult> UpdateModule(
        Guid moduleId, [FromBody] UpdateModuleRequest request, CancellationToken ct)
    {
        var moduleType = Enum.Parse<ModuleType>(request.ModuleType, true);
        var contentJson = request.Content.GetRawText();

        var module = await moduleService.UpdateModuleAsync(
            moduleId, moduleType,
            request.GridX, request.GridY, request.GridWidth, request.GridHeight, request.ZIndex,
            contentJson, GetUserId(), ct);

        return Ok(mapper.Map<ModuleResponse>(module));
    }

    // ── PATCH /modules/{moduleId}/layout ────────────────────────────────

    [HttpPatch("/modules/{moduleId:guid}/layout")]
    public async Task<IActionResult> UpdateModuleLayout(
        Guid moduleId, [FromBody] PatchModuleLayoutRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    // ── DELETE /modules/{moduleId} ──────────────────────────────────────

    [HttpDelete("/modules/{moduleId:guid}")]
    public async Task<IActionResult> DeleteModule(Guid moduleId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
