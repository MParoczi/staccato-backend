using System.Text.Json.Serialization;

namespace ApiModels.Chords;

public record ChordBarreResponse(
    int Fret,
    int FromString,
    [property: JsonPropertyName("toString")] int StringTo
);
