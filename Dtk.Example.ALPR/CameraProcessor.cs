using DTK.LPR;
using DTK.Video;
using System.Drawing;

namespace Dtk.Example.ALPR;

/// <summary>
/// Handles video stream processing for a single camera, performing license plate recognition
/// </summary>
public class CameraProcessor
{
    private readonly string _cameraUrl;
    private readonly LPRParams _lprParams;
    private readonly Action<LicensePlateInfo> _onPlateDetectedCallback;
    private readonly CancellationToken _cancellationToken;

    private LPREngine _engine;
    private VideoCapture _videoCapture;

    /// <summary>
    /// Initializes a new camera processor instance
    /// </summary>
    /// <param name="url">Camera stream URL (RTSP or HTTP)</param>
    /// <param name="parameters">LPR configuration parameters (should be thread-safe if shared)</param>
    /// <param name="plateDetectedCallback">Callback for recognized license plates</param>
    /// <param name="token">Cancellation token for graceful shutdown</param>
    public CameraProcessor(string url, LPRParams parameters, Action<LicensePlateInfo> plateDetectedCallback, CancellationToken token)
    {
        _cameraUrl = url;
        _lprParams = parameters; // Note: Clone if parameters are camera-specific and modified
        _onPlateDetectedCallback = plateDetectedCallback;
        _cancellationToken = token;
    }

    /// <summary>
    /// Starts asynchronous video processing for the camera stream
    /// </summary>
    /// <returns>Task representing the processing operation</returns>
    public Task StartProcessingAsync()
    {
        // Use Task.Run to offload CPU-intensive processing to background thread
        return Task.Run(() =>
        {
            try
            {
                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Starting processor for: {_cameraUrl}");

                // Initialize LPR engine with video processing mode
                _engine = new LPREngine(_lprParams, true, HandleLicensePlateDetected);

                // Validate license status
                int licenseStatus = _engine.IsLicensed;
                if (licenseStatus != 0)
                {
                    Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] LICENSE WARNING: Status={licenseStatus}. Engine may not function properly.");
                }

                // Configure video capture with frame handler and error callback
                _videoCapture = new VideoCapture(HandleFrameCaptured, OnCaptureError, _engine);

                // Register cancellation callback
                _cancellationToken.Register(() => StopCapture());

                // Start IP camera stream processing
                _videoCapture.StartCaptureFromIPCamera(_cameraUrl);

                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Capture started for: {_cameraUrl}");

                // Block until cancellation is requested
                _cancellationToken.WaitHandle.WaitOne();
                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Wait loop ended for: {_cameraUrl} (cancel requested).");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Task canceled for: {_cameraUrl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] CRITICAL ERROR in camera processor {_cameraUrl}");
                // Consider implementing health monitoring/restart logic here
            }
            finally
            {
                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Cleaning resources for: {_cameraUrl}");
                StopCapture(); // Ensure clean shutdown
                _engine?.Dispose();
                _videoCapture?.Dispose();
                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Resources cleaned for: {_cameraUrl}");
            }
        }, _cancellationToken);
    }

    /// <summary>
    /// Handles incoming video frames from the capture device
    /// </summary>
    /// <param name="cap">Video capture source</param>
    /// <param name="frame">Captured video frame</param>
    /// <param name="customObject">Associated LPR engine instance</param>
    private void HandleFrameCaptured(VideoCapture cap, VideoFrame frame, object customObject)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            frame?.Dispose(); // Release frame if shutting down
            return;
        }

        LPREngine engine = (LPREngine)customObject;
        try
        {
            engine?.PutFrame(frame, 0); // Submit frame for LPR processing
        }
        catch (ObjectDisposedException)
        {
            // Ignore during shutdown sequence
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Error processing frame");
        }
        finally
        {
            frame?.Dispose(); // Verify disposal needs with DTK documentation
        }
    }

    /// <summary>
    /// Handles license plate detection events from the LPR engine
    /// </summary>
    /// <param name="engine">LPR engine instance</param>
    /// <param name="plate">Detected license plate data</param>
    private void HandleLicensePlateDetected(LPREngine engine, LicensePlate plate)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            plate?.Dispose();
            return;
        }

        try
        {
            Guid eventId = Guid.NewGuid(); // Consider using RT.Comb.Provider.Sql.Create() for ordered GUIDs

            // Convert full image to JPEG bytes
            byte[]? plateImageData = null;
            byte[]? imageData = null;

            using (Image? img = plate.Image)
            {
                if (img != null)
                {
                    using var ms = new MemoryStream();
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imageData = ms.ToArray();
                }
            }

            // Convert plate close-up image to JPEG bytes
            using (Image? img = plate.PlateImage)
            {
                if (img != null)
                {
                    using var ms = new MemoryStream();
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    plateImageData = ms.ToArray();
                }
            }

            // Package detection data for processing
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
                ImageData = imageData
            };

            _onPlateDetectedCallback?.Invoke(plateInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Error handling detected plate ({plate.Text}): {ex.Message}");
        }
        finally
        {
            plate?.Dispose();
        }
    }

    /// <summary>
    /// Handles video capture error events
    /// </summary>
    /// <param name="videoCap">Video capture source</param>
    /// <param name="errorCode">Error type</param>
    /// <param name="customObject">Associated object</param>
    public void OnCaptureError(VideoCapture videoCap, ERR_CAPTURE errorCode, object customObject)
    {
        if (_cancellationToken.IsCancellationRequested) return;

        Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] CAPTURE ERROR: Code={errorCode}");

        // Implement error-specific recovery logic
        switch (errorCode)
        {
            case ERR_CAPTURE.EOF:
                // Handle end-of-stream scenarios
                break;
            case ERR_CAPTURE.READ_FRAME:
            case ERR_CAPTURE.OPEN_VIDEO:
                // Consider implementing reconnection logic
                break;
        }
    }

    /// <summary>
    /// Stops video capture and releases resources
    /// </summary>
    private void StopCapture()
    {
        try
        {
            _videoCapture?.StopCapture();
            Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Capture stopped for: {_cameraUrl}");
        }
        catch (ObjectDisposedException)
        {
            // Already disposed - no action needed
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Error stopping capture: {ex.Message}");
        }
    }
}