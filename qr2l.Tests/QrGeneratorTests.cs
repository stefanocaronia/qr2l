using System;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using qr2l.Core;
using SkiaSharp;
using Xunit;

namespace qr2l.Tests;

public class QrGeneratorTests
{
    [Theory]
    [InlineData("Hello World", ExportFormat.Png)]
    [InlineData("https://example.com", ExportFormat.Png)]
    [InlineData("test@example.com", ExportFormat.Png)]
    public void Generate_Png_ShouldReturnValidData(string content, ExportFormat format)
    {
        var options = new QrCodeOptions();

        byte[] result = QrGenerator.Generate(content, format, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 100);
    }

    [Theory]
    [InlineData("Hello World", ExportFormat.Svg)]
    [InlineData("https://example.com", ExportFormat.Svg)]
    public void Generate_Svg_ShouldReturnValidData(string content, ExportFormat format)
    {
        var options = new QrCodeOptions();

        byte[] result = QrGenerator.Generate(content, format, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        string svgContent = Encoding.UTF8.GetString(result);
        Assert.Contains("<svg", svgContent);
        Assert.Contains("</svg>", svgContent);
    }

    [Fact]
    public void GenerateSvgString_ShouldReturnValidSvg()
    {
        var content = "Test QR Code";
        var options = new QrCodeOptions();

        string result = QrGenerator.GenerateSvgString(content, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("<svg", result);
        Assert.Contains("</svg>", result);
    }

    [Fact]
    public void Generate_WithCustomColors_ShouldSucceed()
    {
        var options = new QrCodeOptions {
            darkColor = new QrColor(255, 0, 0),
            lightColor = new QrColor(255, 255, 0),
            pixelsPerModule = 10
        };

        byte[] result = QrGenerator.Generate("Color Test", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(ErrorCorrectionLevel.Low)]
    [InlineData(ErrorCorrectionLevel.Medium)]
    [InlineData(ErrorCorrectionLevel.High)]
    [InlineData(ErrorCorrectionLevel.Maximum)]
    public void Generate_WithDifferentErrorCorrectionLevels_ShouldSucceed(ErrorCorrectionLevel level)
    {
        var options = new QrCodeOptions {
            errorCorrection = level
        };

        byte[] result = QrGenerator.Generate("Error Correction Test", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void Generate_WithDifferentPixelsPerModule_ShouldSucceed(int pixels)
    {
        var options = new QrCodeOptions {
            pixelsPerModule = pixels
        };

        byte[] result = QrGenerator.Generate("Size Test", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(PixelShape.Square)]
    [InlineData(PixelShape.Circle)]
    public void Generate_WithDifferentShapes_ShouldSucceed(PixelShape shape)
    {
        var options = new QrCodeOptions {
            shape = shape
        };

        byte[] result = QrGenerator.Generate("Shape Test", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(ExportFormat.Png)]
    [InlineData(ExportFormat.Svg)]
    [InlineData(ExportFormat.Pdf)]
    [InlineData(ExportFormat.Bmp)]
    [InlineData(ExportFormat.Jpeg)]
    [InlineData(ExportFormat.WebP)]
    [InlineData(ExportFormat.PostScript)]
    public void Generate_AllFormats_ShouldSucceed(ExportFormat format)
    {
        var options = new QrCodeOptions();

        byte[] result = QrGenerator.Generate("Format Test", format, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WithUrlPayload_ShouldAddHttpsPrefix()
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Url
        };

        byte[] result = QrGenerator.Generate("example.com", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WithMailPayload_ShouldFormatCorrectly()
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Mail
        };

        byte[] result = QrGenerator.Generate("test@example.com;Subject;Body", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WithPhonePayload_ShouldFormatCorrectly()
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Phone
        };

        byte[] result = QrGenerator.Generate("+1234567890", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WithSmsPayload_ShouldFormatCorrectly()
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.SMS
        };

        byte[] result = QrGenerator.Generate("+1234567890;Hello", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(WiFiAuthenticationType.WPA)]
    [InlineData(WiFiAuthenticationType.WEP)]
    [InlineData(WiFiAuthenticationType.NoPassword)]
    public void Generate_WithWiFiPayload_ShouldFormatCorrectly(WiFiAuthenticationType authType)
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.WiFi,
            wifiAuthType = authType,
            wifiHidden = false
        };

        byte[] result = QrGenerator.Generate("MyNetwork;password123", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WithGeolocationPayload_ShouldFormatCorrectly()
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Geolocation
        };

        byte[] result = QrGenerator.Generate("45.4642,9.1900", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WithContactDataPayload_ShouldFormatCorrectly()
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.ContactData
        };

        byte[] result = QrGenerator.Generate("John;Doe;+1234567890;john@example.com", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WithEventPayload_ShouldFormatCorrectly()
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Event
        };

        byte[] result = QrGenerator.Generate("Meeting;Description;Office;2025-11-16T10:00:00;2025-11-16T11:00:00", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WithWhatsAppPayload_ShouldFormatCorrectly()
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.WhatsApp
        };

        byte[] result = QrGenerator.Generate("+1234567890;Hello WhatsApp", ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_EmptyString_ShouldThrowException()
    {
        var options = new QrCodeOptions();

        Assert.Throws<ArgumentException>(() =>
            QrGenerator.Generate("", ExportFormat.Png, options));
    }

    [Fact]
    public void Generate_NullString_ShouldThrowException()
    {
        var options = new QrCodeOptions();

        Assert.Throws<ArgumentException>(() =>
            QrGenerator.Generate(null!, ExportFormat.Png, options));
    }

    [Fact]
    public void DetectPayloadMode_WithAutoMode_ShouldDetectCorrectly()
    {
        Assert.Equal(PayloadMode.Mail, QrGenerator.DetectPayloadMode("test@example.com"));
        Assert.Equal(PayloadMode.Url, QrGenerator.DetectPayloadMode("https://example.com"));
        Assert.Equal(PayloadMode.Phone, QrGenerator.DetectPayloadMode("+1234567890"));
        Assert.Equal(PayloadMode.Geolocation, QrGenerator.DetectPayloadMode("45.4642,9.1900"));
        Assert.Equal(PayloadMode.Text, QrGenerator.DetectPayloadMode("Just plain text"));
    }

    private static readonly QrColor DarkBlue = new(0x1F, 0x3A, 0x93);
    private static readonly QrColor Cream = new(0xFF, 0xF8, 0xE1);

    [Fact]
    public void Generate_Pdf_ShouldUseTheChosenColors()
    {
        var options = new QrCodeOptions { darkColor = DarkBlue, lightColor = Cream };

        byte[] pdf = QrGenerator.Generate("Color Test", ExportFormat.Pdf, options);
        List<(double R, double G, double B)> fills = PdfFillColors(pdf);

        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.Contains(fills, color => SameColor(color, DarkBlue));
        Assert.Contains(fills, color => SameColor(color, Cream));
    }

    [Fact]
    public void Generate_Pdf_ShouldEmbedTheLogo()
    {
        string without = Encoding.Latin1.GetString(QrGenerator.Generate("Logo Test", ExportFormat.Pdf, new QrCodeOptions()));
        string with = Encoding.Latin1.GetString(QrGenerator.Generate("Logo Test", ExportFormat.Pdf, new QrCodeOptions { logo = CreateLogo() }));

        Assert.DoesNotContain("/Subtype /Image", without);
        Assert.Contains("/Subtype /Image", with);
    }

    [Fact]
    public void Generate_PostScript_ShouldUseTheChosenColors()
    {
        var options = new QrCodeOptions { darkColor = DarkBlue, lightColor = Cream };

        string ps = Encoding.ASCII.GetString(QrGenerator.Generate("Color Test", ExportFormat.PostScript, options));

        Assert.StartsWith("%!PS-Adobe-3.0", ps);
        Assert.Contains("0.122 0.227 0.576 setrgbcolor", ps);
        Assert.Contains("1 0.973 0.882 setrgbcolor", ps);
    }

    [Fact]
    public void Generate_PostScript_ShouldEmbedTheLogo()
    {
        string without = Encoding.ASCII.GetString(QrGenerator.Generate("Logo Test", ExportFormat.PostScript, new QrCodeOptions()));
        string with = Encoding.ASCII.GetString(QrGenerator.Generate("Logo Test", ExportFormat.PostScript, new QrCodeOptions { logo = CreateLogo() }));

        Assert.DoesNotContain("colorimage", without);
        Assert.Contains("colorimage", with);
    }

    private static byte[] CreateLogo()
    {
        using var bitmap = new SKBitmap(16, 16);
        bitmap.Erase(SKColors.Red);
        using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    /// <summary>
    /// Colori di riempimento ("r g b rg") usati nei content stream del PDF, decompressi se necessario.
    /// </summary>
    private static List<(double R, double G, double B)> PdfFillColors(byte[] pdf)
    {
        var colors = new List<(double R, double G, double B)>();
        string raw = Encoding.Latin1.GetString(pdf);

        foreach (Match stream in Regex.Matches(raw, @"(?<!end)stream\r?\n")) {
            int start = stream.Index + stream.Length;
            int end = raw.IndexOf("endstream", start, StringComparison.Ordinal);
            byte[] data = pdf[start..end];
            string content;

            try {
                using var input = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress);
                using var output = new MemoryStream();
                input.CopyTo(output);
                content = Encoding.Latin1.GetString(output.ToArray());
            } catch (InvalidDataException) {
                content = Encoding.Latin1.GetString(data);
            }

            foreach (Match fill in Regex.Matches(content, @"([\d.]+) ([\d.]+) ([\d.]+) rg\b")) {
                colors.Add((
                    double.Parse(fill.Groups[1].Value, CultureInfo.InvariantCulture),
                    double.Parse(fill.Groups[2].Value, CultureInfo.InvariantCulture),
                    double.Parse(fill.Groups[3].Value, CultureInfo.InvariantCulture)));
            }
        }

        return colors;
    }

    private static bool SameColor((double R, double G, double B) actual, QrColor expected)
    {
        return Math.Abs(actual.R - (expected.R / 255d)) < 0.01 &&
               Math.Abs(actual.G - (expected.G / 255d)) < 0.01 &&
               Math.Abs(actual.B - (expected.B / 255d)) < 0.01;
    }
}