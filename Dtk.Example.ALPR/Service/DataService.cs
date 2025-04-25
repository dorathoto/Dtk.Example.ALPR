namespace Dtk.Example.ALPR.Service;

/// <summary>
/// Provides data access services for the ALPR system
/// </summary>
internal class DataService
{
    /// <summary>
    /// Retrieves camera URLs from configuration
    /// </summary>
    /// <returns>List of RTSP camera URLs</returns>
    /// <remarks>
    /// Production implementation should:
    /// - Use dependency injection
    /// - Cache configuration
    /// - Support dynamic reloading
    /// - Consider EntityFramework for database-backed configuration
    /// </remarks>
    public static List<string> FuncaoRetornaListadeUrls()
    {
        // TODO: Replace with proper configuration source
        // Recommended options:
        // - appsettings.json configuration
        // - Database via EntityFramework
        // - External configuration service

        return new List<string> {
            "rtsp://admin:abcd1234@192.168.1.78:554/Streaming/Channels/101/",
            // Additional cameras can be uncommented as needed
            // "rtsp://admin:abcd1234@192.168.1.78:554/Streaming/Channels/101/"
        };
    }

    /// <summary>
    /// Persists license plate recognition data to the database
    /// </summary>
    /// <param name="info">License plate recognition data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the insert operation</returns>
    /// <remarks>
    /// Production implementation should:
    /// - Use parameterized queries
    /// - Implement retry policy
    /// - Consider bulk insert for high-volume scenarios
    /// </remarks>
    public static async Task InsertSQLAsync(LicensePlateInfo info, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DataService] Simulating SQL INSERT for EventId: {info.EventId}, Plate: {info.Text}");
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken); // Simulate database I/O
    }
}