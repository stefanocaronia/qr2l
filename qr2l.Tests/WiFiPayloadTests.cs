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

    [Theory]
    [InlineData("Casa", "segreta", WiFiAuthenticationType.WPA, false)]
    [InlineData("Bar: Da Mario", "pa;ss,wo\"rd\\", WiFiAuthenticationType.WPA, true)]
    [InlineData("Vecchia", "chiave", WiFiAuthenticationType.WEP, false)]
    [InlineData("Aperta", "", WiFiAuthenticationType.NoPassword, false)]
    public void BuildThenParse_ShouldGiveBackTheSameNetwork(string ssid, string password, WiFiAuthenticationType authentication, bool hidden)
    {
        var original = new WiFiNetwork(ssid, password, authentication, hidden);

        Assert.True(Payloads.TryParseWiFi(Payloads.BuildWiFi(original), out WiFiNetwork network));
        Assert.Equal(original, network);
    }

    [Fact]
    public void Parse_ShouldAcceptFieldsInAnyOrder()
    {
        Assert.True(Payloads.TryParseWiFi("WIFI:S:Casa;H:true;P:segreta;T:WPA;;", out WiFiNetwork network));
        Assert.Equal(new WiFiNetwork("Casa", "segreta", WiFiAuthenticationType.WPA, true), network);
    }

    [Fact]
    public void Parse_ShouldReadTheSimplifiedFormat()
    {
        Assert.True(Payloads.TryParseWiFi("WIFI:Casa;ab;cd", out WiFiNetwork network));
        Assert.Equal(new WiFiNetwork("Casa", "ab;cd", WiFiAuthenticationType.WPA, false), network);
    }

    [Fact]
    public void Parse_ShouldReadTextWithoutPrefixAsSimplified()
    {
        Assert.True(Payloads.TryParseWiFi("Casa;segreta", out WiFiNetwork network));
        Assert.Equal(new WiFiNetwork("Casa", "segreta", WiFiAuthenticationType.WPA, false), network);
    }

    [Theory]
    [InlineData("WIFI:")]
    [InlineData("WIFI:T:WPA;P:segreta;;")]
    public void Parse_ShouldLeaveTheNameEmptyWhenMissing(string text)
    {
        Assert.True(Payloads.TryParseWiFi(text, out WiFiNetwork network));
        Assert.Equal(string.Empty, network.Ssid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ShouldRejectEmptyText(string text)
    {
        Assert.False(Payloads.TryParseWiFi(text, out WiFiNetwork _));
    }

    [Theory]
    [InlineData("WIFI:T:WPA;S:;P:segreta;;")]
    [InlineData("WIFI:T:nopass;;")]
    [InlineData("WIFI:;segreta")]
    public void CodeWithoutNetworkName_ShouldBeRejected(string text)
    {
        Assert.Throws<ArgumentException>(() => Payload(text));
    }
}
