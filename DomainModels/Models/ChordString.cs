using DomainModels.Enums;

namespace DomainModels.Models;

public class ChordString
{
    public int StringNumber { get; set; }
    public ChordStringState State { get; set; }
    public int? Fret { get; set; }
    public int? Finger { get; set; }
}
