using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Lab7.Services;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "profilepictures";

    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> UploadProfilePictureAsync(string teacherId, Stream fileStream, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        string blobName = $"profile_{teacherId}.jpg";
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadProfilePictureAsync(string teacherId)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        BlobClient blobClient = containerClient.GetBlobClient($"profile_{teacherId}.jpg");

        BlobDownloadInfo download = await blobClient.DownloadAsync();
        return download.Content;
    }

    public async Task<object> UploadFileAsync(string fileFileName, Stream stream)
    {
        throw new NotImplementedException();
    }
}