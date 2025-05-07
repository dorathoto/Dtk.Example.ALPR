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
            Console.WriteLine("Starting Multi-Camera LPR Service...");
            urlCams = DataService.FuncaoRetornaListadeUrls();
            LPRParams parameters = new LPRParams
            {

                Countries = "BR",       //Change for country (FR, IT, DE, ES, BR, ecc)
                MinPlateWidth = 80,       // Minimum expected plate width in pixels
                MaxPlateWidth = 300,      // Maximum expected plate width in pixels
                DrawPlateBox = true,       // Enable visual plate rectangle overlay
                FormatPlateText = true,    // Apply standard formatting to recognized plates
                RecognitionOnMotion = true // Only process frames with motion detected
            };
            // Register plate detection handler for the queue service
            Action<LicensePlateInfo> plateDetectedHandler = PlateQueueService.Enqueue;

            // Start background task for persisting recognized plates to database
            Console.WriteLine("Starting Background Task (DatabaseWriter)...");
            _databaseWriterTask = PersistenceWorker.RunAsync(_appShutdownTokenSource.Token);

            // Initialize camera processing tasks for each video stream
            Console.WriteLine($"Setting up and starting processing for {urlCams.Count} cameras...");
            foreach (string camUrl in urlCams)
            {
                Console.WriteLine($"Initializing processor for {camUrl}, RecognitionOnMotion={parameters.RecognitionOnMotion}");
                var processor = new CameraProcessor(
                    camUrl,
                    parameters,
                    plateDetectedHandler,
                    _appShutdownTokenSource.Token
                );
                
                await Task.Delay(1000);//without the delay there will be an error with more than 1 camera
                //If you want I can show you how to do something more robust with SemaphoreSlim


                _cameraProcessingTasks.Add(processor.StartProcessingAsync());
            }
            Console.WriteLine($"Processing started for {_cameraProcessingTasks.Count} cameras.");
            Console.WriteLine("Press [Enter] to stop the service...");
            Console.ReadLine();

            // Begin controlled shutdown sequence
            Console.WriteLine("Received stop command. Requesting cancellation...");
            try
            {

                if (!_appShutdownTokenSource.IsCancellationRequested)
                {
                    _appShutdownTokenSource.Cancel();
                }

                // Wait for all camera processors to complete
                await Task.WhenAll(_cameraProcessingTasks);

                // Ensure database writer completes final operations
                if (_databaseWriterTask != null)
                {
                    await _databaseWriterTask;
                }

                // Log.Verbose("All processing tasks have completed successfully.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Processing canceled successfully");
            }
            catch (Exception ex)
            {
                // Log.Fatal(ex, "ALPR service terminated unexpectedly!");
            }
            finally
            {
                // Log.CloseAndFlush();
                _appShutdownTokenSource.Dispose();
            }

            Console.WriteLine("Service shutdown complete.");
        }
    }
}
