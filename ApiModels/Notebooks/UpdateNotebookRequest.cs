namespace ApiModels.Notebooks;

public class UpdateNotebookRequest
{
    public string Title { get; set; } = string.Empty;
    public string CoverColor { get; set; } = string.Empty;
    public Guid? InstrumentId { get; set; }
    public string? PageSize { get; set; }
}
