using System.Collections.Concurrent;

namespace Dtk.Example.ALPR.Service;

public class PlateQueueService
{
    public static ConcurrentQueue<LicensePlateInfo> Queue { get; } = new ConcurrentQueue<LicensePlateInfo>();

    /// <summary>
    /// Adiciona um item à fila de processamento.
    /// Chamado pelo callback do CameraProcessor.
    /// </summary>
    public static void Enqueue(LicensePlateInfo plateInfo)
    {
        try
        {
            Queue.Enqueue(plateInfo);
            // Log leve, se necessário
            // Console.WriteLine($"[QueueService] Enqueued: {plateInfo.EventId}. Size: {Queue.Count}");
        }
        catch (Exception ex)
        {
            // Logar erro grave
            Console.WriteLine($"[QueueService] CRITICAL ERROR enqueuing plate {plateInfo.EventId}: {ex.Message}");
            // Considerar métricas ou alertas adicionais aqui
        }
    }
}

