using System.Text.Json.Serialization;

namespace DomainModels.Models;

public class ChordBarre
{
    public int Fret { get; set; }
    public int FromString { get; set; }

    [JsonPropertyName("toString")]
    public int StringTo { get; set; }
}