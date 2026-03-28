using ApiModels.Notebooks;
using AutoMapper;
using Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("presets")]
public class PresetsController(ISystemStylePresetRepository presetRepo, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPresets(CancellationToken ct)
    {
        var presets = await presetRepo.GetAllAsync(ct);
        return Ok(mapper.Map<List<SystemStylePresetResponse>>(presets));
    }
}