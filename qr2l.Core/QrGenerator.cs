using System.Collections;
using System.Globalization;
using System.Text;
using QRCoder;
using SkiaSharp;

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
            ExportFormat.Png => EncodeRaster(data, options, SKEncodedImageFormat.Png),
            ExportFormat.Jpeg => EncodeRaster(data, options, SKEncodedImageFormat.Jpeg),
            ExportFormat.WebP => EncodeRaster(data, options, SKEncodedImageFormat.Webp),
            ExportFormat.Bmp => GenerateBmp(data, options),
            ExportFormat.Svg => GenerateSvgBytes(data, options),
            ExportFormat.Pdf => GeneratePdf(data, options),
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
        return svgQr.GetGraphic(
            pixelsPerModule: 20,
            darkColorHex: options.darkColor.ToHex(),
            lightColorHex: options.lightColor.ToHex(),
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

    private const int EncodeQuality = 90;
    private const float CirclePixelFactor = 0.8f;
    private const float LogoWidthRatio = 0.24f;
    private const float LogoPaddingRatio = 0.07f;

    // Il PDF conserva le dimensioni fisiche delle versioni precedenti: il disegno in pixel reso a 150 dpi
    private const float PdfDpi = 150f;

    // Lato più lungo, in pixel, del logo incorporato nel PostScript
    private const int PostScriptLogoMaxSize = 512;

    private const string HexDigits = "0123456789ABCDEF";

    private static byte[] EncodeRaster(QRCodeData data, QrCodeOptions options, SKEncodedImageFormat format)
    {
        using SKBitmap bitmap = RenderBitmap(data, options);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData encoded = image.Encode(format, EncodeQuality);
        return encoded.ToArray();
    }

    private static byte[] GenerateBmp(QRCodeData data, QrCodeOptions options)
    {
        using SKBitmap bitmap = RenderBitmap(data, options);
        return BmpEncoder.Encode(bitmap);
    }

    /// <summary>
    /// Rende il codice in un'immagine, un pixel per unità di disegno.
    /// </summary>
    private static SKBitmap RenderBitmap(QRCodeData data, QrCodeOptions options)
    {
        int size = data.ModuleMatrix.Count * options.pixelsPerModule;

        var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        DrawCode(canvas, data, options);
        canvas.Flush();
        return bitmap;
    }

    /// <summary>
    /// Disegna sfondo, moduli (quiet zone compresa) e logo in unità pixel. Lo stesso disegno serve le
    /// immagini e il PDF, così i formati condividono colori, forma dei pixel e logo.
    /// </summary>
    private static void DrawCode(SKCanvas canvas, QRCodeData data, QrCodeOptions options)
    {
        List<BitArray> matrix = data.ModuleMatrix;
        int modules = matrix.Count;
        float module = options.pixelsPerModule;
        float size = modules * module;

        using (var background = new SKPaint { Color = ToSkColor(options.lightColor) }) {
            canvas.DrawRect(0, 0, size, size, background);
        }

        bool circles = options.shape == PixelShape.Circle;
        float radius = module * CirclePixelFactor / 2f;

        // Tutti i moduli in un unico tracciato, riempito una volta sola: nei PDF evita le sottili
        // fessure che alcuni visualizzatori mostrano tra forme adiacenti riempite una per una
        using var squares = new SKPath();
        using var dots = new SKPath();

        for (var y = 0; y < modules; y++) {
            for (var x = 0; x < modules; x++) {
                if (!matrix[y][x]) {
                    continue;
                }

                if (circles && !IsFinderPattern(x, y, modules)) {
                    dots.AddCircle((x + 0.5f) * module, (y + 0.5f) * module, radius);
                } else {
                    squares.AddRect(SKRect.Create(x * module, y * module, module, module));
                }
            }
        }

        using var paint = new SKPaint { Color = ToSkColor(options.darkColor) };
        canvas.DrawPath(squares, paint);

        paint.IsAntialias = true;
        canvas.DrawPath(dots, paint);

        if (options.logo != null) {
            DrawLogo(canvas, size, options.logo, options.lightColor);
        }
    }

    /// <summary>
    /// I tre finder pattern restano quadrati anche con i pixel tondi: i lettori li cercano
    /// come sequenze di moduli pieni e con i cerchi faticano a riconoscerli.
    /// </summary>
    private static bool IsFinderPattern(int x, int y, int modules)
    {
        const int quietZone = 4;
        const int finderSize = 7;
        int last = modules - quietZone - finderSize;

        bool InFirst(int v) => v >= quietZone && v < quietZone + finderSize;
        bool InLast(int v) => v >= last && v < last + finderSize;

        return (InFirst(x) && InFirst(y)) || (InLast(x) && InFirst(y)) || (InFirst(x) && InLast(y));
    }

    /// <summary>
    /// Disegna il logo al centro su uno sfondo che libera i moduli sottostanti:
    /// senza di esso il logo risulterebbe semplicemente sovrapposto al disegno del codice.
    /// </summary>
    private static void DrawLogo(SKCanvas canvas, float size, byte[] logoBytes, QrColor background)
    {
        using SKBitmap logo = DecodeLogo(logoBytes);
        LogoPlacement placement = PlaceLogo(size, logo.Width, logo.Height);

        using var backgroundPaint = new SKPaint { Color = ToSkColor(background) };
        canvas.DrawRect(placement.Background, backgroundPaint);

        using SKImage image = SKImage.FromBitmap(logo);
        var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
        canvas.DrawImage(image, placement.Image, sampling);
    }

    /// <summary>
    /// Posizione del logo al centro del codice e del riquadro di sfondo che lo circonda.
    /// </summary>
    private static LogoPlacement PlaceLogo(float size, int logoWidth, int logoHeight)
    {
        float width = size * LogoWidthRatio;
        float height = width * logoHeight / logoWidth;
        float x = (size - width) / 2f;
        float y = (size - height) / 2f;
        float padding = width * LogoPaddingRatio;

        return new LogoPlacement(
            SKRect.Create(x - padding, y - padding, width + (padding * 2f), height + (padding * 2f)),
            SKRect.Create(x, y, width, height));
    }

    private static SKBitmap DecodeLogo(byte[] logoBytes)
    {
        return SKBitmap.Decode(logoBytes) ?? throw new ArgumentException("The logo is not a valid image.");
    }

    private static SKColor ToSkColor(QrColor color)
    {
        return new SKColor(color.R, color.G, color.B);
    }

    private static byte[] GenerateSvgBytes(QRCodeData data, QrCodeOptions options)
    {
        var svgQr = new SvgQRCode(data);
        string svg = svgQr.GetGraphic(
            pixelsPerModule: 20,
            darkColorHex: options.darkColor.ToHex(),
            lightColorHex: options.lightColor.ToHex(),
            drawQuietZones: true
        );

        return Encoding.UTF8.GetBytes(svg);
    }

    /// <summary>
    /// PDF vettoriale disegnato con lo stesso codice delle immagini, quindi con colori, forma dei pixel e logo.
    /// </summary>
    private static byte[] GeneratePdf(QRCodeData data, QrCodeOptions options)
    {
        float scale = 72f / PdfDpi;
        float page = data.ModuleMatrix.Count * options.pixelsPerModule * scale;

        SKDocumentPdfMetadata metadata = SKDocumentPdfMetadata.Default;
        metadata.Title = "QR Code";
        metadata.Creator = "qr2l";

        using var stream = new SKDynamicMemoryWStream();

        using (SKDocument document = SKDocument.CreatePdf(stream, metadata)
                   ?? throw new InvalidOperationException("PDF export is not available on this platform.")) {
            SKCanvas canvas = document.BeginPage(page, page);
            canvas.Scale(scale);
            DrawCode(canvas, data, options);
            document.EndPage();
            document.Close();
        }

        using SKData pdf = stream.DetachAsData();
        return pdf.ToArray();
    }

    /// <summary>
    /// PostScript scritto direttamente, con la stessa geometria del disegno: colori, forma dei pixel e logo.
    /// Un'unità di disegno vale un punto, come nelle versioni precedenti.
    /// </summary>
    private static byte[] GeneratePostScript(QRCodeData data, QrCodeOptions options)
    {
        List<BitArray> matrix = data.ModuleMatrix;
        int modules = matrix.Count;
        float module = options.pixelsPerModule;
        float size = modules * module;
        bool circles = options.shape == PixelShape.Circle;
        var box = (int)Math.Ceiling(size);

        var ps = new StringBuilder();
        ps.Append("%!PS-Adobe-3.0\n");
        ps.Append("%%Creator: qr2l\n");
        ps.Append("%%Title: QR Code\n");
        ps.Append($"%%BoundingBox: 0 0 {box} {box}\n");
        ps.Append($"%%HiResBoundingBox: 0 0 {Ps(size)} {Ps(size)}\n");
        ps.Append("%%LanguageLevel: 2\n");
        ps.Append("%%DocumentData: Clean7Bit\n");
        ps.Append("%%Pages: 1\n");
        ps.Append("%%EndComments\n");

        // s disegna un modulo quadrato e d uno tondo, a partire dalle coordinate del disegno
        ps.Append("%%BeginProlog\n");
        ps.Append($"/m {Ps(module)} def\n");
        ps.Append($"/rd {Ps(module * CirclePixelFactor / 2f)} def\n");
        ps.Append("/s { moveto m 0 rlineto 0 m rlineto m neg 0 rlineto closepath } bind def\n");
        ps.Append("/d { 2 copy exch rd add exch moveto rd 0 360 arc closepath } bind def\n");
        ps.Append("%%EndProlog\n");

        ps.Append("%%BeginSetup\n");
        ps.Append("%%BeginFeature: *PageSize Default\n");
        ps.Append($"<< /PageSize [{Ps(size)} {Ps(size)}] >> setpagedevice\n");
        ps.Append("%%EndFeature\n");
        ps.Append("%%EndSetup\n");

        ps.Append("%%Page: 1 1\n");
        ps.Append("gsave\n");

        // Le coordinate del disegno crescono verso il basso, quelle PostScript verso l'alto
        ps.Append($"0 {Ps(size)} translate 1 -1 scale\n");
        ps.Append($"{PsColor(options.lightColor)} 0 0 {Ps(size)} {Ps(size)} rectfill\n");

        // Un unico tracciato per tutti i moduli, come nel PDF
        ps.Append($"{PsColor(options.darkColor)} newpath\n");

        for (var y = 0; y < modules; y++) {
            for (var x = 0; x < modules; x++) {
                if (!matrix[y][x]) {
                    continue;
                }

                if (circles && !IsFinderPattern(x, y, modules)) {
                    ps.Append($"{Ps((x + 0.5f) * module)} {Ps((y + 0.5f) * module)} d\n");
                } else {
                    ps.Append($"{Ps(x * module)} {Ps(y * module)} s\n");
                }
            }
        }

        ps.Append("fill\n");

        if (options.logo != null) {
            AppendPostScriptLogo(ps, size, options.logo, options.lightColor);
        }

        ps.Append("grestore\n");
        ps.Append("showpage\n");
        ps.Append("%%EOF\n");

        return Encoding.ASCII.GetBytes(ps.ToString());
    }

    /// <summary>
    /// Il logo entra nel PostScript come immagine RGB già composta sullo sfondo: il livello 2 non
    /// gestisce la trasparenza, e il logo sta comunque su un riquadro a tinta unita.
    /// </summary>
    private static void AppendPostScriptLogo(StringBuilder ps, float size, byte[] logoBytes, QrColor background)
    {
        using SKBitmap logo = DecodeLogo(logoBytes);
        LogoPlacement placement = PlaceLogo(size, logo.Width, logo.Height);

        float reduction = Math.Min(1f, (float)PostScriptLogoMaxSize / Math.Max(logo.Width, logo.Height));
        int width = Math.Max(1, (int)Math.Round(logo.Width * reduction));
        int height = Math.Max(1, (int)Math.Round(logo.Height * reduction));

        using var composed = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));

        using (var canvas = new SKCanvas(composed)) {
            canvas.Clear(ToSkColor(background));
            using SKImage image = SKImage.FromBitmap(logo);
            canvas.DrawImage(image, SKRect.Create(0, 0, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        }

        SKRect back = placement.Background;
        SKRect area = placement.Image;

        ps.Append($"{PsColor(background)} {Ps(back.Left)} {Ps(back.Top)} {Ps(back.Width)} {Ps(back.Height)} rectfill\n");
        ps.Append("gsave\n");
        ps.Append($"{Ps(area.Left)} {Ps(area.Top)} translate {Ps(area.Width)} {Ps(area.Height)} scale\n");

        // Nello spazio già ribaltato la prima riga dell'immagine finisce in alto
        ps.Append($"{width} {height} 8 [{width} 0 0 {height} 0 0] currentfile /ASCIIHexDecode filter false 3 colorimage\n");

        ReadOnlySpan<byte> pixels = composed.GetPixelSpan();
        var column = 0;

        for (var y = 0; y < height; y++) {
            ReadOnlySpan<byte> row = pixels.Slice(y * composed.RowBytes, width * 4);

            for (var x = 0; x < width; x++) {
                // Rgba8888: rosso, verde, blu, alfa; l'alfa è pieno dopo la composizione sullo sfondo
                for (var channel = 0; channel < 3; channel++) {
                    byte value = row[(x * 4) + channel];
                    ps.Append(HexDigits[value >> 4]).Append(HexDigits[value & 0xF]);
                }

                column += 6;

                if (column >= 72) {
                    ps.Append('\n');
                    column = 0;
                }
            }
        }

        ps.Append(">\n");
        ps.Append("grestore\n");
    }

    private static string Ps(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string PsColor(QrColor color)
    {
        return $"{Ps(color.R / 255f)} {Ps(color.G / 255f)} {Ps(color.B / 255f)} setrgbcolor";
    }

    private readonly record struct LogoPlacement(SKRect Background, SKRect Image);
}
