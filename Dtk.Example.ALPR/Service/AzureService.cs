namespace Dtk.Example.ALPR.Service;

/// <summary>
/// Provides Azure Blob Storage integration services for ALPR system
/// </summary>
public static class AzureService
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <param name="eventId">Unique event identifier</param>
    /// <param name="filePath">Local file path to upload</param>
    /// <param name="containerType">Destination container type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the upload operation</returns>
    /// <remarks>
    /// Implementation steps:
    /// 1. Initialize BlobServiceClient with connection string
    /// 2. Get reference to target container (BlobContainerClient)
    /// 3. Create blob client with event-based filename
    /// 4. Perform upload with overwrite protection
    /// </remarks>
    public static async Task UploadBlobAsync(Guid eventId, string filePath, TipoContainerImage containerType, CancellationToken cancellationToken)
    {
        // TODO: Implement actual Azure Blob Storage upload logic
        // Suggested implementation:
        // var blobServiceClient = new BlobServiceClient(connectionString);
        // var containerClient = blobServiceClient.GetBlobContainerClient(containerType.ToString().ToLower());
        // var blobClient = containerClient.GetBlobClient($"{eventId}.jpg");
        // await blobClient.UploadAsync(filePath, overwrite: true, cancellationToken);

        Console.WriteLine($"[AzureService] Simulating BLOB upload for EventId: {eventId}, File: {filePath}");
        await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken); // Simulate network/blob I/O
    }
}

/// <summary>
/// Specifies the type of Azure storage container for image uploads
/// </summary>
public enum TipoContainerImage
{
    /// <summary>
    /// Container for cropped license plate images
    /// </summary>
    Plate = 0,

    /// <summary>
    /// Container for full scene images
    /// </summary>
    FullImage
}