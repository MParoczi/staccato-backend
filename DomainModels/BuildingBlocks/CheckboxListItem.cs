namespace DomainModels.BuildingBlocks;

public class CheckboxListItem
{
    public List<TextSpan> Spans { get; set; } = new();
    public bool IsChecked { get; set; }
}
