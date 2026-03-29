using ApiModels.Instruments;
using AutoMapper;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/instruments")]
public class InstrumentsController(IInstrumentService instrumentService, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetInstruments(CancellationToken ct)
    {
        var instruments = await instrumentService.GetAllAsync(ct);
        return Ok(mapper.Map<IReadOnlyList<InstrumentResponse>>(instruments));
    }
}