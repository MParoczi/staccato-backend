using System.Security.Claims;
using ApiModels.Lessons;
using AutoMapper;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("lessons")]
[Authorize]
public class LessonPagesController(ILessonPageService lessonPageService, IMapper mapper) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // ── GET /lessons/{id}/pages ──────────────────────────────────────────

    [HttpGet("{id:guid}/pages")]
    public async Task<IActionResult> GetPages(Guid id, CancellationToken ct)
    {
        var pages = await lessonPageService.GetByLessonIdAsync(id, GetUserId(), ct);
        return Ok(mapper.Map<List<LessonPageResponse>>(pages));
    }

    // ── POST /lessons/{id}/pages ─────────────────────────────────────────

    [HttpPost("{id:guid}/pages")]
    public async Task<IActionResult> AddPage(Guid id, CancellationToken ct)
    {
        var (page, isOverSoftLimit) = await lessonPageService.AddPageAsync(id, GetUserId(), ct);
        var pageResponse = mapper.Map<LessonPageResponse>(page);

        var response = new LessonPageWithWarningResponse(
            pageResponse,
            isOverSoftLimit
                ? "This lesson has reached the recommended maximum of 10 pages."
                : null);

        return isOverSoftLimit
            ? Ok(response)
            : StatusCode(StatusCodes.Status201Created, response);
    }

    // ── DELETE /lessons/{lessonId}/pages/{pageId} ────────────────────────

    [HttpDelete("{lessonId:guid}/pages/{pageId:guid}")]
    public async Task<IActionResult> DeletePage(Guid lessonId, Guid pageId, CancellationToken ct)
    {
        await lessonPageService.DeletePageAsync(lessonId, pageId, GetUserId(), ct);
        return NoContent();
    }
}
