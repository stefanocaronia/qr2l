using System;
using System.Text;
using qr2l.Core;
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
}