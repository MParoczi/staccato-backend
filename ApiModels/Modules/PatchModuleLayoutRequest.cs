namespace ApiModels.Modules;

public class PatchModuleLayoutRequest
{
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
    public int ZIndex { get; set; }
}
