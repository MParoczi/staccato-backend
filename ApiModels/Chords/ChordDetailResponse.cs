namespace ApiModels.Chords;

public record ChordDetailResponse(
    Guid Id,
    string InstrumentKey,
    string Name,
    string Root,
    string Quality,
    string? Extension,
    string? Alternation,
    IReadOnlyList<ChordPositionResponse> Positions
);