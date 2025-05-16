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


    //variable for restart processor
    private int _restartAttempts = 0;
    private const int MAX_RESTART_ATTEMPTS = 5; // Ou configurável via appsettings.json
    private bool _isRestarting = false;
    private readonly object _restartLock = new object();
    private volatile bool _isDisposing = false; // Adicionar flag





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
                //string licenseErrorMessage = string.Empty;
                //switch (licenseStatus)
                //{
                //    case 0:
                //        break;
                //    case 1:
                //        licenseErrorMessage = "do not have a valid license";
                //        break;
                //    case 2:
                //        licenseErrorMessage = "valid license but no channel available";
                //        break;
                //    case 3:
                //        licenseErrorMessage = "unable to validate test license";
                //        break;
                //    default:
                //        licenseErrorMessage = "license status unknown";
                //        break;
                //}

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
            string cleanedPlate = plate.Text.Replace("-", "").ToUpperInvariant(); //format ABC1234 - uppercase and remove dash
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
            
            //if (cleanedPlate.Length != 7)
            //{
            //    // Log or handle invalid plate length
            //}

            // Package detection data for processing
            var plateInfo = new LicensePlateInfo
            {
                EventId = eventId,
                Text = cleanedPlate,
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
    public async void OnCaptureError(VideoCapture videoCap, ERR_CAPTURE errorCode, object customObject)
    {
        // If cancellation has been requested or the object is being disposed, exit the method.
        if (_cancellationToken.IsCancellationRequested || _isDisposing) return;

        // Checks if the error code indicates an End Of File, a frame reading error, or an error opening the video.
        // These are typically recoverable errors that might warrant a restart attempt.
        if (errorCode == ERR_CAPTURE.EOF || errorCode == ERR_CAPTURE.READ_FRAME || errorCode == ERR_CAPTURE.OPEN_VIDEO)
        {
            bool performRestart = false;
            int currentAttempt = 0;

            // Thread-safe block to check and update restart state.
            lock (_restartLock)
            {
                // Only attempts to restart if it is not already restarting, is not disposing,
                // and has not reached the maximum restart attempt limit.
                if (!_isRestarting && !_isDisposing && _restartAttempts < MAX_RESTART_ATTEMPTS)
                {
                    _isRestarting = true; // Mark that a restart process is now in progress.
                    _restartAttempts++;  // Increment the count of restart attempts.
                    currentAttempt = _restartAttempts; // Store the current attempt number.
                    performRestart = true;    // Set flag to proceed with the restart logic.
                }
            }

            // If the conditions for a restart are met.
            if (performRestart)
            {
                // Calculate delay with exponential backoff using base 4 (e.g., 4s, 16s, 64s, 256s, 960s).
                // The delay is capped at 960 seconds.
                int delaySeconds = (int)Math.Min(Math.Pow(4, currentAttempt), 960);
                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Capture error ({errorCode}). Attempting to restart in {delaySeconds} seconds... (Attempt {currentAttempt}/{MAX_RESTART_ATTEMPTS})");

                try
                {
                    // 1. Safely stop the current capture, if active.
                    if (_videoCapture != null)
                    {
                        Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Stopping VideoCapture before restarting...");
                        _videoCapture.StopCapture(); // May throw ObjectDisposedException if already disposed.
                    }

                    // 2. Wait for the calculated delay, respecting cancellation.
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), _cancellationToken);

                    // Re-check conditions after the delay, before actually attempting reinitialization.
                    if (_cancellationToken.IsCancellationRequested || _isDisposing)
                    {
                        Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Restart aborted (cancellation or dispose) after delay.");
                        lock (_restartLock) { _isRestarting = false; } // Release the restart lock.
                        return;
                    }

                    Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Attempting to restart capture now (Attempt {currentAttempt}/{MAX_RESTART_ATTEMPTS})...");

                    // 3. Completely dispose of the old VideoCapture instance.
                    _videoCapture?.Dispose(); // Calls Dispose to release native resources.
                    _videoCapture = null;     // Set to null so it will be recreated.

                    // 4. Check the LPREngine before proceeding.
                    if (_engine == null)
                    {
                        Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] CRITICAL: LPREngine is null. Cannot restart VideoCapture. Aborting restart for this camera.");
                        lock (_restartLock)
                        {
                            _isRestarting = false;
                            // Mark as a permanent failure to avoid further attempts if the engine failed.
                            _restartAttempts = MAX_RESTART_ATTEMPTS;
                        }
                        return;
                    }

                    // Checking for a license bug, not sure if it can happen, but for assurance.
                    int licenseStatus = _engine.IsLicensed;
                    if (licenseStatus != 0) // 0 typically means licensed and OK.
                    {
                        Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] CRITICAL: LPREngine is not licensed (Status: {licenseStatus}). Cannot restart VideoCapture. Aborting restart for this camera.");
                        lock (_restartLock)
                        {
                            _isRestarting = false;
                            // Mark as a permanent failure.
                            _restartAttempts = MAX_RESTART_ATTEMPTS;
                        }
                        return;
                    }

                    // 5. Recreate and restart VideoCapture (the LPREngine is reused).
                    _videoCapture = new VideoCapture(HandleFrameCaptured, OnCaptureError, _engine); // Reuses the same _engine.
                    int captureStartResult = _videoCapture.StartCaptureFromIPCamera(_cameraUrl);

                    if (captureStartResult == 0) // 0 typically indicates success.
                    {
                        Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Capture RESTARTED successfully after error (Attempt {currentAttempt}).");
                        lock (_restartLock)
                        {
                            _restartAttempts = 0; // Reset counter after success.
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Failed to RESTART VideoCapture. DTK error code: {captureStartResult}. (Attempt {currentAttempt}/{MAX_RESTART_ATTEMPTS})");
                    }
                }
                catch (OperationCanceledException) // If Task.Delay is canceled.
                {
                    Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Restart attempt canceled during delay or operation.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Unexpected exception during restart attempt (Attempt {currentAttempt}). Exception: {ex.Message}");
                }
                finally
                {
                    // Always release the restart flag in a thread-safe manner.
                    lock (_restartLock)
                    {
                        _isRestarting = false;
                    }
                }
            }
            // This block is executed if a restart was not performed, either because it's already restarting,
            // being disposed, or the maximum restart attempts have been reached.
            else if (!_isDisposing && _restartAttempts >= MAX_RESTART_ATTEMPTS)
            {
                Console.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}][{_cameraUrl}] Maximum restart attempts ({MAX_RESTART_ATTEMPTS}) reached for error {errorCode}. Giving up on restarting this camera automatically. Manual intervention required.");
                // Logic can be added here to notify an external monitoring system
                // or mark this camera as "permanently offline" within the application,
                // so it no longer consumes resources trying to process it.
            }
        }
        // If the error code is not one of the specified recoverable errors, it's logged by the caller or handled elsewhere.
        // No specific action is taken in this method for other error codes.
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