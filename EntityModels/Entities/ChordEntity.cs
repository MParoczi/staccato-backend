namespace EntityModels.Entities;

public class ChordEntity : IEntity
{
    public Guid InstrumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Root { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public string? Alternation { get; set; }
    public string PositionsJson { get; set; } = string.Empty;

    public InstrumentEntity Instrument { get; set; } = null!;
    public Guid Id { get; set; }
}