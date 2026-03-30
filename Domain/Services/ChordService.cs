using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public class ChordService(IChordRepository chordRepository, IInstrumentRepository instrumentRepository) : IChordService
{
    public async Task<IReadOnlyList<Chord>> SearchAsync(
        InstrumentKey instrumentKey,
        string? root,
        string? quality,
        string? extension,
        string? alternation,
        CancellationToken ct = default)
    {
        var instrument = await instrumentRepository.GetByKeyAsync(instrumentKey, ct);
        if (instrument is null)
            throw new NotFoundException("INSTRUMENT_NOT_FOUND", "Instrument not found.");

        return await chordRepository.SearchAsync(instrument.Id, root, quality, extension, alternation, ct);
    }

    public async Task<Chord> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var chord = await chordRepository.GetByIdAsync(id, ct);
        if (chord is null)
            throw new NotFoundException("Chord not found.");

        return chord;
    }
}