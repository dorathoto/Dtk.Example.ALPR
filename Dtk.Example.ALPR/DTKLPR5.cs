using DTK.Video;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DTK.LPR
{
    public class DTKLPR5
    {
        const string dllName = "DTKLPR5.dll";
        static DTKLPR5()
        {
            var is64 = IntPtr.Size == 8;
            string assembly_path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(DTKLPR5)).Location);
            string library_path = is64 ? assembly_path + "\\x64\\" : assembly_path + "\\x86\\";

            SetDllDirectory(library_path);
            IntPtr res = LoadLibrary(dllName);
            SetDllDirectory("");
            if (res == IntPtr.Zero)
            {
                LoadLibrary(dllName);
            }
        }

        #region DLL Imports

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        // =====================
        // Callbacks
        // =====================

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void LicensePlateDetectedNative(IntPtr engine, IntPtr plate);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void FrameProcessingCompletedNative(IntPtr engine, long customData, int status);

        // =====================
        // LPREngine
        // =====================
        [DllImport(dllName)]
        public static extern IntPtr LPREngine_Create(IntPtr hParams, [MarshalAs(UnmanagedType.U1)] bool bVideo, IntPtr plateDetectedCallback);

        [DllImport(dllName)]
        public static extern void LPREngine_Destroy(IntPtr hEngine);

        [DllImport(dllName)]
        public static extern void LPREngine_SetFrameProcessingCompletedCallback(IntPtr hEngine, IntPtr prameProcessingCompletedCallback);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern IntPtr LPREngine_ReadFromFile(IntPtr hEngine, string fileName);

        [DllImport(dllName)]
        public static extern IntPtr LPREngine_ReadFromMemFile(IntPtr hEngine, IntPtr pBuffer, int bufferSize);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern IntPtr LPREngine_ReadFromURL(IntPtr hEngine, string url);

        [DllImport(dllName)]
        public static extern IntPtr LPREngine_ReadFromImageBuffer(IntPtr hEngine, IntPtr pBuffer, int width, int height, int stride, PIXFMT pixelFormat);

        [DllImport(dllName)]
        public static extern int LPREngine_PutFrameImageBuffer(IntPtr hEngine, IntPtr pBuffer, int width, int height, int stride, PIXFMT pixelFormat, long timestamp, long customData);

        [DllImport(dllName)]
        public static extern int LPREngine_PutFrame(IntPtr hEngine, IntPtr hFrame, long customData);

        [DllImport(dllName)]
        public static extern int LPREngine_GetProcessingFPS(IntPtr hEngine);

        [DllImport(dllName)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool LPREngine_IsQueueEmpty(IntPtr hEngine);

        [DllImport(dllName)]
        public static extern int LPREngine_IsLicensed(IntPtr hEngine);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPREngine_GetSupportedCountries(StringBuilder buffer, int bufferSize);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPREngine_GetLibraryVersion(StringBuilder buffer, int bufferSize);

        // ==================
        // LPRParams
        // ==================
        [DllImport(dllName)]
        public static extern IntPtr LPRParams_Create();

        [DllImport(dllName)]
        public static extern void LPRParams_Destroy(IntPtr hParams);

        [DllImport(dllName)]
        public static extern int LPRParams_get_MinPlateWidth(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_MinPlateWidth(IntPtr hParams, int val);

        [DllImport(dllName)]
        public static extern int LPRParams_get_MaxPlateWidth(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_MaxPlateWidth(IntPtr hParams, int val);

        [DllImport(dllName)]
        public static extern int LPRParams_get_RotateAngle(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_RotateAngle(IntPtr hParams, int val);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPRParams_get_Countries(IntPtr hParams, StringBuilder val, int val_len);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern void LPRParams_set_Countries(IntPtr hParams, string val);

        [DllImport(dllName)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool LPRParams_get_FormatPlateText(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_FormatPlateText(IntPtr hParams, [MarshalAs(UnmanagedType.U1)] bool val);

        [DllImport(dllName)]
        public static extern int LPRParams_get_NumThreads(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_NumThreads(IntPtr hParams, int val);

        [DllImport(dllName)]
        public static extern int LPRParams_get_FPSLimit(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_FPSLimit(IntPtr hParams, int val);

        [DllImport(dllName)]
        public static extern int LPRParams_get_DuplicateResultsDelay(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_DuplicateResultsDelay(IntPtr hParams, int val);

        [DllImport(dllName)]
        public static extern int LPRParams_get_ResultConfirmationsCount(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_ResultConfirmationsCount(IntPtr hParams, int val);

        [DllImport(dllName)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool LPRParams_get_RecognitionOnMotion(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_RecognitionOnMotion(IntPtr hParams, [MarshalAs(UnmanagedType.U1)] bool val);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPRParams_get_BurnFormatString(IntPtr hParmas, StringBuilder val, int val_len);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern void LPRParams_set_BurnFormatString(IntPtr hParmas, string val);

        [DllImport(dllName)]
        public static extern int LPRParams_get_BurnPosition(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_BurnPosition(IntPtr hParams, int val);

        [DllImport(dllName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool LPRParams_get_DrawPlateBox(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_DrawPlateBox(IntPtr hParams, [MarshalAs(UnmanagedType.I1)] bool val);

        [DllImport(dllName)]
        public static extern int LPRParams_get_DrawPlateBoxColor(IntPtr hParams);

        [DllImport(dllName)]
        public static extern void LPRParams_set_DrawPlateBoxColor(IntPtr hParams, int val);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern void LPRParams_GetXOption(IntPtr hParmas, string optionName, StringBuilder val, int val_len);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern void LPRParams_SetXOption(IntPtr hParmas, string optionName, string val);

        [DllImport(dllName)]
        public static extern int LPRParams_GetZonesCount(IntPtr hParams);

        [DllImport(dllName)]
        public static extern int LPRParams_AddZone(IntPtr hParams);

        [DllImport(dllName)]
        public static extern int LPRParams_RemoveZone(IntPtr hParams, int zone);

        [DllImport(dllName)]
        public static extern int LPRParams_GetZonePointsCount(IntPtr hParams, int zone);

        [DllImport(dllName)]
        public static extern void LPRParams_GetZonePoint(IntPtr hParams, int zone, int pointNum, out int x, out int y);

        [DllImport(dllName)]
        public static extern void LPRParams_SetZonePoint(IntPtr hParams, int zone, int pointNum, int x, int y);

        [DllImport(dllName)]
        public static extern int LPRParams_AddZonePoint(IntPtr hParams, int zone, int x, int y);

        [DllImport(dllName)]
        public static extern int LPRParams_RemoveZonePoint(IntPtr hParams, int zone, int pointNum);

        [DllImport(dllName)]
        public static extern int LPRParams_GetZonePointsCountF(IntPtr hParams, int zone);

        [DllImport(dllName)]
        public static extern void LPRParams_GetZonePointF(IntPtr hParams, int zone, int pointNum, out float x, out float y);

        [DllImport(dllName)]
        public static extern void LPRParams_SetZonePointF(IntPtr hParams, int zone, int pointNum, float x, float y);

        [DllImport(dllName)]
        public static extern int LPRParams_AddZonePointF(IntPtr hParams, int zone, float x, float y);

        [DllImport(dllName)]
        public static extern int LPRParams_RemoveZonePointF(IntPtr hParams, int zone, int pointNum);

        // ==================
        // LPRResult
        // ==================

        [DllImport(dllName)]
        public static extern void LPRResult_Destroy(IntPtr hResult);

        [DllImport(dllName)]
        public static extern int LPRResult_GetPlatesCount(IntPtr hResult);

        [DllImport(dllName)]
        public static extern IntPtr LPRResult_GetPlate(IntPtr hResult, int index);

        [DllImport(dllName)]
        public static extern int LPRResult_GetProcessingTime(IntPtr hResult);

        [DllImport(dllName)]
        public static extern int LPRResult_GetErrorCode(IntPtr hResult);

        // ==================
        // LicensePlate
        // ==================

        [DllImport(dllName)]
        public static extern int LicensePlate_Destroy(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetText(IntPtr hPlate, [MarshalAs(UnmanagedType.LPArray)] byte[] text, int maxLen);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LicensePlate_GetCountryCode(IntPtr hPlate, StringBuilder code, int maxLen);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetState(IntPtr hPlate, [MarshalAs(UnmanagedType.LPArray)] byte[] state, int maxLen);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetType(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetNumRows(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetX(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetY(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetWidth(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetHeight(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetSymbolsCount(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetSymbolX(IntPtr hPlate, int index);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetSymbolY(IntPtr hPlate, int index);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetSymbolWidth(IntPtr hPlate, int index);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetSymbolHeight(IntPtr hPlate, int index);

        [DllImport(dllName, CharSet = CharSet.Auto)]
        public static extern char LicensePlate_GetSymbol(IntPtr hPlate, int index);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetSymbolConfidence(IntPtr hPlate, int index);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetSymbolRowNum(IntPtr hPlate, int index);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetConfidence(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetZone(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetDirection(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetMovingDirection(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern long LicensePlate_GetTimestamp(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern long LicensePlate_GetFrameTimestamp(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern int LicensePlate_GetDateTimeString(IntPtr hPlate, StringBuilder buffer, int bufferSize);

        [DllImport(dllName)]
        public static extern long LicensePlate_GetId(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern long LicensePlate_GetCustomData(IntPtr hPlate);

        [DllImport(dllName)]
        public static extern void LicensePlate_GetImageBuffer(IntPtr hPlate, out IntPtr pBuffer, out int width, out int height, out int stride);

        [DllImport(dllName)]
        public static extern void LicensePlate_GetPlateImageBuffer(IntPtr hPlate, out IntPtr pBuffer, out int width, out int height, out int stride);

        [DllImport(dllName)]
        public static extern void LicensePlate_FreeImageBuffer(IntPtr pBuffer);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LicensePlate_SaveImage(IntPtr hPlate, string fileName, int param);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LicensePlate_SavePlateImage(IntPtr hPlate, string fileName, int param);

        // ==================
        // License
        // ==================
        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPREngine_ActivateLicenseOnline(string licenseKey, [MarshalAs(UnmanagedType.LPArray)] byte[] comments);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPREngine_ActivateLicenseOnlineEx(string licenseKey, [MarshalAs(UnmanagedType.LPArray)] byte[] comments, int channels, string security_key);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern void LPREngine_GetActivatedLicenseInfo(StringBuilder licenseKey, int licenseKeyMaxLen, [MarshalAs(UnmanagedType.LPArray)] byte[] comments, int commentsMaxLen, out int channels, out ulong expirationDate);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern void LPREngine_GetActivatedLicenseInfoEx(StringBuilder licenseKey, int licenseKeyMaxLen, [MarshalAs(UnmanagedType.LPArray)] byte[] comments, int commentsMaxLen, out int channels, out ulong expirationDate, StringBuilder usbDongleID, int usbDongleIDMaxLen);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern void LPREngine_GetLicenseInfo(StringBuilder licenseKey, int licenseKeyMaxLen, [MarshalAs(UnmanagedType.LPArray)] byte[] comments, int commentsMaxLen, out int channels, out ulong expirationDate, StringBuilder usbDongleID, int usbDongleIDMaxLen, out int valid);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPREngine_GetSystemID(StringBuilder systemID, int systemIDMaxLen);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPREngine_ActivateLicenseOffline(string activationCode);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern int LPREngine_GetActivationLink(string licenseKey, [MarshalAs(UnmanagedType.LPArray)] byte[] comments, StringBuilder activationLink, int activationLinkMaxLen);

        [DllImport(dllName, CharSet = CharSet.Ansi)]
        public static extern void LPREngine_SetNetLicenseServer(string ip_address, int port);

        [DllImport(dllName)]
        public static extern void LPREngine_ReloadUSBDongles();

        #endregion
        [Obsolete]
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, int count);


        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        internal static extern void MoveMemory(IntPtr dest, IntPtr src, IntPtr byteCount); // Use IntPtr ou UIntPtr para o tamanho para compatibilidade 64-bit

        public static Bitmap CreateBitmapFromBuffer(IntPtr pBuffer, int width, int height, int stride, PIXFMT pixelFormat)
        {
            // Create bitmap and copy pixels data
            PixelFormat format;
            if (pixelFormat == PIXFMT.BGR24)
                format = PixelFormat.Format24bppRgb;
            else if (pixelFormat == PIXFMT.RGB24)
                format = PixelFormat.Format24bppRgb;
            else if (pixelFormat == PIXFMT.GRAYSCALE)
                format = PixelFormat.Format8bppIndexed;
            else
                return null;
            Bitmap bmp = new Bitmap(width, height, format);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            IntPtr dst = bmpData.Scan0;
            IntPtr src = pBuffer;
            for (int i = 0; i < height; i++)
            {
                MoveMemory(dst, src, stride); //new method
                dst = new IntPtr(dst.ToInt64() + bmpData.Stride);
                src = new IntPtr(src.ToInt64() + stride);
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }
    }

    public delegate void LicensePlateDetected(LPREngine engine, LicensePlate plate);
    public delegate void FrameProcessingCompleted(LPREngine engine, long customData, int status);

    public enum BURN_POS
    {
        LEFT_TOP = 0,
        RIGHT_TOP = 1,
        LEFT_BOTTOM = 2,
        RIGHT_BOTTOM = 3,
    }

    public enum PLATE_TYPE
    {
        DARK_ON_LIGHT_BKG = 1,
        LIGHT_ON_DARK_BKG = 2,
    }

    public enum MOVING_DIRECTION
    {
        MV_INCOMMING = 1,
        MV_OUTGOING = 2,
        MV_NO_MOVEMENT = 3,
    }

    /// <summary>
    /// LPRParams class
    /// </summary>
    public class LPRParams
    {
        internal IntPtr hParmas = IntPtr.Zero;

        public LPRParams()
        {
            this.hParmas = DTKLPR5.LPRParams_Create();
        }

        ~LPRParams()
        {
            DTKLPR5.LPRParams_Destroy(this.hParmas);
        }

        public int MinPlateWidth
        {
            get
            {
                return DTKLPR5.LPRParams_get_MinPlateWidth(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_MinPlateWidth(this.hParmas, value);
            }
        }

        public int MaxPlateWidth
        {
            get
            {
                return DTKLPR5.LPRParams_get_MaxPlateWidth(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_MaxPlateWidth(this.hParmas, value);
            }
        }
        public bool FormatPlateText
        {
            get
            {
                return DTKLPR5.LPRParams_get_FormatPlateText(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_FormatPlateText(this.hParmas, value);
            }
        }

        public int RotateAngle
        {
            get
            {
                return DTKLPR5.LPRParams_get_RotateAngle(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_RotateAngle(this.hParmas, value);
            }
        }

        public int FPSLimit
        {
            get
            {
                return DTKLPR5.LPRParams_get_FPSLimit(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_FPSLimit(this.hParmas, value);
            }
        }

        public int DuplicateResultsDelay
        {
            get
            {
                return DTKLPR5.LPRParams_get_DuplicateResultsDelay(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_DuplicateResultsDelay(this.hParmas, value);
            }
        }

        public bool RecognitionOnMotion
        {
            get
            {
                return DTKLPR5.LPRParams_get_RecognitionOnMotion(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_RecognitionOnMotion(this.hParmas, value);
            }
        }

        public int ResultConfirmationsCount
        {
            get
            {
                return DTKLPR5.LPRParams_get_ResultConfirmationsCount(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_ResultConfirmationsCount(this.hParmas, value);
            }
        }

        public int NumThreads
        {
            get
            {
                return DTKLPR5.LPRParams_get_NumThreads(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_NumThreads(this.hParmas, value);
            }
        }

        public string BurnFormatString
        {
            get
            {
                int size = DTKLPR5.LPRParams_get_BurnFormatString(this.hParmas, null, 0);
                StringBuilder builder = new StringBuilder(size + 1);
                DTKLPR5.LPRParams_get_BurnFormatString(this.hParmas, builder, builder.Capacity);
                return builder.ToString();
            }
            set
            {
                DTKLPR5.LPRParams_set_BurnFormatString(this.hParmas, value);
            }
        }

        public int BurnPosition
        {
            get
            {
                return DTKLPR5.LPRParams_get_BurnPosition(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_BurnPosition(this.hParmas, value);
            }
        }

        public bool DrawPlateBox
        {
            get
            {
                return DTKLPR5.LPRParams_get_DrawPlateBox(this.hParmas);
            }
            set
            {
                DTKLPR5.LPRParams_set_DrawPlateBox(this.hParmas, value);
            }
        }

        public Color DrawPlateBoxColor
        {
            get
            {
                int c = DTKLPR5.LPRParams_get_DrawPlateBoxColor(this.hParmas);
                int r = (c >> 16) & 255;
                int g = (c >> 8) & 255;
                int b = c & 255;
                return Color.FromArgb(r, g, b);
            }
            set
            {
                int c = ((value.R << 16) | (value.G << 8) | value.B);
                DTKLPR5.LPRParams_set_DrawPlateBoxColor(this.hParmas, c);
            }
        }

        public int GetZonesCount()
        {
            return DTKLPR5.LPRParams_GetZonesCount(this.hParmas);
        }

        public int AddZone()
        {
            return DTKLPR5.LPRParams_AddZone(this.hParmas);
        }

        public void RemoveZone(int zoneIndex)
        {
            DTKLPR5.LPRParams_RemoveZone(this.hParmas, zoneIndex);
        }

        public int GetZonePointsCount(int zoneIndex)
        {
            return DTKLPR5.LPRParams_GetZonePointsCount(this.hParmas, zoneIndex);
        }

        public void GetZonePoint(int zoneIndex, int pointIndex, out int x, out int y)
        {
            DTKLPR5.LPRParams_GetZonePoint(this.hParmas, zoneIndex, pointIndex, out x, out y);
        }
        public void SetZonePoint(int zoneIndex, int pointIndex, int x, int y)
        {
            DTKLPR5.LPRParams_SetZonePoint(this.hParmas, zoneIndex, pointIndex, x, y);
        }

        public int AddZonePoint(int zoneIndex, int x, int y)
        {
            return DTKLPR5.LPRParams_AddZonePoint(this.hParmas, zoneIndex, x, y);
        }

        public int RemoveZonePoint(int zoneIndex, int pointIndex)
        {
            return DTKLPR5.LPRParams_RemoveZonePoint(this.hParmas, zoneIndex, pointIndex);
        }

        public int GetZonePointsCountF(int zoneIndex)
        {
            return DTKLPR5.LPRParams_GetZonePointsCountF(this.hParmas, zoneIndex);
        }

        public void GetZonePointF(int zoneIndex, int pointIndex, out float x, out float y)
        {
            DTKLPR5.LPRParams_GetZonePointF(this.hParmas, zoneIndex, pointIndex, out x, out y);
        }
        public void SetZonePointF(int zoneIndex, int pointIndex, float x, float y)
        {
            DTKLPR5.LPRParams_SetZonePointF(this.hParmas, zoneIndex, pointIndex, x, y);
        }

        public int AddZonePointF(int zoneIndex, float x, float y)
        {
            return DTKLPR5.LPRParams_AddZonePointF(this.hParmas, zoneIndex, x, y);
        }

        public int RemoveZonePointF(int zoneIndex, int pointIndex)
        {
            return DTKLPR5.LPRParams_RemoveZonePointF(this.hParmas, zoneIndex, pointIndex);
        }

        public string Countries
        {
            get
            {
                int size = DTKLPR5.LPRParams_get_Countries(this.hParmas, null, 0);
                if (size > 0)
                {
                    StringBuilder builder = new StringBuilder(size + 1);
                    DTKLPR5.LPRParams_get_Countries(this.hParmas, builder, builder.Capacity);
                    return builder.ToString();
                }
                return "";
            }
            set
            {
                DTKLPR5.LPRParams_set_Countries(this.hParmas, value);
            }
        }


        public void SetXOption(string optionName, string val)
        {
            DTKLPR5.LPRParams_SetXOption(this.hParmas, optionName, val);
        }

        public string GetXOption(string optionName)
        {
            StringBuilder builder = new StringBuilder(255);
            DTKLPR5.LPRParams_GetXOption(this.hParmas, optionName, builder, 255);
            return builder.ToString();
        }

    }

    /// <summary>
    /// LicensePlate class
    /// </summary>
    public class LicensePlate : IDisposable
    {
        internal IntPtr hPlate = IntPtr.Zero;

        internal LicensePlate(IntPtr hPlate)
        {
            this.hPlate = hPlate;
        }
        ~LicensePlate()
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
            if (hPlate != IntPtr.Zero)
            {
                DTKLPR5.LicensePlate_Destroy(this.hPlate);
                hPlate = IntPtr.Zero;
            }
        }

        public DateTime DateTime
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(this.Timestamp).ToLocalTime();
            }
        }

        public string Text
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return "";
                byte[] data = new byte[32];
                int size = DTKLPR5.LicensePlate_GetText(this.hPlate, data, data.Length);
                return Encoding.UTF8.GetString(data, 0, size);
            }
        }

        public string CountryCode
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return "";
                StringBuilder builder = new StringBuilder(8);
                DTKLPR5.LicensePlate_GetCountryCode(this.hPlate, builder, 8);
                return builder.ToString();
            }
        }
        public string State
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return "";
                byte[] data = new byte[8];
                int size = DTKLPR5.LicensePlate_GetState(this.hPlate, data, data.Length);
                return Encoding.UTF8.GetString(data, 0, size);
            }
        }

        public PLATE_TYPE Type
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return (PLATE_TYPE)DTKLPR5.LicensePlate_GetType(this.hPlate);
            }
        }

        public int NumRows
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetNumRows(this.hPlate);
            }
        }

        public int X
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetX(this.hPlate);
            }
        }

        public int Y
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetY(this.hPlate);
            }
        }

        public int Width
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetWidth(this.hPlate);
            }
        }

        public int Height
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetHeight(this.hPlate);
            }
        }

        public int GetSymbolsCount()
        {
            if (this.hPlate == IntPtr.Zero)
                return 0;
            return DTKLPR5.LicensePlate_GetSymbolsCount(this.hPlate);
        }

        public int GetSymbolX(int index)
        {
            if (this.hPlate == IntPtr.Zero)
                return 0;
            return DTKLPR5.LicensePlate_GetSymbolX(this.hPlate, index);
        }

        public int GetSymbolY(int index)
        {
            if (this.hPlate == IntPtr.Zero)
                return 0;
            return DTKLPR5.LicensePlate_GetSymbolY(this.hPlate, index);
        }

        public int GetSymbolWidth(int index)
        {
            if (this.hPlate == IntPtr.Zero)
                return 0;
            return DTKLPR5.LicensePlate_GetSymbolWidth(this.hPlate, index);
        }

        public int GetSymbolHeight(int index)
        {
            if (this.hPlate == IntPtr.Zero)
                return 0;
            return DTKLPR5.LicensePlate_GetSymbolHeight(this.hPlate, index);
        }

        public char GetSymbol(int index)
        {
            if (this.hPlate == IntPtr.Zero)
                return '?';
            return DTKLPR5.LicensePlate_GetSymbol(this.hPlate, index);
        }

        public int GetSymbolConfidence(int index)
        {
            if (this.hPlate == IntPtr.Zero)
                return 0;
            return DTKLPR5.LicensePlate_GetSymbolConfidence(this.hPlate, index);
        }

        public int GetSymbolRowNum(int index)
        {
            if (this.hPlate == IntPtr.Zero)
                return 0;
            return DTKLPR5.LicensePlate_GetSymbolRowNum(this.hPlate, index);
        }

        public int Confidence
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetConfidence(this.hPlate);
            }
        }

        public int Zone
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetZone(this.hPlate);
            }
        }

        public int Direction
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetDirection(this.hPlate);
            }
        }

        public MOVING_DIRECTION MovingDirection
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return MOVING_DIRECTION.MV_NO_MOVEMENT;
                return (MOVING_DIRECTION)DTKLPR5.LicensePlate_GetMovingDirection(this.hPlate);
            }
        }

        public long Timestamp
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetTimestamp(this.hPlate);
            }
        }

        public long FrameTimestamp
        {
            get
            {
                return DTKLPR5.LicensePlate_GetFrameTimestamp(this.hPlate);
            }
        }

        public string DateTimeString
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return "";
                int size = DTKLPR5.LicensePlate_GetDateTimeString(this.hPlate, null, 0);
                StringBuilder builder = new StringBuilder(size);
                DTKLPR5.LicensePlate_GetDateTimeString(this.hPlate, builder, size);
                return builder.ToString();
            }
        }

        public long Id
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetId(this.hPlate);
            }
        }

        public long CustomData
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return 0;
                return DTKLPR5.LicensePlate_GetCustomData(this.hPlate);
            }
        }

        public int SaveImage(string fileName, int param)
        {
            return DTKLPR5.LicensePlate_SaveImage(this.hPlate, fileName, param);
        }

        public int SavePlateImage(string fileName, int param)
        {
            return DTKLPR5.LicensePlate_SavePlateImage(this.hPlate, fileName, param);
        }

        public Image Image
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return null;
                IntPtr pBuffer;
                int width;
                int height;
                int stride;
                DTKLPR5.LicensePlate_GetImageBuffer(this.hPlate, out pBuffer, out width, out height, out stride);
                if (pBuffer == IntPtr.Zero)
                    return null;
                Bitmap bmp = DTKLPR5.CreateBitmapFromBuffer(pBuffer, width, height, stride, PIXFMT.RGB24);
                DTKLPR5.LicensePlate_FreeImageBuffer(pBuffer);
                return bmp;
            }
        }

        public Image PlateImage
        {
            get
            {
                if (this.hPlate == IntPtr.Zero)
                    return null;
                IntPtr pBuffer;
                int width;
                int height;
                int stride;
                DTKLPR5.LicensePlate_GetPlateImageBuffer(this.hPlate, out pBuffer, out width, out height, out stride);
                if (pBuffer == IntPtr.Zero)
                    return null;
                Bitmap bmp = DTKLPR5.CreateBitmapFromBuffer(pBuffer, width, height, stride, PIXFMT.RGB24);
                DTKLPR5.LicensePlate_FreeImageBuffer(pBuffer);
                return bmp;
            }
        }
    }

    /// <summary>
    /// LPREngine class
    /// </summary>
    public class LPREngine : IDisposable
    {
        internal IntPtr hEngine = IntPtr.Zero;

        private LicensePlateDetected licensePlateDetected_callback = null;
        private FrameProcessingCompleted frameProcessingCompleted_callback = null;
        private DTKLPR5.LicensePlateDetectedNative licensePlateDetected_callback_native = null;
        private DTKLPR5.FrameProcessingCompletedNative frameProcessingCompleted_callback_native = null;

        private LPRParams parameters = null;

        public LPREngine(LPRParams parameters, bool bVideo, LicensePlateDetected callback)
        {
            this.licensePlateDetected_callback = callback;
            this.parameters = parameters;
            this.licensePlateDetected_callback_native = new DTKLPR5.LicensePlateDetectedNative(OnLicensePlateDetectedNative);
            this.frameProcessingCompleted_callback_native = new DTKLPR5.FrameProcessingCompletedNative(OnFrameProcessingCompletedNative);
            this.hEngine = DTKLPR5.LPREngine_Create(parameters.hParmas, bVideo, Marshal.GetFunctionPointerForDelegate(licensePlateDetected_callback_native));
        }
        ~LPREngine()
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
            if (hEngine != IntPtr.Zero)
            {
                DTKLPR5.LPREngine_Destroy(this.hEngine);
                hEngine = IntPtr.Zero;
            }
        }

        public void SetFrameProcessingCompletedCallback(FrameProcessingCompleted callback)
        {
            DTKLPR5.LPREngine_SetFrameProcessingCompletedCallback(this.hEngine, Marshal.GetFunctionPointerForDelegate(this.frameProcessingCompleted_callback_native));
            frameProcessingCompleted_callback = callback;
        }

        private void OnLicensePlateDetectedNative(IntPtr engine, IntPtr hPlate)
        {
            LicensePlate plate = new LicensePlate(hPlate);
            if (this.licensePlateDetected_callback != null)
                this.licensePlateDetected_callback(this, plate);
        }

        private void OnFrameProcessingCompletedNative(IntPtr engine, long customData, int status)
        {
            if (this.frameProcessingCompleted_callback != null)
                this.frameProcessingCompleted_callback(this, customData, status);
        }

        public int GetProcessingFPS()
        {
            return DTKLPR5.LPREngine_GetProcessingFPS(this.hEngine);
        }

        public bool IsQueueEmpty
        {
            get
            {
                return DTKLPR5.LPREngine_IsQueueEmpty(this.hEngine);
            }
        }

        public int IsLicensed
        {
            get
            {
                return DTKLPR5.LPREngine_IsLicensed(this.hEngine);
            }
        }

        private List<LicensePlate> _GetResult(IntPtr hResult, out int processingTime)
        {
            if (hResult == IntPtr.Zero)
                throw new Exception("Error reading image");

            int errorCode = DTKLPR5.LPRResult_GetErrorCode(this.hEngine);
            if (errorCode == 1)
                throw new NoLicenseException("No license");
            else if (errorCode == 2)
                throw new NoChannelsException("No channels available");

            List<LicensePlate> res = new List<LicensePlate>();
            int count = DTKLPR5.LPRResult_GetPlatesCount(hResult);
            for (int i = 0; i < count; i++)
            {
                IntPtr hPlate = DTKLPR5.LPRResult_GetPlate(hResult, i);
                res.Add(new LicensePlate(hPlate));
            }
            processingTime = DTKLPR5.LPRResult_GetProcessingTime(hResult);
            return res;
        }

        public List<LicensePlate> ReadFromFile(string fileName)
        {
            int dummy;
            return ReadFromFile(fileName, out dummy);
        }
        public List<LicensePlate> ReadFromFile(string fileName, out int processingTime)
        {
            IntPtr hResult = DTKLPR5.LPREngine_ReadFromFile(this.hEngine, fileName);
            List<LicensePlate> res = _GetResult(hResult, out processingTime);
            DTKLPR5.LPRResult_Destroy(hResult);
            return res;
        }

        public List<LicensePlate> ReadFromMemFile(IntPtr pBuffer, int bufferSize)
        {
            int dummy;
            return ReadFromMemFile(pBuffer, bufferSize, out dummy);
        }

        public List<LicensePlate> ReadFromMemFile(IntPtr pBuffer, int bufferSize, out int processingTime)
        {
            IntPtr hResult = DTKLPR5.LPREngine_ReadFromMemFile(this.hEngine, pBuffer, bufferSize);
            List<LicensePlate> res = _GetResult(hResult, out processingTime);
            DTKLPR5.LPRResult_Destroy(hResult);
            return res;
        }

        public List<LicensePlate> ReadFromURL(string url)
        {
            int dummy;
            return ReadFromURL(url, out dummy);
        }

        public List<LicensePlate> ReadFromURL(string url, out int processingTime)
        {
            IntPtr hResult = DTKLPR5.LPREngine_ReadFromURL(this.hEngine, url);
            List<LicensePlate> res = _GetResult(hResult, out processingTime);
            DTKLPR5.LPRResult_Destroy(hResult);
            return res;
        }

        public List<LicensePlate> ReadFromImageBuffer(IntPtr pBuffer, int width, int height, int stride, PIXFMT pixelFormat)
        {
            int dummy;
            return ReadFromImageBuffer(pBuffer, width, height, stride, pixelFormat, out dummy);
        }

        public List<LicensePlate> ReadFromImageBuffer(IntPtr pBuffer, int width, int height, int stride, PIXFMT pixelFormat, out int processingTime)
        {
            IntPtr hResult = DTKLPR5.LPREngine_ReadFromImageBuffer(this.hEngine, pBuffer, width, height, stride, pixelFormat);
            List<LicensePlate> res = _GetResult(hResult, out processingTime);
            DTKLPR5.LPRResult_Destroy(hResult);
            return res;
        }

        public int PutFrame(VideoFrame frame, long customData)
        {
            return DTKLPR5.LPREngine_PutFrame(this.hEngine, frame.hFrame, customData);
        }

        public int PutFrameImageBuffer(IntPtr pBuffer, int width, int height, int stride, PIXFMT pixelFormat, long timestamp, long customData)
        {
            return DTKLPR5.LPREngine_PutFrameImageBuffer(this.hEngine, pBuffer, width, height, stride, pixelFormat, timestamp, customData);
        }

        public static List<string> GetSupportedCountries()
        {
            int required_size = DTKLPR5.LPREngine_GetSupportedCountries(null, 0);
            StringBuilder builder = new StringBuilder(required_size);
            DTKLPR5.LPREngine_GetSupportedCountries(builder, required_size);
            List<string> res = new List<string>();
            foreach (string item in builder.ToString().Split(';'))
            {
                res.Add(item);
            }
            return res;
        }

        public static string GetLibraryVersion()
        {
            int required_size = DTKLPR5.LPREngine_GetLibraryVersion(null, 0);
            StringBuilder builder = new StringBuilder(required_size);
            DTKLPR5.LPREngine_GetLibraryVersion(builder, required_size);
            return builder.ToString();
        }

        static public int ActivateLicenseOnline(string licenseKey, string comments)
        {
            byte[] comments_utf8 = Encoding.UTF8.GetBytes(comments);
            return DTKLPR5.LPREngine_ActivateLicenseOnline(licenseKey, comments_utf8);
        }
        static public int ActivateLicenseOnlineEx(string licenseKey, string comments, int channels, string security_key)
        {
            byte[] comments_utf8 = Encoding.UTF8.GetBytes(comments);
            return DTKLPR5.LPREngine_ActivateLicenseOnlineEx(licenseKey, comments_utf8, channels, security_key);
        }

        [Obsolete("GetActivatedLicenseInfo is deprecated, please use GetLicenseInfo instead.")]
        static public void GetActivatedLicenseInfo(out string licenseKey, out string comments, out int channels, out DateTime expirationDate)
        {
            string dummy;
            GetActivatedLicenseInfo(out licenseKey, out comments, out channels, out expirationDate, out dummy);
        }
        [Obsolete("GetActivatedLicenseInfo is deprecated, please use GetLicenseInfo instead.")]
        static public void GetActivatedLicenseInfo(out string licenseKey, out string comments, out int channels, out DateTime expirationDate, out string usbDongleID)
        {
            int valid;
            GetLicenseInfo(out licenseKey, out comments, out channels, out expirationDate, out usbDongleID, out valid);
        }
        static public void GetLicenseInfo(out string licenseKey, out string comments, out int channels, out DateTime expirationDate, out string usbDongleID, out int valid)
        {
            StringBuilder licKey = new StringBuilder(512);
            byte[] comm = new byte[255];
            StringBuilder dongleID = new StringBuilder(255);
            ulong expDate_time_t;

            DTKLPR5.LPREngine_GetLicenseInfo(licKey, 512, comm, comm.Length, out channels, out expDate_time_t, dongleID, 255, out valid);

            if (expDate_time_t != 0)
            {
                expirationDate = new DateTime(1970, 1, 1).ToLocalTime().AddSeconds(expDate_time_t);
                if (expirationDate.Year < 2000)
                    expirationDate = DateTime.MaxValue;
            }
            else
            {
                expirationDate = DateTime.MaxValue;
            }
            licenseKey = licKey.ToString();
            comments = Encoding.UTF8.GetString(comm, 0, comm.Length).Replace("\0", "");
            usbDongleID = dongleID.ToString();
        }

        static public string GetSystemID()
        {
            StringBuilder sysID = new StringBuilder(50);
            DTKLPR5.LPREngine_GetSystemID(sysID, 50);
            return sysID.ToString();
        }

        static public int ActivateLicenseOffline(string activationCode)
        {
            return DTKLPR5.LPREngine_ActivateLicenseOffline(activationCode);
        }

        static public string GetActivationLink(string licenseKey, string comments)
        {
            StringBuilder actLink = new StringBuilder(255);
            byte[] comments_utf8 = Encoding.UTF8.GetBytes(comments);
            if (DTKLPR5.LPREngine_GetActivationLink(licenseKey, comments_utf8, actLink, 255) > 0)
            {
                return actLink.ToString();
            }
            return "";
        }

        static public void SetNetLicenseServer(string ip_address, int port)
        {
            DTKLPR5.LPREngine_SetNetLicenseServer(ip_address, port);
        }

        static public void ReloadUSBDongles()
        {
            DTKLPR5.LPREngine_ReloadUSBDongles();
        }
    }

    public class NoLicenseException : Exception
    {
        public NoLicenseException()
        {
        }

        public NoLicenseException(string message)
            : base(message)
        {
        }

        public NoLicenseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class NoChannelsException : NoLicenseException
    {
        public NoChannelsException()
        {
        }

        public NoChannelsException(string message)
            : base(message)
        {
        }

        public NoChannelsException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
