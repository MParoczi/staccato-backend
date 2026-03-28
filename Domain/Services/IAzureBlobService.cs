namespace Domain.Services;

public interface IAzureBlobService
{
    Task<string> UploadAsync(Stream content, string contentType, string blobPath, CancellationToken ct = default);
    Task DeleteAsync(string blobPath, CancellationToken ct = default);
    Task<Stream?> GetStreamAsync(string blobPath, CancellationToken ct = default);
}