using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public interface IChordService
{
    /// <summary>
    ///     Returns chords for the given instrument, with optional root and quality filters.
    ///     Throws <see cref="Exceptions.NotFoundException" /> with code INSTRUMENT_NOT_FOUND if the instrument key is not in the database.
    /// </summary>
    Task<IReadOnlyList<Chord>> SearchAsync(
        InstrumentKey instrumentKey,
        string? root,
        string? quality,
        CancellationToken ct = default);

    /// <summary>
    ///     Returns the chord with all positions, or throws <see cref="Exceptions.NotFoundException" /> if not found.
    /// </summary>
    Task<Chord> GetByIdAsync(Guid id, CancellationToken ct = default);
}