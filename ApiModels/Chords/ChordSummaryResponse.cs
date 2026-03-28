namespace ApiModels.Chords;

public record ChordSummaryResponse(
    Guid Id,
    string InstrumentKey,
    string Name,
    string Root,
    string Quality,
    string? Extension,
    string? Alternation,
    ChordPositionResponse PreviewPosition
);