using System.Security.Claims;
using ApiModels.Lessons;
using AutoMapper;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class LessonsController(ILessonService lessonService, IMapper mapper) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // ── GET /notebooks/{id}/lessons ──────────────────────────────────────

    [HttpGet("notebooks/{id:guid}/lessons")]
    public async Task<IActionResult> GetLessons(Guid id, CancellationToken ct)
    {
        var summaries = await lessonService.GetByNotebookIdAsync(id, GetUserId(), ct);
        return Ok(mapper.Map<List<LessonSummaryResponse>>(summaries));
    }

    // ── POST /notebooks/{id}/lessons ─────────────────────────────────────

    [HttpPost("notebooks/{id:guid}/lessons")]
    public async Task<IActionResult> CreateLesson(Guid id, CreateLessonRequest request, CancellationToken ct)
    {
        var (lesson, pages) = await lessonService.CreateAsync(id, GetUserId(), request.Title, ct);
        var response = new LessonDetailResponse(
            lesson.Id,
            lesson.NotebookId,
            lesson.Title,
            lesson.CreatedAt.ToString("o"),
            mapper.Map<List<LessonPageResponse>>(pages));
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // ── GET /lessons/{id} ────────────────────────────────────────────────

    [HttpGet("lessons/{id:guid}")]
    public async Task<IActionResult> GetLesson(Guid id, CancellationToken ct)
    {
        var (lesson, pages) = await lessonService.GetByIdAsync(id, GetUserId(), ct);
        var response = new LessonDetailResponse(
            lesson.Id,
            lesson.NotebookId,
            lesson.Title,
            lesson.CreatedAt.ToString("o"),
            mapper.Map<List<LessonPageResponse>>(pages));
        return Ok(response);
    }

    // ── PUT /lessons/{id} ────────────────────────────────────────────────

    [HttpPut("lessons/{id:guid}")]
    public async Task<IActionResult> UpdateLesson(Guid id, UpdateLessonRequest request, CancellationToken ct)
    {
        var (lesson, pages) = await lessonService.UpdateAsync(id, GetUserId(), request.Title, ct);
        var response = new LessonDetailResponse(
            lesson.Id,
            lesson.NotebookId,
            lesson.Title,
            lesson.CreatedAt.ToString("o"),
            mapper.Map<List<LessonPageResponse>>(pages));
        return Ok(response);
    }

    // ── DELETE /lessons/{id} ─────────────────────────────────────────────

    [HttpDelete("lessons/{id:guid}")]
    public async Task<IActionResult> DeleteLesson(Guid id, CancellationToken ct)
    {
        await lessonService.DeleteAsync(id, GetUserId(), ct);
        return NoContent();
    }

    // ── GET /notebooks/{id}/index ────────────────────────────────────────

    [HttpGet("notebooks/{id:guid}/index")]
    public async Task<IActionResult> GetNotebookIndex(Guid id, CancellationToken ct)
    {
        var entries = await lessonService.GetNotebookIndexAsync(id, GetUserId(), ct);
        var response = new NotebookIndexResponse(
            mapper.Map<List<NotebookIndexEntryResponse>>(entries));
        return Ok(response);
    }
}