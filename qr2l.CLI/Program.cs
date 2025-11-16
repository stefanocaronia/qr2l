﻿using System.Drawing;
using System.Text;
using qr2l.Core;

namespace Qr2l.CLI;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length < 2) {
            ShowUsage();
            return;
        }

        string content = args[0];
        string output = args[1];

        QrCodeOptions options = ParseOptions(args.Skip(2).ToArray());

        ShowInfo($"📝 Encoding: {content}");

        try {
            ExportFormat format = GetFormatFromExtension(output);
            byte[] data = QrGenerator.Generate(content, format, options);

            File.WriteAllBytes(output, data);
            ShowSuccess($"✅  QR Code generated: {output}");
            ShowOptionsInfo(options);
        } catch (Exception ex) {
            ShowError("❌ Error generating QR Code:");
            ShowError(ex.Message);
        }
    }

    private static void ShowUsage()
    {
        ShowInfo($"*** {Project.Title} ***");
        ShowInfo("");
        ShowInfo("Usage: qr2l <text|url> <output file> [options]");
        ShowInfo("");
        ShowInfo("Supported formats: .png, .svg, .pdf, .bmp, .jpg, .jpeg, .gif, .ps");
        ShowInfo("");
        ShowInfo("Options:");
        ShowInfo("  --error-correction=<level>    Error correction level: low, medium, high, maximum (default: medium)");
        ShowInfo("  --dark-color=<hex>            Dark color in hex format (default: 000000)");
        ShowInfo("  --light-color=<hex>           Light color in hex format (default: FFFFFF)");
        ShowInfo("  --logo=<path>                 Path to logo image file");
        ShowInfo("  --pixels-per-module=<n>       Size of each module in pixels (default: 20)");
        ShowInfo("  --shape=<shape>               Pixel shape: square, circle (default: square)");
        ShowInfo("  --payload-mode=<mode>         Payload mode: auto, text, url, mail, sms, phone, wifi, geolocation, contact, event, whatsapp (default: auto)");
        ShowInfo("  --wifi-auth=<type>            WiFi authentication: wpa, wep, nopass (default: wpa)");
        ShowInfo("  --wifi-hidden                 Mark WiFi network as hidden");
        ShowInfo("");
        ShowInfo("Payload Formats:");
        ShowInfo("  text         Plain text");
        ShowInfo("  url          URL (adds https:// if missing)");
        ShowInfo("  mail         email;subject;body (subject and body are optional)");
        ShowInfo("  sms          number;message");
        ShowInfo("  phone        phonenumber");
        ShowInfo("  wifi         WIFI:SSID;password or WIFI:T:WPA;S:SSID;P:password;");
        ShowInfo("  geolocation  latitude,longitude");
        ShowInfo("  contact      firstName;lastName;phone;email");
        ShowInfo("  event        subject;description;location;startDateTime;endDateTime");
        ShowInfo("  whatsapp     +number;message");
        ShowInfo("");
        ShowInfo("Examples:");
        ShowInfo("  qr2l \"Hello World\" output.png");
        ShowInfo("  qr2l \"https://example.com\" qr.svg --dark-color=FF0000");
        ShowInfo("  qr2l \"WIFI:MyWiFi;password123\" wifi.png --payload-mode=wifi");
        ShowInfo("  qr2l \"WIFI:T:WPA;S:MyNetwork;P:secret123;\" wifi.png");
        ShowInfo("  qr2l \"Logo QR\" branded.png --logo=logo.png --error-correction=high");
        ShowInfo("  qr2l \"info@example.com;Hello;Email body\" mail.png --payload-mode=mail");
        ShowInfo("  qr2l \"+1234567890;Hello from QR\" sms.png --payload-mode=sms");
        ShowInfo("  qr2l \"45.4642,9.1900\" location.png --payload-mode=geolocation");
    }

    private static QrCodeOptions ParseOptions(string[] args)
    {
        var options = new QrCodeOptions();

        foreach (string arg in args) {
            if (!arg.StartsWith("--")) {
                continue;
            }

            string option = arg.Substring(2);
            string[] parts = option.Split('=');
            string key = parts[0];
            string? value = parts.Length > 1 ? parts[1] : null;

            switch (key) {
                case "error-correction":
                    options.errorCorrection = ParseErrorCorrection(value);
                    break;

                case "dark-color":
                    options.darkColor = ParseColor(value);
                    break;

                case "light-color":
                    options.lightColor = ParseColor(value);
                    break;

                case "logo":
                    if (!string.IsNullOrEmpty(value) && File.Exists(value)) {
                        options.logo = Image.FromFile(value);
                    } else {
                        ShowError($"⚠️  Logo file not found: {value}");
                    }

                    break;

                case "pixels-per-module":
                    if (int.TryParse(value, out int pixels)) {
                        options.pixelsPerModule = pixels;
                    }

                    break;

                case "shape":
                    options.shape = ParseShape(value);
                    break;

                case "payload-mode":
                    options.payloadMode = ParsePayloadMode(value);
                    break;

                case "wifi-auth":
                    options.wifiAuthType = ParseWiFiAuth(value);
                    break;

                case "wifi-hidden":
                    options.wifiHidden = true;
                    break;
            }
        }

        return options;
    }

    private static ExportFormat GetFormatFromExtension(string filename)
    {
        string ext = Path.GetExtension(filename).ToLower();

        return ext switch {
            ".png" => ExportFormat.Png,
            ".svg" => ExportFormat.Svg,
            ".pdf" => ExportFormat.Pdf,
            ".bmp" => ExportFormat.Bmp,
            ".jpg" or ".jpeg" => ExportFormat.Jpeg,
            ".gif" => ExportFormat.Gif,
            ".ps" => ExportFormat.PostScript,
            var _ => throw new ArgumentException($"Unsupported file format: {ext}")
        };
    }

    private static ErrorCorrectionLevel ParseErrorCorrection(string? value)
    {
        return value?.ToLower() switch {
            "low" => ErrorCorrectionLevel.Low,
            "medium" => ErrorCorrectionLevel.Medium,
            "high" => ErrorCorrectionLevel.High,
            "maximum" => ErrorCorrectionLevel.Maximum,
            var _ => ErrorCorrectionLevel.Medium
        };
    }

    private static Color ParseColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) {
            return Color.Black;
        }

        hex = hex.TrimStart('#');

        if (hex.Length == 6) {
            var r = Convert.ToInt32(hex.Substring(0, 2), 16);
            var g = Convert.ToInt32(hex.Substring(2, 2), 16);
            var b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return Color.FromArgb(r, g, b);
        }

        return Color.Black;
    }

    private static PixelShape ParseShape(string? value)
    {
        return value?.ToLower() switch {
            "circle" => PixelShape.Circle,
            "square" => PixelShape.Square,
            var _ => PixelShape.Square
        };
    }

    private static PayloadMode ParsePayloadMode(string? value)
    {
        return value?.ToLower() switch {
            "auto" => PayloadMode.Auto,
            "text" => PayloadMode.Text,
            "url" => PayloadMode.Url,
            "mail" => PayloadMode.Mail,
            "sms" => PayloadMode.SMS,
            "phone" => PayloadMode.Phone,
            "wifi" => PayloadMode.WiFi,
            "geolocation" or "geo" or "location" => PayloadMode.Geolocation,
            "contact" or "vcard" => PayloadMode.ContactData,
            "event" or "calendar" => PayloadMode.Event,
            "whatsapp" or "wa" => PayloadMode.WhatsApp,
            var _ => PayloadMode.Auto
        };
    }

    private static WiFiAuthenticationType ParseWiFiAuth(string? value)
    {
        return value?.ToLower() switch {
            "wpa" => WiFiAuthenticationType.WPA,
            "wep" => WiFiAuthenticationType.WEP,
            "nopass" => WiFiAuthenticationType.NoPassword,
            var _ => WiFiAuthenticationType.WPA
        };
    }

    private static void ShowOptionsInfo(QrCodeOptions options)
    {
        ShowInfo($"⚙️  Error Correction: {options.errorCorrection}");
        ShowInfo($"⚙️  Colors: #{options.darkColor.R:X2}{options.darkColor.G:X2}{options.darkColor.B:X2} / #{options.lightColor.R:X2}{options.lightColor.G:X2}{options.lightColor.B:X2}");
        ShowInfo($"⚙️  Shape: {options.shape}");
        ShowInfo($"⚙️  Payload Mode: {options.payloadMode}");

        if (options.logo != null) {
            ShowInfo("⚙️  Logo: Embedded");
        }

        if (options.payloadMode == PayloadMode.WiFi) {
            ShowInfo($"⚙️  WiFi Auth: {options.wifiAuthType}");

            if (options.wifiHidden) {
                ShowInfo("⚙️  WiFi Hidden: Yes");
            }
        }
    }

    private static void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void ShowInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}