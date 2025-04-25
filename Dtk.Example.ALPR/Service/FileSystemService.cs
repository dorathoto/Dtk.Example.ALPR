namespace Dtk.Example.ALPR.Service;

/// <summary>
/// Provides file system related utility methods.
/// </summary>
public class FileSystemService
{
    /// <summary>
    /// Deletes the temporary file after processing (upload)
    /// </summary>
    /// <param name="filePath">The path of the file to be deleted.</param>
    public static void DeleteTempFile(string? filePath)
    {
        // Check if the file path is provided and the file exists.
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                // The actual file deletion is commented out.
                // File.Delete(filePath);

                // Log message indicating the (simulated) deletion of the temporary file.
                Console.WriteLine($"[DeleteTempFile] Temporary file deleted: {filePath}");
            }
            catch (Exception delEx)
            {
                // Log an error message if an exception occurs during the process.
                Console.WriteLine($"[DeleteTempFile] ERROR deleting temporary file {filePath}: {delEx.Message}");
            }
        }
    }
}