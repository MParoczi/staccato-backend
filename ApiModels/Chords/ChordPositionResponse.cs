namespace ApiModels.Chords;

public record ChordPositionResponse(
    string Label,
    int BaseFret,
    ChordBarreResponse? Barre,
    IReadOnlyList<ChordStringResponse> Strings
);
