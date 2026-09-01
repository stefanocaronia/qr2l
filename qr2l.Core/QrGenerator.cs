using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using QRCoder;

namespace qr2l.Core;

public static class QrGenerator
{
    public static byte[] Generate(string text, ExportFormat format, QrCodeOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(text)) {
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));
        }

        options ??= new QrCodeOptions();

        string payload = PreparePayload(text, options.payloadMode, options);

        if (options.logo != null && options.errorCorrection != ErrorCorrectionLevel.Maximum) {
            options.errorCorrection = ErrorCorrectionLevel.Maximum;
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
        if (string.IsNullOrWhiteSpace(text)) {
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));
        }

        options ??= new QrCodeOptions();

        string payload = PreparePayload(text, options.payloadMode, options);

        if (options.logo != null && options.errorCorrection != ErrorCorrectionLevel.Maximum) {
            options.errorCorrection = ErrorCorrectionLevel.Maximum;
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
        if (mode == PayloadMode.Auto) {
            mode = DetectPayloadMode(text);
        }
        
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
    
    public static PayloadMode DetectPayloadMode(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) {
            return PayloadMode.Text;
        }
        
        text = text.Trim();
        
        // URL detection with explicit protocol must come first
        if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) {
            return PayloadMode.Url;
        }
        
        // Email detection
        if (text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) {
            return PayloadMode.Mail;
        }
        
        if (text.Contains('@') && !text.Contains(';')) {
            string[] atParts = text.Split('@');
            if (atParts.Length == 2 && atParts[1].Contains('.') && !atParts[1].Contains(' ')) {
                return PayloadMode.Mail;
            }
        }
        
        // URL detection without explicit protocol
        if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
            (text.Contains('.') && !text.Contains(' ') && !text.Contains(';') && !text.Contains('@') &&
             (text.EndsWith(".com") || text.EndsWith(".net") || text.EndsWith(".org") || 
              text.EndsWith(".io") || text.EndsWith(".it") || text.Contains(".com/") || 
              text.Contains(".net/") || text.Contains(".org/") || text.Contains(".io/")))) {
            return PayloadMode.Url;
        }
        
        // Geolocation detection: lat,lon format (decimals with point as separator)
        if (text.Contains(',') && !text.Contains(';')) {
            string[] parts = text.Split(',');
            if (parts.Length >= 2 && parts.Length <= 3) {
                if (double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) && 
                    double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon)) {
                    if (lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180) {
                        return PayloadMode.Geolocation;
                    }
                }
            }
        }
        
        // Phone detection: only digits, spaces, +, -, (, )
        string phonePattern = text.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        if (phonePattern.Length >= 7 && phonePattern.All(char.IsDigit) && !text.Contains(';') && !text.Contains(',')) {
            return PayloadMode.Phone;
        }
        
        // WiFi detection: must start with WIFI: prefix or have complete format
        if (text.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase)) {
            return PayloadMode.WiFi;
        }
        
        // Structured data with semicolons
        if (text.Contains(';')) {
            string[] parts = text.Split(';');
            
            // WhatsApp detection: starts with + followed by digits
            if (parts.Length >= 1 && parts[0].Trim().StartsWith("+") && 
                parts[0].Trim().Substring(1).Replace(" ", "").All(char.IsDigit)) {
                return PayloadMode.WhatsApp;
            }
            
            // SMS detection: phone number followed by message
            string firstPart = parts[0].Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
            if (firstPart.Length >= 7 && firstPart.All(char.IsDigit)) {
                return PayloadMode.SMS;
            }
            
            // Event detection: contains date-like patterns (ISO format)
            if (parts.Length >= 3) {
                foreach (string part in parts) {
                    if (DateTime.TryParse(part.Trim(), out _)) {
                        return PayloadMode.Event;
                    }
                }
            }
            
            // ContactData detection: 2+ parts, looks like name/contact info
            if (parts.Length >= 2 && parts.Length <= 4) {
                bool hasEmail = parts.Any(p => p.Contains('@'));
                bool hasPhone = parts.Any(p => {
                    string clean = p.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
                    return clean.Length >= 7 && clean.All(char.IsDigit);
                });
                
                if (hasEmail || hasPhone) {
                    return PayloadMode.ContactData;
                }
            }
        }
        
        // Default to Text
        return PayloadMode.Text;
    }
    
    private static string PrepareMailPayload(string text)
    {
        if (text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) {
            text = text.Substring(7);
        }
        
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
        // If already in complete WIFI: format, return as-is
        if (text.StartsWith("WIFI:T:", StringComparison.OrdinalIgnoreCase)) {
            return text;
        }
        
        // Remove WIFI: prefix if present and parse simplified format
        if (text.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase)) {
            text = text.Substring(5);
        }
        
        string[] parts = text.Split(';');

        if (parts.Length < 1 || parts.Length > 2) {
            throw new ArgumentException("WiFi payload must be in format: WIFI:ssid;password (password optional for open networks)");
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

        if (authType == WiFiAuthenticationType.NoPassword || string.IsNullOrEmpty(password)) {
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
        return SaveBitmap(RenderBitmap(data, options), ImageFormat.Png);
    }

    private static byte[] GenerateBmp(QRCodeData data, QrCodeOptions options)
    {
        return SaveBitmap(RenderBitmap(data, options), ImageFormat.Bmp);
    }

    private static byte[] GenerateJpeg(QRCodeData data, QrCodeOptions options)
    {
        return SaveBitmap(RenderBitmap(data, options), ImageFormat.Jpeg);
    }

    private static byte[] GenerateGif(QRCodeData data, QrCodeOptions options)
    {
        return SaveBitmap(RenderBitmap(data, options), ImageFormat.Gif);
    }

    /// <summary>
    /// Disegna il codice in tutti i formati raster, logo compreso.
    /// </summary>
    private static Bitmap RenderBitmap(QRCodeData data, QrCodeOptions options)
    {
        Bitmap bitmap;

        if (options.shape == PixelShape.Circle) {
            using var artQr = new ArtQRCode(data);
            bitmap = artQr.GetGraphic(
                pixelsPerModule: options.pixelsPerModule,
                darkColor: options.darkColor,
                lightColor: options.lightColor,
                backgroundColor: options.lightColor,
                pixelSizeFactor: 0.8f,
                drawQuietZones: true
            );
        } else {
            using var qr = new QRCode(data);
            bitmap = qr.GetGraphic(
                pixelsPerModule: options.pixelsPerModule,
                darkColor: options.darkColor,
                lightColor: options.lightColor,
                drawQuietZones: true
            );
        }

        if (options.logo != null) {
            DrawLogo(bitmap, options.logo, options.lightColor);
        }

        return bitmap;
    }

    /// <summary>
    /// Disegna il logo al centro su uno sfondo arrotondato che libera i moduli sottostanti:
    /// senza di esso il logo risulterebbe semplicemente sovrapposto al disegno del codice.
    /// </summary>
    private static void DrawLogo(Bitmap bitmap, Image logo, Color backgroundColor)
    {
        // Porzione del lato occupata dal logo e margine attorno, in frazione del logo stesso
        const float logoWidthRatio = 0.24f;
        const float paddingRatio = 0.07f;

        float logoWidth = bitmap.Width * logoWidthRatio;
        float logoHeight = logoWidth * logo.Height / logo.Width;

        float x = (bitmap.Width - logoWidth) / 2f;
        float y = (bitmap.Height - logoHeight) / 2f;

        float padding = logoWidth * paddingRatio;
        var background = new RectangleF(
            x - padding,
            y - padding,
            logoWidth + (padding * 2f),
            logoHeight + (padding * 2f));

        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using (var brush = new SolidBrush(backgroundColor)) {
            graphics.FillRectangle(brush, background);
        }

        graphics.DrawImage(logo, new RectangleF(x, y, logoWidth, logoHeight));
    }

    private static byte[] SaveBitmap(Bitmap bitmap, ImageFormat format)
    {
        using (bitmap) {
            using var stream = new MemoryStream();
            bitmap.Save(stream, format);
            return stream.ToArray();
        }
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