using System.ComponentModel.DataAnnotations;
using ApiModels.Chords;
using AutoMapper;
using Domain.Services;
using DomainModels.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("chords")]
public class ChordsController(IChordService chordService, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetChords(
        [FromQuery] [Required] InstrumentKey? instrument,
        [FromQuery] string? root,
        [FromQuery] string? quality,
        [FromQuery] string? extension,
        [FromQuery] string? alternation,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        var chords = await chordService.SearchAsync(instrument!.Value, root, quality, extension, alternation, ct);
        return Ok(mapper.Map<IReadOnlyList<ChordSummaryResponse>>(chords));
    }

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetChordById(Guid id, CancellationToken ct)
    {
        var chord = await chordService.GetByIdAsync(id, ct);
        return Ok(mapper.Map<ChordDetailResponse>(chord));
    }
}