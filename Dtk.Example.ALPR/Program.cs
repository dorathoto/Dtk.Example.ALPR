using Dtk.Example.ALPR.Service;
using DTK.LPR;

namespace Dtk.Example.ALPR
{
    internal class Program
    {
        public static List<string> urlCams { get; set; }
        private static Task _databaseWriterTask;
        private static List<Task> _cameraProcessingTasks = new List<Task>();
        private static CancellationTokenSource _appShutdownTokenSource = new CancellationTokenSource();


        static async Task Main(string[] args)
        {
            Console.WriteLine("Iniciando o serviço de LPR Multi-Câmera...");
            urlCams = DataService.FuncaoRetornaListadeUrls();
            LPRParams parameters = new LPRParams
            {

                Countries = "BR",
                MinPlateWidth = 80,
                MaxPlateWidth = 300,
                DrawPlateBox = true,
                FormatPlateText = true,
                RecognitionOnMotion = true
            };
            Action<LicensePlateInfo> plateDetectedHandler = PlateQueueService.Enqueue;


            Console.WriteLine($"Iniciando Tarefa de Background (DatabaseWriter)...");
            _databaseWriterTask = PersistenceWorker.RunAsync(_appShutdownTokenSource.Token); // Inicia o consumidor da fila


            Console.WriteLine($"Configurando e iniciando processamento para {urlCams.Count} câmeras...");
            foreach (string camUrl in urlCams)
            {
                Console.WriteLine("Configurando processador para {CameraUrl} , RecognitionOnMotion={parameters}", camUrl, parameters.RecognitionOnMotion);
                var processor = new CameraProcessor(
                    camUrl,
                    parameters,
                    plateDetectedHandler,
                    _appShutdownTokenSource.Token
                );

                _cameraProcessingTasks.Add(processor.StartProcessingAsync());
            }
            Console.WriteLine($"Processamento iniciado para {_cameraProcessingTasks.Count} câmeras.");
            Console.WriteLine("Pressione [Enter] para parar o serviço...");
            Console.ReadLine();


            Console.WriteLine("Recebido comando de parada. Solicitando cancelamento...");
            try
            {

                if (!_appShutdownTokenSource.IsCancellationRequested)
                {
                    _appShutdownTokenSource.Cancel();
                }

                await Task.WhenAll(_cameraProcessingTasks);

                if (_databaseWriterTask != null) // Verifica se a tarefa foi iniciada
                {
                    await _databaseWriterTask; // Espera o writer terminar
                }

              //  Log.Verbose("Todas as tarefas de processamento foram concluídas.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Processamento cancelado com sucesso");

            }
            catch (Exception ex)
            {
               // Log.Fatal(ex, "Serviço ALPR terminado inesperadamente!");
            }
            finally
            {
              //  Log.CloseAndFlush();
                _appShutdownTokenSource.Dispose();
            }

            Console.WriteLine("Serviço finalizado.");
        }
    }
}
