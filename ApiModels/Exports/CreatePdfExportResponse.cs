namespace ApiModels.Exports;

public record CreatePdfExportResponse(
    Guid ExportId,
    string Status);
