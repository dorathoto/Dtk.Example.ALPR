namespace Dtk.Example.ALPR.Service;

internal class DataService
{
    public static List<string> FuncaoRetornaListadeUrls()
    {
        //TODO: Pode ser feito com EntityFramework, já que é carregado apenas 1 vez.

        return new List<string> {
        "rtsp://admin:abcd1234@192.168.1.78:554/Streaming/Channels/101/",
        // "rtsp://admin:abcd1234@192.168.1.78:554/Streaming/Channels/101/"
        };
    }



    public static async Task InsertSQLAsync(LicensePlateInfo info, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DataService] Simulando INSERT SQL para EventId: {info.EventId}, Placa: {info.Text}");
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
    }
}
