using System.Drawing;

namespace qr2l.Core;

public class QrCodeOptions
{
    #region Properties

    public Color darkColor { get; set; } = Color.Black;
    public Color lightColor { get; set; } = Color.White;
    public ErrorCorrectionLevel errorCorrection { get; set; } = ErrorCorrectionLevel.Medium;
    public Image? logo { get; set; }
    public int pixelsPerModule { get; set; } = 20;
    public PixelShape shape { get; set; } = PixelShape.Square;
    public PayloadMode payloadMode { get; set; } = PayloadMode.Auto;
    public WiFiAuthenticationType wifiAuthType { get; set; } = WiFiAuthenticationType.WPA;
    public bool wifiHidden { get; set; } = false;

    #endregion
}

#region Enums

public enum ErrorCorrectionLevel
{
    Low,
    Medium,
    High,
    Maximum
}

public enum PixelShape
{
    Square,
    Circle
}

public enum PayloadMode
{
    Auto,
    Text,
    Url,
    Mail,
    SMS,
    Phone,
    WiFi,
    Geolocation,
    ContactData,
    Event,
    WhatsApp
}

public enum WiFiAuthenticationType
{
    WPA,
    WEP,
    NoPassword
}

public enum ExportFormat
{
    Png,
    Svg,
    Pdf,
    Bmp,
    Jpeg,
    Gif,
    PostScript
}

public static class ExportFormatExtensions
{
    public static string GetExtension(this ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Png => "png",
            ExportFormat.Svg => "svg",
            ExportFormat.Pdf => "pdf",
            ExportFormat.Bmp => "bmp",
            ExportFormat.Jpeg => "jpg",
            ExportFormat.Gif => "gif",
            ExportFormat.PostScript => "ps",
            _ => "dat"
        };
    }
}

#endregion