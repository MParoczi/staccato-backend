namespace ApiModels.Notebooks;

public record ModuleStyleRequest(
    string ModuleType,
    string BackgroundColor,
    string BorderColor,
    string BorderStyle,
    int BorderWidth,
    int BorderRadius,
    string HeaderBgColor,
    string HeaderTextColor,
    string BodyTextColor,
    string FontFamily
);
