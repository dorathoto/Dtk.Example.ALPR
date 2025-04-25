using Dtk.Example.ALPR.Service;

namespace Dtk.Example.ALPR;

public class PersistenceWorker
{
    /// <summary>
    /// Tarefa de background que consome a fila _plateQueue e processa os dados.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {

        const int MaxItemsToProcessInBatch = 50;

        try
        {
            while (!cancellationToken.IsCancellationRequested || !PlateQueueService.Queue.IsEmpty)
            {
                int processedCount = 0;
                var batchToProcess = new List<LicensePlateInfo>();

                while (processedCount < MaxItemsToProcessInBatch && PlateQueueService.Queue.TryDequeue(out LicensePlateInfo? plateInfo))
                {
                    if (plateInfo != null) // Verifica se conseguiu retirar um item válido
                    {
                        batchToProcess.Add(plateInfo);
                        processedCount++;
                    }
                }

                // Se pegou itens, processa o lote
                if (batchToProcess.Any())
                {
                    Console.WriteLine($"[RunAsync Início] Processando lote de {batchToProcess.Count} item(s)...");
                    // Processa cada item do lote (poderia ser otimizado para batch insert no DB)
                    foreach (var itemInfo in batchToProcess)
                    {
                        // Checa cancelamento antes de processar cada item, se necessário
                        if (cancellationToken.IsCancellationRequested) break;
                        await ProcessSinglePlateAsync(itemInfo, cancellationToken);
                    }
                    Console.WriteLine($"[RunAsync FIM] Lote concluído.");
                }
                else if (cancellationToken.IsCancellationRequested && PlateQueueService.Queue.IsEmpty)
                {

                    break;// Cancelamento solicitado e fila vazia, pode sair do loop
                }
                else
                {
                    // Fila vazia, espera um pouco antes de checar novamente
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken); // Espera 200ms
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                // Verifica cancelamento novamente ao final do loop
                if (cancellationToken.IsCancellationRequested && PlateQueueService.Queue.IsEmpty) break;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[PersistenceWorker] Tarefa de escrita cancelada.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceWorker] ERRO FATAL na tarefa de escrita: {ex.ToString()}");
        }
        finally
        {
            Console.WriteLine($"[PersistenceWorker] Finalizando... Itens restantes na fila: {PlateQueueService.Queue.Count}");
            // Idealmente, a fila deve estar vazia aqui se o shutdown foi soft.
            // Poderia ter uma última tentativa de processar o restante aqui, se necessário?
        }
    }

    /// <summary>
    /// Processa um único item LicensePlateInfo (escreve temp, db, blob, deleta temp).
    /// </summary>
    private static async Task ProcessSinglePlateAsync(LicensePlateInfo plateInfo, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[ProcessSinglePlateAsync] Processando EventId: {plateInfo.EventId}");
        string? tempFullImagePath = null;
        string? tempPlateImagePath = null;
        bool success = false;

        try
        {


            string tempDirBase = Path.Combine(Path.GetTempPath(), "LPR_Plates_Temp"); // Subdiretório temporário
            Directory.CreateDirectory(tempDirBase);

            //1. Salvar as imagens temporariamente

            // Imagem completa do veículo
            tempFullImagePath = await SaveImageTemporarilyAsync(plateInfo.ImageData,
                                                              tempDirBase,
                                                              "Full",
                                                              plateInfo.EventId,
                                                              cancellationToken);


            // Imagem da placa apenas
            tempPlateImagePath = await SaveImageTemporarilyAsync(plateInfo.PlateImageData,
                                                                 tempDirBase,
                                                                 "Plate",
                                                                 plateInfo.EventId,
                                                                 cancellationToken);


            // 2. Iniciar Tarefas Paralelas (SQL e Blob Upload)
            var tasks = new List<Task>();

            Task dbTask = DataService.InsertSQLAsync(plateInfo, cancellationToken);
            tasks.Add(dbTask);

            var blobTasks = new List<Task>();
            if (!string.IsNullOrEmpty(tempFullImagePath))
            {
                blobTasks.Add(AzureService.UploadBlobAsync(plateInfo.EventId, tempFullImagePath, TipoContainerImage.FullImage, cancellationToken));
            }
            if (!string.IsNullOrEmpty(tempPlateImagePath))
            {
                blobTasks.Add(AzureService.UploadBlobAsync(plateInfo.EventId, tempPlateImagePath, TipoContainerImage.Plate, cancellationToken));
            }
            // Adiciona todas as tarefas de blob à lista principal (se houver alguma)
            if (blobTasks.Count != 0)
            { tasks.AddRange(blobTasks); }

            await Task.WhenAll(tasks); // Espera SQL e/ou Blob

            success = true; // Marcar como sucesso se chegou aqui sem exceções das tarefas
            Console.WriteLine($"[ProcessSinglePlateAsync] Sucesso no processamento DB/Blob para EventId: {plateInfo.EventId}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[ProcessSinglePlateAsync] Operação cancelada durante processamento do EventId: {plateInfo.EventId}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessSinglePlateAsync] ERRO no processamento DB/Blob do EventId {plateInfo.EventId}:");
        }
        finally
        {
            FileSystemService.DeleteTempFile(tempFullImagePath);
            FileSystemService.DeleteTempFile(tempPlateImagePath);
        }
    }

    /// <summary>
    /// Salva os dados de uma imagem em um arquivo temporário dentro de um subdiretório específico.
    /// </summary>
    /// <param name="imageData">Os bytes da imagem.</param>
    /// <param name="baseTempDir">O diretório temporário base.</param>
    /// <param name="subDir">O nome do subdiretório (ex: "Plate", "Full").</param>
    /// <param name="eventId">O Guid para usar como nome do arquivo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O caminho completo do arquivo salvo, ou null se a imagem não foi salva.</returns>
    private static async Task<string?> SaveImageTemporarilyAsync(byte[]? imageData, string baseTempDir, string subDir, Guid eventId, CancellationToken cancellationToken)
    {
        // Verifica se há dados de imagem para salvar
        if (imageData == null || imageData.Length == 0)
        {
            Console.WriteLine($"[PersistenceWorker] EventId {eventId} imagem vazia ou nula. Arquivo não será salvo.");
            return null;
        }
        string? finalFilePath = null;
        try
        {
            string targetDirectory = Path.Combine(baseTempDir, subDir);
            Directory.CreateDirectory(targetDirectory);
            finalFilePath = Path.Combine(targetDirectory, $"{eventId}.jpg");
            await File.WriteAllBytesAsync(finalFilePath, imageData, cancellationToken);
            return finalFilePath;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[PersistenceWorker] Escrita do arquivo cancelada para {eventId}: {finalFilePath ?? "N/A"}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceWorker] ERRO ao salvar arquivo temporário (EventId: {eventId}) em {finalFilePath ?? "N/A"}: {ex.Message}");
            return null;
        }
    }
}
