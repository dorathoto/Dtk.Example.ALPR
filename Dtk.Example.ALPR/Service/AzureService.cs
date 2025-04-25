namespace Dtk.Example.ALPR.Service;

public static class AzureService
{

    public static async Task UploadBlobAsync(Guid eventId, string filePath, TipoContainerImage tipoContainer, CancellationToken cancellationToken)
    {

        // TODO: Implementar lógica de upload para Azure Blob Storage
        // 1. Obter credenciais e cliente Blob (BlobServiceClient, BlobContainerClient)
        // 2. Obter referência ao blob (ex: containerClient.GetBlobClient($"{eventId}.jpg"))
        // 3. Fazer upload do arquivo (ex: await blobClient.UploadAsync(filePath, true, cancellationToken))
        Console.WriteLine($"[AzureService] Simulando UPLOAD Blob para EventId: {eventId} do arquivo: {filePath}");
        await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken); // Simular I/O de rede/blob
    }

}
public enum TipoContainerImage
{
    Plate = 0,
    FullImage
}
