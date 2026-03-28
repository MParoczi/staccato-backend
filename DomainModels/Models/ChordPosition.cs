namespace DomainModels.Models;

public class ChordPosition
{
    public string Label { get; set; } = string.Empty;
    public int BaseFret { get; set; }
    public ChordBarre? Barre { get; set; }
    public List<ChordString> Strings { get; set; } = [];
}
