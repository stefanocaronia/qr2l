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
    [InlineData("contact+label@example.co.uk", PayloadMode.Mail)]
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
    [InlineData("https://www.google.com/maps/@45.5392001,9.2430083,15z?hl=it&entry=ttu&g_ep=EgoyMDI1MTExMi4wIKXMDSoASAFQAw%3D%3D", PayloadMode.Url)]
    [InlineData("http://localhost:8080", PayloadMode.Url)]
    [InlineData("https://user:password@example.com/path", PayloadMode.Url)]
    [InlineData("http://192.168.1.1", PayloadMode.Url)]
    [InlineData("https://10.0.0.1:3000/api", PayloadMode.Url)]
    [InlineData("ftp://files.example.com", PayloadMode.Url)]
    [InlineData("http://example.com/user@domain", PayloadMode.Url)]
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
    [InlineData("90,180", PayloadMode.Geolocation)]
    [InlineData("-90,-180", PayloadMode.Geolocation)]
    [InlineData("45.123456,9.987654", PayloadMode.Geolocation)]
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
    [InlineData("+441234567890", PayloadMode.Phone)]
    [InlineData("00393456789012", PayloadMode.Phone)]
    public void DetectPayloadMode_Phone_ShouldReturnPhone(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("+39 123 456 7890;Ciao!", PayloadMode.WhatsApp)]
    [InlineData("+1234567890;Hello", PayloadMode.WhatsApp)]
    [InlineData("+441234567890;", PayloadMode.WhatsApp)]
    [InlineData("+393456789012;Message with special chars: @#$%", PayloadMode.WhatsApp)]
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
    [InlineData("WIFI:MyNetwork;password123", PayloadMode.WiFi)]
    [InlineData("WIFI:HomeWiFi;mypass", PayloadMode.WiFi)]
    [InlineData("WIFI:My Home Network;secret_password", PayloadMode.WiFi)]
    [InlineData("WIFI:WiFi-Guest;", PayloadMode.WiFi)]
    [InlineData("WIFI:T:WPA;S:MyNetwork;P:password123;", PayloadMode.WiFi)]
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
    [InlineData("12345", PayloadMode.Text)]
    [InlineData("123456", PayloadMode.Text)]
    [InlineData("user @ domain", PayloadMode.Text)]
    [InlineData("not an email@", PayloadMode.Text)]
    [InlineData("@notanemail", PayloadMode.Text)]
    [InlineData("1.2.3.4.5.6", PayloadMode.Text)]
    [InlineData("version 2.0.1", PayloadMode.Text)]
    [InlineData("MyNetwork;password123", PayloadMode.Text)]
    [InlineData("some text;with semicolon", PayloadMode.Text)]
    [InlineData("title;subtitle", PayloadMode.Text)]
    public void DetectPayloadMode_Text_ShouldReturnText(string input, PayloadMode expected)
    {
        PayloadMode result = QrGenerator.DetectPayloadMode(input);
        Assert.Equal(expected, result);
    }
}