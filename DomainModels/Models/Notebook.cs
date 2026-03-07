using DomainModels.Enums;

namespace DomainModels.Models;

public class Notebook
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid InstrumentId { get; init; }
    public PageSize PageSize { get; init; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
