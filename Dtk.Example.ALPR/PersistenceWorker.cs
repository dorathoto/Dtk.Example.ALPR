using Dtk.Example.ALPR.Service;

namespace Dtk.Example.ALPR;

/// <summary>
/// Background worker responsible for processing license plate data from the queue
/// and persisting to database and cloud storage
/// </summary>
public class PersistenceWorker
{
    private const int MaxItemsToProcessInBatch = 50;
    private const int QueueEmptyDelayMs = 200;

    /// <summary>
    /// Main background task that consumes the plate queue and processes data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
    /// <returns>Task representing the background operation</returns>
    /// <remarks>
    /// Processing strategy:
    /// 1. Processes items in batches for efficiency
    /// 2. Implements graceful shutdown handling
    /// 3. Includes comprehensive error logging
    /// 4. Maintains data consistency through transaction-like patterns
    /// </remarks>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!ShouldTerminate(cancellationToken))
            {
                var batchToProcess = await GetNextBatchAsync(cancellationToken);

                if (batchToProcess.Any())
                {
                    await ProcessBatchAsync(batchToProcess, cancellationToken);
                }
                else
                {
                    await HandleEmptyQueueAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[PersistenceWorker] Database writing task canceled gracefully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceWorker] FATAL ERROR in persistence task: {ex}");
        }
        finally
        {
            LogShutdownStatus();
        }
    }

    /// <summary>
    /// Processes a single license plate record including:
    /// 1. Temporary file storage
    /// 2. Database insertion
    /// 3. Cloud storage upload
    /// 4. Cleanup
    /// </summary>
    private static async Task ProcessSinglePlateAsync(LicensePlateInfo plateInfo, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[ProcessSinglePlate] Processing EventId: {plateInfo.EventId}");

        string? tempFullImagePath = null;
        string? tempPlateImagePath = null;

        try
        {
            // 1. Prepare temporary storage
            string tempDirBase = Path.Combine(Path.GetTempPath(), "LPR_Plates_Temp");
            Directory.CreateDirectory(tempDirBase);

            // 2. Save images temporarily
            tempFullImagePath = await SaveImageTemporarilyAsync(
                plateInfo.ImageData,
                tempDirBase,
                "Full",
                plateInfo.EventId,
                cancellationToken);

            tempPlateImagePath = await SaveImageTemporarilyAsync(
                plateInfo.PlateImageData,
                tempDirBase,
                "Plate",
                plateInfo.EventId,
                cancellationToken);

            // 3. Parallel persistence operations
            var persistenceTasks = new List<Task>
            {
                DataService.InsertSQLAsync(plateInfo, cancellationToken)
            };

            // Add cloud upload tasks if files were created
            if (!string.IsNullOrEmpty(tempFullImagePath))
            {
                persistenceTasks.Add(AzureService.UploadBlobAsync(
                    plateInfo.EventId,
                    tempFullImagePath,
                    TipoContainerImage.FullImage,
                    cancellationToken));
            }

            if (!string.IsNullOrEmpty(tempPlateImagePath))
            {
                persistenceTasks.Add(AzureService.UploadBlobAsync(
                    plateInfo.EventId,
                    tempPlateImagePath,
                    TipoContainerImage.Plate,
                    cancellationToken));
            }

            await Task.WhenAll(persistenceTasks);

            Console.WriteLine($"[ProcessSinglePlate] Successfully processed EventId: {plateInfo.EventId}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[ProcessSinglePlate] Operation canceled for EventId: {plateInfo.EventId}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessSinglePlate] ERROR processing EventId {plateInfo.EventId}: {ex.Message}");
        }
        finally
        {
            // 4. Cleanup temporary files
            FileSystemService.DeleteTempFile(tempFullImagePath);
            FileSystemService.DeleteTempFile(tempPlateImagePath);
        }
    }

    /// <summary>
    /// Saves image data to a temporary file
    /// </summary>
    /// <returns>Full path to the temporary file or null if failed</returns>
    private static async Task<string?> SaveImageTemporarilyAsync(
        byte[]? imageData,
        string baseTempDir,
        string subDir,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (imageData == null || imageData.Length == 0)
        {
            Console.WriteLine($"[PersistenceWorker] Empty image data for EventId {eventId}");
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
            Console.WriteLine($"[PersistenceWorker] File write canceled for {eventId}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersistenceWorker] ERROR saving temp file for {eventId}: {ex.Message}");
            return null;
        }
    }

    #region Helper Methods

    private static bool ShouldTerminate(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested && PlateQueueService.Queue.IsEmpty;
    }

    private static async Task<List<LicensePlateInfo>> GetNextBatchAsync(CancellationToken cancellationToken)
    {
        var batch = new List<LicensePlateInfo>();
        int processedCount = 0;

        while (processedCount < MaxItemsToProcessInBatch &&
               PlateQueueService.Queue.TryDequeue(out var plateInfo))
        {
            if (plateInfo != null)
            {
                batch.Add(plateInfo);
                processedCount++;
            }
        }

        if (batch.Any())
        {
            Console.WriteLine($"[PersistenceWorker] Processing batch of {batch.Count} item(s)...");
        }

        return batch;
    }

    private static async Task ProcessBatchAsync(List<LicensePlateInfo> batch, CancellationToken cancellationToken)
    {
        foreach (var item in batch)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessSinglePlateAsync(item, cancellationToken);
        }
        Console.WriteLine($"[PersistenceWorker] Batch processing completed.");
    }

    private static async Task HandleEmptyQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(QueueEmptyDelayMs), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    private static void LogShutdownStatus()
    {
        int remainingItems = PlateQueueService.Queue.Count;
        Console.WriteLine($"[PersistenceWorker] Shutting down... Remaining queue items: {remainingItems}");

        if (remainingItems > 0)
        {
            Console.WriteLine("[PersistenceWorker] Warning: Items remaining in queue during shutdown");
        }
    }

    #endregion
}