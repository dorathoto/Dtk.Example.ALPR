using System.Collections.Concurrent;

namespace Dtk.Example.ALPR.Service;

/// <summary>
/// Provides thread-safe queue management for license plate processing
/// </summary>
/// <remarks>
/// This service acts as the buffer between camera processors and persistence workers,
/// ensuring safe cross-thread operations in the ALPR pipeline.
/// </remarks>
public class PlateQueueService
{
    /// <summary>
    /// Thread-safe queue holding license plate recognition results
    /// </summary>
    /// <remarks>
    /// Uses ConcurrentQueue for lock-free enqueue/dequeue operations.
    /// Capacity is only limited by available memory.
    /// </remarks>
    public static ConcurrentQueue<LicensePlateInfo> Queue { get; } = new ConcurrentQueue<LicensePlateInfo>();

    /// <summary>
    /// Adds a license plate recognition result to the processing queue
    /// </summary>
    /// <param name="plateInfo">Recognized plate information</param>
    /// <remarks>
    /// Called by CameraProcessor's detection callback.
    /// Includes comprehensive error handling to prevent pipeline failures.
    /// </remarks>
    public static void Enqueue(LicensePlateInfo plateInfo)
    {
        try
        {
            Queue.Enqueue(plateInfo);
            // Uncomment for debugging queue pressure:
            // Console.WriteLine($"[QueueService] Enqueued: {plateInfo.EventId}. Queue size: {Queue.Count}");
        }
        catch (Exception ex)
        {
            // Log error 
            Console.WriteLine($"[QueueService] CRITICAL ERROR enqueuing plate {plateInfo.EventId}: {ex.Message}");
        }
    }
}

