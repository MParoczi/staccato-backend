using System.Security.Claims;
using System.Text.Json;
using ApiModels.Notebooks;
using AutoMapper;
using Domain.Services;
using DomainModels.Enums;
using DomainModels.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("notebooks")]
[Authorize]
public class NotebooksController(INotebookService notebookService, IMapper mapper) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private static NotebookModuleStyle ToStyleDomain(ModuleStyleRequest r)
    {
        return new NotebookModuleStyle
        {
            ModuleType = Enum.Parse<ModuleType>(r.ModuleType, true),
            StylesJson = JsonSerializer.Serialize(new
            {
                backgroundColor = r.BackgroundColor,
                borderColor = r.BorderColor,
                borderStyle = r.BorderStyle,
                borderWidth = r.BorderWidth,
                borderRadius = r.BorderRadius,
                headerBgColor = r.HeaderBgColor,
                headerTextColor = r.HeaderTextColor,
                bodyTextColor = r.BodyTextColor,
                fontFamily = r.FontFamily
            }, JsonOptions)
        };
    }

    // ── GET /notebooks ────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetNotebooks(CancellationToken ct)
    {
        var summaries = await notebookService.GetAllByUserAsync(GetUserId(), ct);
        return Ok(mapper.Map<List<NotebookSummaryResponse>>(summaries));
    }

    // ── POST /notebooks ───────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> CreateNotebook(CreateNotebookRequest request, CancellationToken ct)
    {
        var pageSize = Enum.Parse<PageSize>(request.PageSize, true);
        var styles = request.Styles?.Select(ToStyleDomain).ToList();

        var (notebook, stylesList) = await notebookService.CreateAsync(
            GetUserId(), request.Title, request.InstrumentId,
            pageSize, request.CoverColor, styles, ct);

        var response = mapper.Map<NotebookDetailResponse>(notebook)
            with
            {
                Styles = mapper.Map<List<ModuleStyleResponse>>(stylesList)
            };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    // ── GET /notebooks/{id} ───────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetNotebook(Guid id, CancellationToken ct)
    {
        var (notebook, styles) = await notebookService.GetByIdAsync(id, GetUserId(), ct);

        var response = mapper.Map<NotebookDetailResponse>(notebook)
            with
            {
                Styles = mapper.Map<List<ModuleStyleResponse>>(styles)
            };

        return Ok(response);
    }

    // ── PUT /notebooks/{id} ───────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateNotebook(Guid id, UpdateNotebookRequest request, CancellationToken ct)
    {
        var (notebook, styles) = await notebookService.UpdateAsync(
            id, GetUserId(), request.Title, request.CoverColor, ct);

        var response = mapper.Map<NotebookDetailResponse>(notebook)
            with
            {
                Styles = mapper.Map<List<ModuleStyleResponse>>(styles)
            };

        return Ok(response);
    }

    // ── DELETE /notebooks/{id} ────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNotebook(Guid id, CancellationToken ct)
    {
        await notebookService.DeleteAsync(id, GetUserId(), ct);
        return NoContent();
    }

    // ── GET /notebooks/{id}/styles ────────────────────────────────────────

    [HttpGet("{id:guid}/styles")]
    public async Task<IActionResult> GetStyles(Guid id, CancellationToken ct)
    {
        var styles = await notebookService.GetStylesAsync(id, GetUserId(), ct);
        return Ok(mapper.Map<List<ModuleStyleResponse>>(styles));
    }

    // ── PUT /notebooks/{id}/styles ────────────────────────────────────────

    [HttpPut("{id:guid}/styles")]
    public async Task<IActionResult> BulkUpdateStyles(
        Guid id, List<ModuleStyleRequest> request, CancellationToken ct)
    {
        var styles = request.Select(ToStyleDomain).ToList();
        var updated = await notebookService.BulkUpdateStylesAsync(id, GetUserId(), styles, ct);
        return Ok(mapper.Map<List<ModuleStyleResponse>>(updated));
    }

    // ── POST /notebooks/{id}/styles/apply-preset/{presetId} ──────────────

    [HttpPost("{id:guid}/styles/apply-preset/{presetId:guid}")]
    public async Task<IActionResult> ApplyPreset(Guid id, Guid presetId, CancellationToken ct)
    {
        var updated = await notebookService.ApplyPresetAsync(id, GetUserId(), presetId, ct);
        return Ok(mapper.Map<List<ModuleStyleResponse>>(updated));
    }
}