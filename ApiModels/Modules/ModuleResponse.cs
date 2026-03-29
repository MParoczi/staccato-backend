using System.Text.Json;

namespace ApiModels.Modules;

public record ModuleResponse(
    Guid Id,
    Guid LessonPageId,
    string ModuleType,
    int GridX,
    int GridY,
    int GridWidth,
    int GridHeight,
    int ZIndex,
    JsonElement Content);
