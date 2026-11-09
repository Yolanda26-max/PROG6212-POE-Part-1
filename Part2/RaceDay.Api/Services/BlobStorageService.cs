using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
    Task DeleteImageAsync(string? blobUrl);
}

public class BlobStorageService : IBlobStorageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly BlobContainerClient _container;
    private bool _containerReady;

    public BlobStorageService(IConfiguration config)
    {
        // Uses appsettings.json (or an environment variable in Docker).
        // Falls back to the local Azurite emulator if nothing is configured.
        var connectionString = config["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "UseDevelopmentStorage=true";
        }

        var containerName = config["AzureBlob:ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName))
        {
            containerName = "raceday-images";
        }

        // Creating the client does not contact storage yet, so the API starts even if storage is off
        _container = new BlobContainerClient(connectionString, containerName);
    }

    // Creates the container the first time an image is uploaded.
    // Images are readable by anyone with the link; only the API can upload.
    private async Task EnsureContainerAsync()
    {
        if (_containerReady) return;

        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);
        _containerReady = true;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was provided.");

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException("The image must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Only .jpg, .jpeg, .png and .webp images are allowed.");

        await EnsureContainerAsync();

        var blobName = $"{folder}/{Guid.NewGuid()}{extension}";
        var blobClient = _container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blobClient.Uri.ToString();
    }

    public async Task DeleteImageAsync(string? blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl)) return;

        try
        {
            var blobName = new BlobClient(new Uri(blobUrl)).Name;
            await _container.GetBlobClient(blobName).DeleteIfExistsAsync();
        }
        catch
        {
            // If the old image can't be removed we just carry on
        }
    }
}