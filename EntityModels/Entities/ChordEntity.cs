namespace EntityModels.Entities;

public class ChordEntity
{
    public Guid Id { get; set; }
    public Guid InstrumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public string PositionsJson { get; set; } = string.Empty;

    public InstrumentEntity Instrument { get; set; } = null!;
}
