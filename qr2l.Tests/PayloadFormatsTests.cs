using qr2l.Core;
using Xunit;

namespace qr2l.Tests;

public class PayloadFormatsTests
{
    [Theory]
    [InlineData("test@example.com", PayloadMode.Mail)]
    [InlineData("mailto:test@example.com", PayloadMode.Mail)]
    [InlineData("test@example.com;Subject Line", PayloadMode.Mail)]
    [InlineData("test@example.com;Subject;Body text", PayloadMode.Mail)]
    [InlineData("test@example.com;;Body only", PayloadMode.Mail)]
    [InlineData("user.name+tag@subdomain.example.co.uk;Meeting;Let's meet tomorrow", PayloadMode.Mail)]
    public void Generate_EmailFormats_ShouldSucceed(string content, PayloadMode mode)
    {
        var options = new QrCodeOptions {
            payloadMode = mode
        };

        byte[] result = QrGenerator.Generate(content, ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 100);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("mailto:test@example.com")]
    [InlineData("test@example.com;Hello")]
    [InlineData("test@example.com;Hello;Body text here")]
    [InlineData("test@example.com;;Just body")]
    public void Generate_EmailPayload_ShouldGenerateValidQR(string input)
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Mail
        };

        string svgResult = QrGenerator.GenerateSvgString(input, options);

        Assert.NotNull(svgResult);
        Assert.Contains("svg", svgResult.ToLower());
    }

    [Theory]
    [InlineData("WIFI:MyNetwork;password123", PayloadMode.WiFi)]
    [InlineData("WIFI:HomeNetwork;mySecretPass123", PayloadMode.WiFi)]
    [InlineData("WIFI:Office WiFi;complex@pass#123", PayloadMode.WiFi)]
    [InlineData("WIFI:OpenNetwork;", PayloadMode.WiFi)]
    [InlineData("WIFI:T:WPA;S:MyNetwork;P:password123;", PayloadMode.WiFi)]
    [InlineData("WIFI:T:WPA;S:TestNet;P:pass;", PayloadMode.WiFi)]
    [InlineData("WIFI:T:WEP;S:OldRouter;P:wepkey;", PayloadMode.WiFi)]
    public void Generate_WiFiFormats_ShouldSucceed(string content, PayloadMode mode)
    {
        var options = new QrCodeOptions {
            payloadMode = mode
        };

        byte[] result = QrGenerator.Generate(content, ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 100);
    }

    [Theory]
    [InlineData("WIFI:MyNetwork;password123", WiFiAuthenticationType.WPA, false)]
    [InlineData("WIFI:SecureNet;secret", WiFiAuthenticationType.WPA, true)]
    [InlineData("WIFI:OldRouter;key123", WiFiAuthenticationType.WEP, false)]
    [InlineData("WIFI:PublicWiFi;", WiFiAuthenticationType.NoPassword, false)]
    public void Generate_WiFiWithOptions_ShouldSucceed(string content, WiFiAuthenticationType authType, bool hidden)
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.WiFi,
            wifiAuthType = authType,
            wifiHidden = hidden
        };

        byte[] result = QrGenerator.Generate(content, ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 100);
    }

    [Theory]
    [InlineData("WIFI:Network;pass")]
    [InlineData("WIFI:OpenNet;")]
    public void Generate_WiFiSimplifiedFormat_ShouldGenerateValidQR(string input)
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.WiFi
        };

        string svgResult = QrGenerator.GenerateSvgString(input, options);

        Assert.NotNull(svgResult);
        Assert.Contains("svg", svgResult.ToLower());
    }

    [Theory]
    [InlineData("WIFI:T:WPA;S:MyNetwork;P:password123;")]
    [InlineData("WIFI:T:WEP;S:OldNet;P:key;")]
    [InlineData("WIFI:T:nopass;S:PublicNet;")]
    public void Generate_WiFiCompleteFormat_ShouldBeAcceptedAsIs(string content)
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.WiFi
        };

        byte[] result = QrGenerator.Generate(content, ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 100);
    }

    [Fact]
    public void Generate_EmailWithAutoDetection_ShouldSucceed()
    {
        string content = "test@example.com;Meeting Tomorrow;Let's meet at 10am";
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Auto
        };

        byte[] result = QrGenerator.Generate(content, ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_WiFiWithAutoDetection_ShouldSucceed()
    {
        string content = "WIFI:MyNetwork;password123";
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Auto
        };

        byte[] result = QrGenerator.Generate(content, ExportFormat.Png, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("info@company.com", ExportFormat.Png)]
    [InlineData("info@company.com;Newsletter", ExportFormat.Svg)]
    [InlineData("info@company.com;Welcome;Thank you for signing up", ExportFormat.Pdf)]
    public void Generate_EmailWithDifferentFormats_ShouldSucceed(string content, ExportFormat format)
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.Mail
        };

        byte[] result = QrGenerator.Generate(content, format, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("WIFI:GuestNetwork;guest123", ExportFormat.Png)]
    [InlineData("WIFI:T:WPA;S:Office;P:secure123;", ExportFormat.Svg)]
    [InlineData("WIFI:PublicSpot;", ExportFormat.Pdf)]
    public void Generate_WiFiWithDifferentFormats_ShouldSucceed(string content, ExportFormat format)
    {
        var options = new QrCodeOptions {
            payloadMode = PayloadMode.WiFi
        };

        byte[] result = QrGenerator.Generate(content, format, options);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}

