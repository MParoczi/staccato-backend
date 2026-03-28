namespace ApiModels.Instruments;

public record InstrumentResponse(
    Guid Id,
    string Key,
    string Name,
    int StringCount
);
