namespace Dtk.Example.ALPR;

public class LicensePlateInfo
{
    public Guid EventId { get; set; }
    public string Text { get; set; }
    public string CountryCode { get; set; }
    public double Confidence { get; set; }
    public string CameraUrl { get; set; }
    public int Direction { get; set; }
    public DateTime Timestamp { get; set; }

    public byte[]? PlateImageData { get; set; }
    public byte[]? ImageData { get; set; }
}
