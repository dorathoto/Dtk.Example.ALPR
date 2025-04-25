using DTK.LPR;
using DTK.Video;
using System.Drawing;

namespace Dtk.Example.ALPR;

public class CameraProcessor
{
    private readonly string _cameraUrl;
    private readonly LPRParams _lprParams;
    private readonly Action<LicensePlateInfo> _onPlateDetectedCallback;
    private readonly CancellationToken _cancellationToken;

    private LPREngine _engine;
    private VideoCapture _videoCapture;

    public CameraProcessor(string url, LPRParams parameters, Action<LicensePlateInfo> plateDetectedCallback, CancellationToken token)
    {
        _cameraUrl = url;
        _lprParams = parameters; // Pode precisar clonar se forem modificados por câmera
        _onPlateDetectedCallback = plateDetectedCallback;
        _cancellationToken = token;
    }

    // Método que inicia o processamento de forma assíncrona
    public Task StartProcessingAsync()
    {
        // Task.Run garante que a inicialização e o loop rodem em background
        return Task.Run(() =>
        {
            try
            {
               Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Iniciando processador para: {_cameraUrl}");

                _engine = new LPREngine(_lprParams, true, HandleLicensePlateDetected); // true = modo vídeo
                int licenseStatus = _engine.IsLicensed;
                if (licenseStatus != 0)
                {
                   Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] ALERTA DE LICENÇA: Status={licenseStatus}. O Engine pode não funcionar corretamente.");
                }

                _videoCapture = new VideoCapture(HandleFrameCaptured, OnCaptureError, _engine);

                _cancellationToken.Register(() => StopCapture());

                // Inicia a captura da câmera IP
                _videoCapture.StartCaptureFromIPCamera(_cameraUrl);

               Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Captura iniciada para: {_cameraUrl}");
                _cancellationToken.WaitHandle.WaitOne(); // Espera pelo sinal de cancelamento
               Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Loop de espera terminado para: {_cameraUrl} (cancelamento solicitado).");

            }
            catch (OperationCanceledException)
            {
                // Ocorre se WaitHandle.WaitOne() for interrompido pelo cancelamento.
               Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Tarefa cancelada para: {_cameraUrl}");
            }
            catch (Exception ex)
            {
                // Captura erros na inicialização ou durante a operação (se StartCapture for bloqueante e lançar erro)
               Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] ERRO CRÍTICO no processador da câmera {_cameraUrl}");
                // Logar erro completo, possivelmente notificar um sistema de monitoramento.
            }
            finally
            {
                // Garante a limpeza dos recursos
               Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Limpando recursos para: {_cameraUrl}");
                StopCapture(); // Garante que a captura seja parada
                _engine?.Dispose();
                _videoCapture?.Dispose();
               Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Recursos limpos para: {_cameraUrl}");
            }
        }, _cancellationToken);
    }



    private void HandleFrameCaptured(VideoCapture cap, VideoFrame frame, object customObject)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            frame?.Dispose(); // Liberar frame se estamos cancelando
            return;
        }

        LPREngine engine = (LPREngine)customObject;
        try
        {
            engine?.PutFrame(frame, 0); // Envia o frame para o motor LPR
        }
        catch (ObjectDisposedException)
        {
            // Ignorar se o engine já foi disposed durante o shutdown
        }
        catch (Exception ex)
        {
           Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Erro ao processar frame");

        }
        finally
        {
            frame?.Dispose();//será que precisa? achar na documentação DTK
        }
    }

    /// <summary>
    /// aqui é onde entende que achou a informação.
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="plate"></param>
    private void HandleLicensePlateDetected(LPREngine engine, LicensePlate plate)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            plate?.Dispose();
            return;
        }

        try
        {
            Guid eventId = Guid.NewGuid();// RT.Comb.Provider.Sql.Create();
            byte[]? plateImageData = null;
            byte[]? ImageData = null;

            using (Image? img = plate.Image)
            {
                if (img != null)
                {
                    using var ms = new MemoryStream();
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    ImageData = ms.ToArray();
                }
            }

            using (Image? img = plate.PlateImage)
            {
                if (img != null)
                {
                    using var ms = new MemoryStream();
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    plateImageData = ms.ToArray();
                }
            }

            var plateInfo = new LicensePlateInfo
            {
                EventId = eventId,
                Text = plate.Text,
                CountryCode = plate.CountryCode,
                Confidence = plate.Confidence,
                Direction = plate.Direction,
                CameraUrl = _cameraUrl,
                Timestamp = DateTime.UtcNow,
                PlateImageData = plateImageData,
                ImageData = ImageData
            };
            // Chama o callback fornecido (SavePlateToDatabase)
            _onPlateDetectedCallback?.Invoke(plateInfo);
        }
        catch (Exception ex)
        {
           Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Erro ao manusear placa detectada ({plate.Text}): {ex.Message}");
        }
        finally
        {
            plate?.Dispose();
        }
    }

    public void OnCaptureError(VideoCapture videoCap, ERR_CAPTURE errorCode, object customObject)
    {
        if (_cancellationToken.IsCancellationRequested) return;
       Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] ERRO DE CAPTURA: Código={errorCode.ToString()}"); // Log o código do erro

        if (errorCode == ERR_CAPTURE.EOF)
        {
            //SetFrame(null);
        }
        if (errorCode == ERR_CAPTURE.READ_FRAME || errorCode == ERR_CAPTURE.OPEN_VIDEO)
        {
            // restartFlag = true;
        }
    }

    private void StopCapture()
    {
        try
        {
            _videoCapture?.StopCapture();
           Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Captura parada para: {_cameraUrl}");
        }
        catch (ObjectDisposedException)
        {
            // Ignorar se o objeto já foi disposed
        }
        catch (Exception ex)
        {
           Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Erro ao parar captura: {ex.Message}");
            // Logar erro
        }
    }
}

