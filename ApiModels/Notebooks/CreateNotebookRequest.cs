namespace ApiModels.Notebooks;

public class CreateNotebookRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid InstrumentId { get; set; }
    public string PageSize { get; set; } = string.Empty;
    public string CoverColor { get; set; } = string.Empty;
    public List<ModuleStyleRequest>? Styles { get; set; }
}
