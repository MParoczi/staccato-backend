using System.Security.Claims;
using ApiModels.Exports;
using AutoMapper;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("exports")]
[Authorize]
public class ExportsController(IPdfExportService exportService, IMapper mapper) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExport(
        [FromBody] CreatePdfExportRequest request, CancellationToken ct)
    {
        var export = await exportService.QueueExportAsync(
            GetUserId(), request.NotebookId, request.LessonIds, ct);

        var response = mapper.Map<CreatePdfExportResponse>(export);
        return StatusCode(StatusCodes.Status202Accepted, response);
    }

    [HttpGet]
    public async Task<IActionResult> GetExports(CancellationToken ct)
    {
        var exports = await exportService.GetExportsByUserAsync(GetUserId(), ct);
        var response = mapper.Map<IReadOnlyList<PdfExportResponse>>(exports);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetExport(Guid id, CancellationToken ct)
    {
        var export = await exportService.GetExportByIdAsync(id, GetUserId(), ct);
        var response = mapper.Map<PdfExportResponse>(export);
        return Ok(response);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadExport(Guid id, CancellationToken ct)
    {
        var (content, fileName, contentType) =
            await exportService.DownloadExportAsync(id, GetUserId(), ct);

        return File(content, contentType, fileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExport(Guid id, CancellationToken ct)
    {
        await exportService.DeleteExportAsync(id, GetUserId(), ct);
        return NoContent();
    }
}
