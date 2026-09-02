using System.Globalization;

namespace qr2l.Core;

/// <summary>
/// Colore RGB indipendente da qualsiasi libreria grafica, così l'API di Core resta portabile.
/// </summary>
public readonly record struct QrColor(byte R, byte G, byte B)
{
    public static readonly QrColor Black = new(0, 0, 0);
    public static readonly QrColor White = new(255, 255, 255);

    /// <summary>
    /// Forma esadecimale "#RRGGBB", come attesa da SVG e CSS.
    /// </summary>
    public string ToHex()
    {
        return $"#{R:X2}{G:X2}{B:X2}";
    }

    /// <summary>
    /// Accetta "RRGGBB" o "#RRGGBB"; restituisce null se il formato non è valido.
    /// </summary>
    public static QrColor? TryParseHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) {
            return null;
        }

        string value = hex.Trim().TrimStart('#');

        if (value.Length != 6) {
            return null;
        }

        if (byte.TryParse(value.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
            byte.TryParse(value.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
            byte.TryParse(value.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b)) {
            return new QrColor(r, g, b);
        }

        return null;
    }
}

public class QrCodeOptions
{
    #region Properties

    public QrColor darkColor { get; set; } = QrColor.Black;
    public QrColor lightColor { get; set; } = QrColor.White;
    public ErrorCorrectionLevel errorCorrection { get; set; } = ErrorCorrectionLevel.Medium;

    /// <summary>
    /// Logo da inserire al centro: i byte del file immagine (PNG, JPEG, WebP, ...).
    /// </summary>
    public byte[]? logo { get; set; }

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
    WebP,
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
            ExportFormat.WebP => "webp",
            ExportFormat.PostScript => "ps",
            _ => "dat"
        };
    }
}

#endregion
