using DomainModels.Enums;

namespace DomainModels.Models;

public class Chord
{
    public Guid Id { get; set; }
    public Guid InstrumentId { get; set; }
    public InstrumentKey InstrumentKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Root { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public string? Alternation { get; set; }
    public List<ChordPosition> Positions { get; set; } = [];
}