using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using QRCoder;

namespace qr2l.Core;

public static class QrGenerator
{
    public static byte[] Generate(string text, ExportFormat format, QrCodeOptions? options = null)
    {
        options ??= new QrCodeOptions();

        string payload = PreparePayload(text, options.payloadMode, options);

        if (options.logo != null && options.errorCorrection != ErrorCorrectionLevel.High && options.errorCorrection != ErrorCorrectionLevel.Maximum) {
            options.errorCorrection = ErrorCorrectionLevel.High;
        }

        using var generator = new QRCodeGenerator();
        QRCodeData data = generator.CreateQrCode(payload, ConvertErrorCorrectionLevel(options.errorCorrection));

        return format switch {
            ExportFormat.Png => GeneratePng(data, options),
            ExportFormat.Svg => GenerateSvgBytes(data, options),
            ExportFormat.Pdf => GeneratePdf(data, options),
            ExportFormat.Bmp => GenerateBmp(data, options),
            ExportFormat.Jpeg => GenerateJpeg(data, options),
            ExportFormat.Gif => GenerateGif(data, options),
            ExportFormat.PostScript => GeneratePostScript(data, options),
            var _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    public static string GenerateSvgString(string text, QrCodeOptions? options = null)
    {
        options ??= new QrCodeOptions();

        string payload = PreparePayload(text, options.payloadMode, options);

        if (options.logo != null && options.errorCorrection != ErrorCorrectionLevel.High && options.errorCorrection != ErrorCorrectionLevel.Maximum) {
            options.errorCorrection = ErrorCorrectionLevel.High;
        }

        using var generator = new QRCodeGenerator();
        QRCodeData data = generator.CreateQrCode(payload, ConvertErrorCorrectionLevel(options.errorCorrection));

        var svgQr = new SvgQRCode(data);
        Color darkColor = options.darkColor;
        Color lightColor = options.lightColor;

        return svgQr.GetGraphic(
            pixelsPerModule: 20,
            darkColorHex: $"#{darkColor.R:X2}{darkColor.G:X2}{darkColor.B:X2}",
            lightColorHex: $"#{lightColor.R:X2}{lightColor.G:X2}{lightColor.B:X2}",
            drawQuietZones: true
        );
    }

    private static string PreparePayload(string text, PayloadMode mode, QrCodeOptions? options = null)
    {
        return mode switch {
            PayloadMode.Text => text,
            PayloadMode.Url => text.StartsWith("http://") || text.StartsWith("https://") ? text : $"https://{text}",
            PayloadMode.Mail => PrepareMailPayload(text),
            PayloadMode.SMS => PrepareSmsPayload(text),
            PayloadMode.Phone => PreparePhonePayload(text),
            PayloadMode.WiFi => PrepareWiFiPayload(text, options),
            PayloadMode.Geolocation => PrepareGeolocationPayload(text),
            PayloadMode.ContactData => PrepareContactDataPayload(text),
            PayloadMode.Event => PrepareEventPayload(text),
            PayloadMode.WhatsApp => PrepareWhatsAppPayload(text),
            var _ => text
        };
    }
    
    private static string PrepareMailPayload(string text)
    {
        // Format: email or email;subject;body
        string[] parts = text.Split(';');
        string email = parts[0].Trim();
        string subject = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        string body = parts.Length > 2 ? parts[2].Trim() : string.Empty;
        
        var generator = new PayloadGenerator.Mail(email, subject, body);
        return generator.ToString();
    }
    
    private static string PrepareSmsPayload(string text)
    {
        // Format: number;message
        string[] parts = text.Split(';');
        if (parts.Length < 1) {
            throw new ArgumentException("SMS payload must be in format: number;message");
        }
        
        string number = parts[0].Trim();
        string message = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        
        var generator = new PayloadGenerator.SMS(number, message);
        return generator.ToString();
    }
    
    private static string PreparePhonePayload(string text)
    {
        var generator = new PayloadGenerator.PhoneNumber(text.Trim());
        return generator.ToString();
    }
    
    private static string PrepareGeolocationPayload(string text)
    {
        // Format: latitude,longitude or latitude,longitude,altitude
        string[] parts = text.Split(',');
        if (parts.Length < 2) {
            throw new ArgumentException("Geolocation payload must be in format: latitude,longitude");
        }
        
        string latitude = parts[0].Trim();
        string longitude = parts[1].Trim();
        
        var generator = new PayloadGenerator.Geolocation(latitude, longitude);
        return generator.ToString();
    }
    
    private static string PrepareContactDataPayload(string text)
    {
        // Format: firstName;lastName;phone;email (minimal vCard)
        string[] parts = text.Split(';');
        if (parts.Length < 2) {
            throw new ArgumentException("ContactData payload must be in format: firstName;lastName;phone;email");
        }
        
        string firstName = parts[0].Trim();
        string lastName = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        string phone = parts.Length > 2 ? parts[2].Trim() : string.Empty;
        string email = parts.Length > 3 ? parts[3].Trim() : string.Empty;
        
        var generator = new PayloadGenerator.ContactData(
            PayloadGenerator.ContactData.ContactOutputType.VCard3,
            firstName,
            lastName,
            phone: phone,
            email: email
        );
        return generator.ToString();
    }
    
    private static string PrepareEventPayload(string text)
    {
        // Format: subject;description;location;startDateTime;endDateTime (ISO format for dates)
        string[] parts = text.Split(';');
        if (parts.Length < 3) {
            throw new ArgumentException("Event payload must be in format: subject;description;location;startDateTime;endDateTime");
        }
        
        string subject = parts[0].Trim();
        string description = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        string location = parts.Length > 2 ? parts[2].Trim() : string.Empty;
        DateTime start = parts.Length > 3 ? DateTime.Parse(parts[3].Trim()) : DateTime.Now;
        DateTime end = parts.Length > 4 ? DateTime.Parse(parts[4].Trim()) : start.AddHours(1);
        
        var generator = new PayloadGenerator.CalendarEvent(subject, description, location, start, end, false);
        return generator.ToString();
    }
    
    private static string PrepareWhatsAppPayload(string text)
    {
        // Format: number;message
        string[] parts = text.Split(';');
        if (parts.Length < 1) {
            throw new ArgumentException("WhatsApp payload must be in format: number;message");
        }
        
        string number = parts[0].Trim();
        string message = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        
        var generator = new PayloadGenerator.WhatsAppMessage(number, message);
        return generator.ToString();
    }

    private static string PrepareWiFiPayload(string text, QrCodeOptions? options)
    {
        string[] parts = text.Split(';');

        if (parts.Length < 1 || parts.Length > 2) {
            throw new ArgumentException("WiFi payload must be in format: SSID;password (password optional for open networks)");
        }

        string ssid = parts[0].Trim();
        string password = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        WiFiAuthenticationType authType = options?.wifiAuthType ?? WiFiAuthenticationType.WPA;
        bool hidden = options?.wifiHidden ?? false;

        string authTypeStr = authType switch {
            WiFiAuthenticationType.WPA => "WPA",
            WiFiAuthenticationType.WEP => "WEP",
            WiFiAuthenticationType.NoPassword => "nopass",
            var _ => "WPA"
        };

        string hiddenStr = hidden ? "H:true;" : "";

        if (authType == WiFiAuthenticationType.NoPassword) {
            return $"WIFI:T:{authTypeStr};S:{ssid};{hiddenStr};";
        }

        return $"WIFI:T:{authTypeStr};S:{ssid};P:{password};{hiddenStr};";
    }

    private static QRCodeGenerator.ECCLevel ConvertErrorCorrectionLevel(ErrorCorrectionLevel level)
    {
        return level switch {
            ErrorCorrectionLevel.Low => QRCodeGenerator.ECCLevel.L,
            ErrorCorrectionLevel.Medium => QRCodeGenerator.ECCLevel.M,
            ErrorCorrectionLevel.High => QRCodeGenerator.ECCLevel.Q,
            ErrorCorrectionLevel.Maximum => QRCodeGenerator.ECCLevel.H,
            var _ => QRCodeGenerator.ECCLevel.M
        };
    }

    private static byte[] GeneratePng(QRCodeData data, QrCodeOptions options)
    {
        if (options.shape == PixelShape.Circle) {
            using var qr = new ArtQRCode(data);
            using Bitmap bitmap = qr.GetGraphic(
                pixelsPerModule: options.pixelsPerModule,
                darkColor: options.darkColor,
                lightColor: options.lightColor,
                backgroundColor: options.lightColor,
                pixelSizeFactor: 0.8f,
                drawQuietZones: true
            );
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        } else {
            using var qr = new QRCode(data);
            Bitmap? logoBitmap = options.logo != null ? (Bitmap)options.logo : null;

            using Bitmap bitmap = qr.GetGraphic(
                pixelsPerModule: options.pixelsPerModule,
                darkColor: options.darkColor,
                lightColor: options.lightColor,
                icon: logoBitmap,
                iconSizePercent: 15,
                iconBorderWidth: 0,
                drawQuietZones: true
            );
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    private static byte[] GenerateBmp(QRCodeData data, QrCodeOptions options)
    {
        using var qr = new QRCode(data);
        using Bitmap bitmap = qr.GetGraphic(
            pixelsPerModule: options.pixelsPerModule,
            darkColor: options.darkColor,
            lightColor: options.lightColor,
            drawQuietZones: true
        );
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Bmp);
        return ms.ToArray();
    }

    private static byte[] GenerateJpeg(QRCodeData data, QrCodeOptions options)
    {
        using var qr = new QRCode(data);
        using Bitmap bitmap = qr.GetGraphic(
            pixelsPerModule: options.pixelsPerModule,
            darkColor: options.darkColor,
            lightColor: options.lightColor,
            drawQuietZones: true
        );
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Jpeg);
        return ms.ToArray();
    }

    private static byte[] GenerateGif(QRCodeData data, QrCodeOptions options)
    {
        using var qr = new QRCode(data);
        using Bitmap bitmap = qr.GetGraphic(
            pixelsPerModule: options.pixelsPerModule,
            darkColor: options.darkColor,
            lightColor: options.lightColor,
            drawQuietZones: true
        );
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Gif);
        return ms.ToArray();
    }

    private static byte[] GenerateSvgBytes(QRCodeData data, QrCodeOptions options)
    {
        var svgQr = new SvgQRCode(data);
        Color darkColor = options.darkColor;
        Color lightColor = options.lightColor;

        string svg = svgQr.GetGraphic(
            pixelsPerModule: 20,
            darkColorHex: $"#{darkColor.R:X2}{darkColor.G:X2}{darkColor.B:X2}",
            lightColorHex: $"#{lightColor.R:X2}{lightColor.G:X2}{lightColor.B:X2}",
            drawQuietZones: true
        );

        return Encoding.UTF8.GetBytes(svg);
    }

    private static byte[] GeneratePdf(QRCodeData data, QrCodeOptions options)
    {
        using var qr = new PdfByteQRCode(data);
        byte[] pdf = qr.GetGraphic(options.pixelsPerModule);
        return pdf;
    }

    private static byte[] GeneratePostScript(QRCodeData data, QrCodeOptions options)
    {
        using var qr = new PostscriptQRCode(data);
        string postscript = qr.GetGraphic(options.pixelsPerModule);
        return Encoding.UTF8.GetBytes(postscript);
    }
}