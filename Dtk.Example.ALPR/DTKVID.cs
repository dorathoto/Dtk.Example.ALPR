using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DTK.Video
{
    internal class DTKVID
    {
        const string dllName = "DTKVID.dll";
        static DTKVID()
        {
            var is64 = IntPtr.Size == 8;
            string assembly_path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(DTKVID)).Location);
            string library_path = is64 ? assembly_path + "\\x64\\" : assembly_path + "\\x86\\";
            SetDllDirectory(library_path);
            IntPtr res = LoadLibrary(dllName);
            SetDllDirectory("");
            if (res == IntPtr.Zero)
                LoadLibrary(dllName);
        }

        #region DLL Imports

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        // =====================
        // Callbacks
        // =====================

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void FrameCapturedNative(IntPtr videoCap, IntPtr frame, IntPtr customObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void CaptureErrorNative(IntPtr videoCap, ERR_CAPTURE errorCode, IntPtr customObject);

        // =====================
        // Video Capture
        // =====================

        [DllImport(dllName)]
        internal static extern IntPtr VideoCapture_Create(IntPtr frameCapturedCallback, IntPtr captureErrorCallback, IntPtr customObject);

        [DllImport(dllName)]
        internal static extern void VideoCapture_Destroy(IntPtr hVideoCapture);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        internal static extern int VideoCapture_StartCaptureFromFile(IntPtr hVideoCapture, string fileName, int repeat_count);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        internal static extern int VideoCapture_StartCaptureFromIPCamera(IntPtr hVideoCapture, string ipCameraURL);

        [DllImport(dllName)]
        internal static extern int VideoCapture_StartCaptureFromDevice(IntPtr hVideoCapture, int deviceIndex, int captureWidth, int captureHeight);

        [DllImport(dllName)]
        internal static extern int VideoCapture_StopCapture(IntPtr hVideoCapture);

        [DllImport(dllName)]
        internal static extern int VideoCapture_GetVideoWidth(IntPtr hVideoCapture);

        [DllImport(dllName)]
        internal static extern int VideoCapture_GetVideoHeight(IntPtr hVideoCapture);

        [DllImport(dllName)]
        internal static extern int VideoCapture_GetVideoFPS(IntPtr hVideoCapture);

        [DllImport(dllName)]
        internal static extern int VideoCapture_GetVideoFOURCC(IntPtr hVideoCapture);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int VideoCapture_GetLibraryVersion(StringBuilder buffer, int bufferSize);

        // =====================
        // VideoFrame
        // =====================

        [DllImport(dllName)]
        internal static extern void VideoFrame_Destroy(IntPtr hFrame);

        [DllImport(dllName)]
        internal static extern int VideoFrame_GetWidth(IntPtr hFrame);

        [DllImport(dllName)]
        internal static extern int VideoFrame_GetHeight(IntPtr hFrame);

        [DllImport(dllName)]
        internal static extern UInt64 VideoFrame_Timestamp(IntPtr hFrame);

        [DllImport(dllName)]
        public static extern void VideoFrame_GetImageBuffer(IntPtr hFrame, int format, out IntPtr pBuffer, out int width, out int height, out int stride);

        [DllImport(dllName)]
        public static extern void VideoFrame_FreeImageBuffer(IntPtr pBuffer);

        #endregion

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        internal static extern void CopyMemory(IntPtr dest, IntPtr src, int count);
    }

    public delegate void FrameCaptured(VideoCapture videoCap, VideoFrame frame, object customObject);

    public delegate void CaptureError(VideoCapture videoCap, ERR_CAPTURE errorCode, object customObject);

    public enum PIXFMT
    {
        GRAYSCALE = 1,
        RGB24 = 2,
        BGR24 = 3,
        YUV420 = 4,
    }
    public enum ERR_CAPTURE
    {
        OPEN_VIDEO = 1,
        READ_FRAME = 2,
        EOF = 3,
    }

    /// <summary>
    /// VideoCapture class
    /// </summary>
    public class VideoCapture : IDisposable
    {
        internal IntPtr hVideoCapture = IntPtr.Zero;

        private FrameCaptured frameCapturedCallback = null;
        private CaptureError captureErrorCallback = null;
        private DTKVID.FrameCapturedNative navive_frameCapturedCallback = null;
        private DTKVID.CaptureErrorNative navive_captureErrorCallback = null;
        private object customObject;

        public VideoCapture(FrameCaptured frameCapturedCallback, CaptureError captureErrorCallback, object customObject)
        {
            this.frameCapturedCallback = frameCapturedCallback;
            this.captureErrorCallback = captureErrorCallback;
            this.navive_frameCapturedCallback = new DTKVID.FrameCapturedNative(OnFrameCapturedNative);
            this.navive_captureErrorCallback = new DTKVID.CaptureErrorNative(OnCaptureErrorNative);
            this.customObject = customObject;

            this.hVideoCapture = DTKVID.VideoCapture_Create(
                Marshal.GetFunctionPointerForDelegate(navive_frameCapturedCallback),
                Marshal.GetFunctionPointerForDelegate(navive_captureErrorCallback),
                IntPtr.Zero);
        }
        ~VideoCapture()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing == true)
            {
            }
            else
            {
            }
            if (hVideoCapture != IntPtr.Zero)
            {
                DTKVID.VideoCapture_StopCapture(this.hVideoCapture);
                DTKVID.VideoCapture_Destroy(this.hVideoCapture);
                hVideoCapture = IntPtr.Zero;
            }
        }

        private void OnFrameCapturedNative(IntPtr hVideoCapture, IntPtr frame, IntPtr notUsed)
        {
            if (this.frameCapturedCallback != null)
                this.frameCapturedCallback(this, new VideoFrame(frame), this.customObject);
        }

        private void OnCaptureErrorNative(IntPtr hVideoCapture, ERR_CAPTURE errorCode, IntPtr notUsed)
        {
            if (this.captureErrorCallback != null)
                this.captureErrorCallback(this, errorCode, this.customObject);
        }

        public int StartCaptureFromFile(string fileName, int repeat_count)
        {
            return DTKVID.VideoCapture_StartCaptureFromFile(this.hVideoCapture, fileName, repeat_count);
        }

        public int StartCaptureFromIPCamera(string ipCameraURL)
        {
            return DTKVID.VideoCapture_StartCaptureFromIPCamera(this.hVideoCapture, ipCameraURL);
        }

        public int StartCaptureFromDevice(int deviceIndex, int captureWidth, int captureHeight)
        {
            return DTKVID.VideoCapture_StartCaptureFromDevice(this.hVideoCapture, deviceIndex, captureWidth, captureHeight);
        }

        public int GetVideoWidth()
        {
            return DTKVID.VideoCapture_GetVideoWidth(this.hVideoCapture);
        }
        public int GetVideoHeight()
        {
            return DTKVID.VideoCapture_GetVideoHeight(this.hVideoCapture);
        }
        public int GetVideoFPS()
        {
            return DTKVID.VideoCapture_GetVideoFPS(this.hVideoCapture);
        }
        public int GetVideoFOURCC()
        {
            return DTKVID.VideoCapture_GetVideoFOURCC(this.hVideoCapture);
        }
        public int StopCapture()
        {
            return DTKVID.VideoCapture_StopCapture(this.hVideoCapture);
        }
        public static string GetLibraryVersion()
        {
            int required_size = DTKVID.VideoCapture_GetLibraryVersion(null, 0);
            StringBuilder builder = new StringBuilder(required_size);
            DTKVID.VideoCapture_GetLibraryVersion(builder, required_size);
            return builder.ToString();
        }
    }


    /// <summary>
    /// VideoFrame class
    /// </summary>
    public class VideoFrame : IDisposable
    {
        internal IntPtr hFrame = IntPtr.Zero;
        internal VideoFrame(IntPtr hFrame)
        {
            this.hFrame = hFrame;
        }
        ~VideoFrame()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing == true)
            {
            }
            else
            {
            }
        }

        public void Release()
        {
            if (hFrame != IntPtr.Zero)
            {
                DTKVID.VideoFrame_Destroy(this.hFrame);
                hFrame = IntPtr.Zero;
            }
        }

        public int Width
        {
            get
            {
                return DTKVID.VideoFrame_GetWidth(this.hFrame);
            }

        }
        public int Height
        {
            get
            {
                return DTKVID.VideoFrame_GetHeight(this.hFrame);
            }

        }
        public UInt64 Timestamp
        {
            get
            {
                return DTKVID.VideoFrame_Timestamp(this.hFrame);
            }
        }

        public Image GetImage()
        {
            if (this.hFrame == IntPtr.Zero)
                return null;
            IntPtr pBuffer;
            int width;
            int height;
            int stride;
            DTKVID.VideoFrame_GetImageBuffer(this.hFrame, (int)PIXFMT.BGR24, out pBuffer, out width, out height, out stride);
            if (pBuffer == IntPtr.Zero)
                return null;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            IntPtr dst = bmpData.Scan0;
            IntPtr src = pBuffer;
            for (int i = 0; i < height; i++)
            {
                DTKVID.CopyMemory(dst, src, stride);
                dst = new IntPtr(dst.ToInt64() + bmpData.Stride);
                src = new IntPtr(src.ToInt64() + stride);
            }
            bmp.UnlockBits(bmpData);

            DTKVID.VideoFrame_FreeImageBuffer(pBuffer);

            return bmp;
        }
    }
}
