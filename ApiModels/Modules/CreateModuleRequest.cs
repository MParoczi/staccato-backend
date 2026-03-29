using System.Text.Json;

namespace ApiModels.Modules;

public class CreateModuleRequest
{
    public string ModuleType { get; set; } = string.Empty;
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
    public int ZIndex { get; set; }
    public JsonElement Content { get; set; }
}
