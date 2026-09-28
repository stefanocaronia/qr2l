using qr2l.Core;
using Xunit;

namespace qr2l.Tests;

/// <summary>
/// Il testo che finisce nel QR per le reti WiFi, nel formato letto dai telefoni.
/// </summary>
public class WiFiPayloadTests
{
    private static string Payload(string text, QrCodeOptions? options = null)
    {
        return QrGenerator.PreparePayload(text, PayloadMode.WiFi, options ?? new QrCodeOptions());
    }

    [Theory]
    [InlineData("WIFI:Casa")]
    [InlineData("WIFI:Casa;")]
    public void OpenNetwork_ShouldBeEncodedWithoutProtection(string text)
    {
        Assert.Equal("WIFI:T:nopass;S:Casa;;", Payload(text));
    }

    [Fact]
    public void OpenNetwork_ShouldIgnoreTheChosenProtection()
    {
        Assert.Equal("WIFI:T:nopass;S:Casa;;", Payload("WIFI:Casa;", new QrCodeOptions { wifiAuthType = WiFiAuthenticationType.WEP }));
    }

    [Fact]
    public void ProtectedNetwork_ShouldUseWpaByDefault()
    {
        Assert.Equal("WIFI:T:WPA;S:Casa;P:segreta;;", Payload("WIFI:Casa;segreta"));
    }

    [Fact]
    public void WepNetwork_ShouldKeepWep()
    {
        Assert.Equal("WIFI:T:WEP;S:Vecchia;P:chiave;;", Payload("WIFI:Vecchia;chiave", new QrCodeOptions { wifiAuthType = WiFiAuthenticationType.WEP }));
    }

    [Fact]
    public void HiddenNetwork_ShouldBeMarked()
    {
        Assert.Equal("WIFI:T:WPA;S:Casa;P:segreta;H:true;;", Payload("WIFI:Casa;segreta", new QrCodeOptions { wifiHidden = true }));
    }

    [Fact]
    public void SpecialCharacters_ShouldBeEscaped()
    {
        string payload = Payload("WIFI:Bar: Da Mario;pa:ss,wo\"rd\\");

        Assert.Equal("WIFI:T:WPA;S:Bar\\: Da Mario;P:pa\\:ss\\,wo\\\"rd\\\\;;", payload);
    }

    [Fact]
    public void PasswordWithSemicolons_ShouldBeKeptWhole()
    {
        Assert.Equal("WIFI:T:WPA;S:Casa;P:ab\\;cd\\;ef;;", Payload("WIFI:Casa;ab;cd;ef"));
    }

    [Fact]
    public void SimplifiedFormatWithoutPrefix_ShouldWorkWhenWiFiIsChosen()
    {
        Assert.Equal("WIFI:T:WPA;S:Casa;P:segreta;;", Payload("Casa;segreta"));
    }

    [Fact]
    public void CompleteFormat_ShouldBeKeptAsWritten()
    {
        Assert.Equal("WIFI:T:WEP;S:Rete;P:abc;H:true;;", Payload("WIFI:T:WEP;S:Rete;P:abc;H:true;;"));
    }
}
