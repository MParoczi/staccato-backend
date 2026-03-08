using DomainModels.Enums;

namespace DomainModels.Models;

public class Instrument
{
    public Guid Id { get; set; }
    public InstrumentKey Key { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int StringCount { get; set; }
}