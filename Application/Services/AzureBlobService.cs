using Application.Options;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain.Services;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class AzureBlobService(BlobServiceClient blobServiceClient, IOptions<AzureBlobOptions> options) : IAzureBlobService
{
    private readonly string _containerName = options.Value.ContainerName;

    public async Task<string> UploadAsync(Stream content, string contentType, string blobPath, CancellationToken ct = default)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(_containerName).GetBlobClient(blobPath);
        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        await blobServiceClient.GetBlobContainerClient(_containerName)
            .DeleteBlobIfExistsAsync(blobPath, cancellationToken: ct);
    }

    public async Task<Stream?> GetStreamAsync(string blobPath, CancellationToken ct = default)
    {
        try
        {
            var response = await blobServiceClient.GetBlobContainerClient(_containerName)
                .GetBlobClient(blobPath)
                .DownloadStreamingAsync(cancellationToken: ct);
            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}