using System.Text.Json.Serialization;

namespace ApiModels.Chords;

public record ChordStringResponse(
    [property: JsonPropertyName("string")] int String,
    string State,
    int? Fret,
    int? Finger
);
