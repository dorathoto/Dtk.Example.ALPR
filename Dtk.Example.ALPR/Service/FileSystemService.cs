namespace Dtk.Example.ALPR.Service;

public class FileSystemService
{
    /// <summary>
    /// Deleta o arquivo do temp após processamento (upload)
    /// </summary>
    /// <param name="filePath"></param>
    public static void DeleteTempFile(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                // File.Delete(filePath);
                Console.WriteLine($"[DeleteTempFile] Arquivo temporário deletado: {filePath}");
            }
            catch (Exception delEx)
            {
                Console.WriteLine($"[DeleteTempFile] ERRO ao deletar arquivo temporário {filePath}: {delEx.Message}");
            }
        }
    }


}

