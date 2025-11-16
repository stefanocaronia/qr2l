using qr2l.Core;
using Xunit;

namespace qr2l.Tests;

public class PayloadModeDetectionTests
{
    [Theory]
    [InlineData("test@example.com", PayloadMode.Mail)]
    [InlineData("mailto:test@example.com", PayloadMode.Mail)]
    [InlineData("user.name@domain.com", PayloadMode.Mail)]
    [InlineData("email@subdomain.example.com", PayloadMode.Mail)]
    public void DetectPayloadMode_Email_ShouldReturnMail(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("http://example.com", PayloadMode.Url)]
    [InlineData("https://example.com", PayloadMode.Url)]
    [InlineData("www.example.com", PayloadMode.Url)]
    [InlineData("example.com", PayloadMode.Url)]
    [InlineData("github.io/project", PayloadMode.Url)]
    [InlineData("site.net/page", PayloadMode.Url)]
    public void DetectPayloadMode_Url_ShouldReturnUrl(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("45.4642,9.1900", PayloadMode.Geolocation)]
    [InlineData("40.7128,-74.0060", PayloadMode.Geolocation)]
    [InlineData("-33.8688,151.2093", PayloadMode.Geolocation)]
    [InlineData("0,0", PayloadMode.Geolocation)]
    public void DetectPayloadMode_Geolocation_ShouldReturnGeolocation(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("+39 123 456 7890", PayloadMode.Phone)]
    [InlineData("1234567890", PayloadMode.Phone)]
    [InlineData("+1 (555) 123-4567", PayloadMode.Phone)]
    [InlineData("555-123-4567", PayloadMode.Phone)]
    public void DetectPayloadMode_Phone_ShouldReturnPhone(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("+39 123 456 7890;Ciao!", PayloadMode.WhatsApp)]
    [InlineData("+1234567890;Hello", PayloadMode.WhatsApp)]
    public void DetectPayloadMode_WhatsApp_ShouldReturnWhatsApp(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1234567890;Message text", PayloadMode.SMS)]
    [InlineData("555-123-4567;Hello", PayloadMode.SMS)]
    public void DetectPayloadMode_SMS_ShouldReturnSMS(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("MyNetwork;password123", PayloadMode.WiFi)]
    [InlineData("HomeWiFi;mypass", PayloadMode.WiFi)]
    public void DetectPayloadMode_WiFi_ShouldReturnWiFi(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("John;Doe;+1234567890;john@example.com", PayloadMode.ContactData)]
    [InlineData("Jane;Smith;;jane@example.com", PayloadMode.ContactData)]
    [InlineData("Bob;Johnson;555-123-4567;", PayloadMode.ContactData)]
    public void DetectPayloadMode_ContactData_ShouldReturnContactData(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Meeting;Important meeting;Office;2025-11-16T10:00:00;2025-11-16T11:00:00", PayloadMode.Event)]
    [InlineData("Conference;Annual conference;Center;2025-12-01;2025-12-01", PayloadMode.Event)]
    public void DetectPayloadMode_Event_ShouldReturnEvent(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Just some plain text", PayloadMode.Text)]
    [InlineData("Random words without structure", PayloadMode.Text)]
    [InlineData("123 456", PayloadMode.Text)]
    [InlineData("", PayloadMode.Text)]
    public void DetectPayloadMode_Text_ShouldReturnText(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }
}