using DomainModels.Enums;
using EntityModels;

namespace EntityModels.Entities;

public class InstrumentEntity : IEntity
{
    public Guid Id { get; set; }
    public InstrumentKey Key { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int StringCount { get; set; }

    public ICollection<ChordEntity> Chords { get; set; } = new List<ChordEntity>();
}
