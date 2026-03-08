using DomainModels.Enums;

namespace EntityModels.Entities;

public class InstrumentEntity : IEntity
{
    public InstrumentKey Key { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int StringCount { get; set; }

    public ICollection<ChordEntity> Chords { get; set; } = new List<ChordEntity>();
    public Guid Id { get; set; }
}