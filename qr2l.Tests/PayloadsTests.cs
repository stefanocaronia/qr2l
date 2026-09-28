using qr2l.Core;
using Xunit;

namespace qr2l.Tests;

/// <summary>
/// Composizione e lettura dei contenuti con più campi: quello che i moduli scrivono deve rileggersi uguale,
/// e i formati completi incollati nella casella devono riempire i campi giusti.
/// </summary>
public class PayloadsTests
{
    #region Mail

    [Fact]
    public void Mail_ShouldEncodeSubjectAndBody()
    {
        string payload = Payloads.BuildMail(new MailMessage("info@example.com", "Ciao a tutti", "Riga 1\nRiga 2 & altro"));

        Assert.Equal("mailto:info@example.com?subject=Ciao%20a%20tutti&body=Riga%201%0ARiga%202%20%26%20altro", payload);
    }

    [Fact]
    public void Mail_WithoutSubjectAndBody_ShouldBeJustTheAddress()
    {
        Assert.Equal("mailto:info@example.com", Payloads.BuildMail(new MailMessage("info@example.com", "", "")));
    }

    [Theory]
    [InlineData("info@example.com", "", "")]
    [InlineData("info@example.com", "Oggetto; con punto e virgola", "")]
    [InlineData("info@example.com", "", "Messaggio & simboli: 50% ?")]
    public void Mail_BuildThenParse_ShouldGiveBackTheSameMessage(string address, string subject, string body)
    {
        var original = new MailMessage(address, subject, body);

        Assert.True(Payloads.TryParseMail(Payloads.BuildMail(original), out MailMessage mail));
        Assert.Equal(original, mail);
    }

    [Theory]
    [InlineData("info@example.com;Oggetto;Testo; anche con punto e virgola", "info@example.com", "Oggetto", "Testo; anche con punto e virgola")]
    [InlineData("MATMSG:TO:info@example.com;SUB:Oggetto;BODY:Testo;;", "info@example.com", "Oggetto", "Testo")]
    [InlineData("mario", "mario", "", "")]
    public void Mail_ShouldReadOtherFormats(string text, string address, string subject, string body)
    {
        Assert.True(Payloads.TryParseMail(text, out MailMessage mail));
        Assert.Equal(new MailMessage(address, subject, body), mail);
    }

    #endregion

    #region SMS and WhatsApp

    [Fact]
    public void Sms_ShouldDropSpacesFromTheNumber()
    {
        Assert.Equal("sms:+393331234567?body=Ciao%2C%20come%20va%3F", Payloads.BuildSms(new TextMessage("+39 333 123 45 67", "Ciao, come va?")));
    }

    [Theory]
    [InlineData("sms:+393331234567?body=Ciao%20Mario", "+393331234567", "Ciao Mario")]
    [InlineData("SMSTO:+393331234567:Ciao: tutto bene", "+393331234567", "Ciao: tutto bene")]
    [InlineData("333 1234567;Ciao", "333 1234567", "Ciao")]
    [InlineData("sms:3331234567", "3331234567", "")]
    public void Sms_ShouldReadEveryFormat(string text, string number, string message)
    {
        Assert.True(Payloads.TryParseSms(text, out TextMessage sms));
        Assert.Equal(new TextMessage(number, message), sms);
    }

    [Fact]
    public void WhatsApp_ShouldUseOnlyTheDigitsOfTheNumber()
    {
        Assert.Equal("https://wa.me/393331234567?text=Ciao%20%26%20benvenuto", Payloads.BuildWhatsApp(new TextMessage("+39 333 1234567", "Ciao & benvenuto")));
    }

    [Theory]
    [InlineData("https://wa.me/393331234567?text=Ciao%20Mario", "+393331234567", "Ciao Mario")]
    [InlineData("https://api.whatsapp.com/send?phone=393331234567&text=Ciao", "+393331234567", "Ciao")]
    [InlineData("whatsapp://send?phone=+393331234567", "+393331234567", "")]
    [InlineData("+39 333 1234567;Ciao", "+39 333 1234567", "Ciao")]
    public void WhatsApp_ShouldReadEveryFormat(string text, string number, string message)
    {
        Assert.True(Payloads.TryParseWhatsApp(text, out TextMessage parsed));
        Assert.Equal(new TextMessage(number, message), parsed);
    }

    #endregion

    #region Contact

    [Fact]
    public void Contact_ShouldBeAVCardWithProtectedSeparators()
    {
        string payload = Payloads.BuildContact(new ContactCard("Mario", "Rossi", "+39 02 1234567", "mario@example.com", "Rossi; Bianchi, Srl", "https://example.com"));

        Assert.Equal(
            "BEGIN:VCARD\r\nVERSION:3.0\r\nN:Rossi;Mario;;;\r\nFN:Mario Rossi\r\nORG:Rossi\\; Bianchi\\, Srl\r\n" +
            "TEL:+39 02 1234567\r\nEMAIL:mario@example.com\r\nURL:https://example.com\r\nEND:VCARD",
            payload);
    }

    [Fact]
    public void Contact_WithoutName_ShouldBeFiledUnderTheCompany()
    {
        Assert.Contains("FN:ACME\r\n", Payloads.BuildContact(new ContactCard("", "", "", "", "ACME", "")));
    }

    [Theory]
    [InlineData("Mario", "Rossi", "+39 02 1234567", "mario@example.com", "ACME", "https://example.com")]
    [InlineData("Anna Maria", "De Luca", "", "", "", "")]
    [InlineData("Luca", "", "", "", "Bar; Da Mario, Milano", "")]
    public void Contact_BuildThenParse_ShouldGiveBackTheSameCard(string first, string last, string phone, string email, string organization, string website)
    {
        var original = new ContactCard(first, last, phone, email, organization, website);

        Assert.True(Payloads.TryParseContact(Payloads.BuildContact(original), out ContactCard card));
        Assert.Equal(original, card);
    }

    [Fact]
    public void Contact_ShouldReadVCardsFromOtherApps()
    {
        string vcard = "BEGIN:VCARD\nVERSION:2.1\nN;CHARSET=UTF-8:Rossi;Mario\nTEL;TYPE=CELL:+39 333 1234567\nTEL;TYPE=WORK:+39 02 7654321\n" +
                       "EMAIL;TYPE=INTERNET:mario@example.com\nADR:;;Via Roma 1;Milano;;;\nEND:VCARD";

        Assert.True(Payloads.TryParseContact(vcard, out ContactCard card));
        Assert.Equal(new ContactCard("Mario", "Rossi", "+39 333 1234567", "mario@example.com", "", ""), card);
    }

    [Theory]
    [InlineData("MECARD:N:Rossi,Mario;TEL:+39021234567;EMAIL:mario@example.com;;", "Mario", "Rossi", "+39021234567", "mario@example.com")]
    [InlineData("Mario;Rossi;+39 02 1234567;mario@example.com", "Mario", "Rossi", "+39 02 1234567", "mario@example.com")]
    [InlineData("Mario", "Mario", "", "", "")]
    public void Contact_ShouldReadOtherFormats(string text, string first, string last, string phone, string email)
    {
        Assert.True(Payloads.TryParseContact(text, out ContactCard card));
        Assert.Equal(new ContactCard(first, last, phone, email, "", ""), card);
    }

    #endregion

    #region Event

    [Fact]
    public void Event_ShouldBeAVEventWithLocalTimes()
    {
        var entry = new CalendarEntry("Cena, amici", "Porta il vino; e il dolce", "Milano", new DateTime(2026, 10, 1, 18, 0, 0), new DateTime(2026, 10, 1, 20, 30, 0), false);

        Assert.Equal(
            "BEGIN:VEVENT\r\nSUMMARY:Cena\\, amici\r\nDESCRIPTION:Porta il vino\\; e il dolce\r\nLOCATION:Milano\r\n" +
            "DTSTART:20261001T180000\r\nDTEND:20261001T203000\r\nEND:VEVENT",
            Payloads.BuildEvent(entry));
    }

    [Fact]
    public void AllDayEvent_ShouldEndTheDayAfterTheLastOne()
    {
        var entry = new CalendarEntry("Ferie", "", "", new DateTime(2026, 8, 10), new DateTime(2026, 8, 20), true);

        Assert.Equal(
            "BEGIN:VEVENT\r\nSUMMARY:Ferie\r\nDTSTART;VALUE=DATE:20260810\r\nDTEND;VALUE=DATE:20260821\r\nEND:VEVENT",
            Payloads.BuildEvent(entry));
    }

    [Fact]
    public void Event_EndingBeforeTheStart_ShouldEndAtTheStart()
    {
        var start = new DateTime(2026, 10, 1, 18, 0, 0);

        Assert.Contains("DTEND:20261001T180000", Payloads.BuildEvent(new CalendarEntry("Cena", "", "", start, start.AddHours(-2), false)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Event_BuildThenParse_ShouldGiveBackTheSameEntry(bool allDay)
    {
        var original = allDay
            ? new CalendarEntry("Ferie", "Mare, finalmente", "Sardegna", new DateTime(2026, 8, 10), new DateTime(2026, 8, 20), true)
            : new CalendarEntry("Cena; amici", "Riga 1\nRiga 2", "Via Roma 1, Milano", new DateTime(2026, 10, 1, 18, 0, 0), new DateTime(2026, 10, 1, 20, 30, 0), false);

        Assert.True(Payloads.TryParseEvent(Payloads.BuildEvent(original), out CalendarEntry entry));
        Assert.Equal(original, entry);
    }

    [Fact]
    public void Event_ShouldReadTheSimplifiedFormat()
    {
        Assert.True(Payloads.TryParseEvent("Cena;Porta il vino;Milano;2026-10-01 18:00;2026-10-01 20:30", out CalendarEntry entry));
        Assert.Equal(new CalendarEntry("Cena", "Porta il vino", "Milano", new DateTime(2026, 10, 1, 18, 0, 0), new DateTime(2026, 10, 1, 20, 30, 0), false), entry);
    }

    [Fact]
    public void Event_WithOnlyADate_ShouldLastAllDay()
    {
        Assert.True(Payloads.TryParseEvent("Ferie;;;2026-08-10", out CalendarEntry entry));
        Assert.True(entry.AllDay);
        Assert.Equal(new DateTime(2026, 8, 10), entry.End);
    }

    [Fact]
    public void Event_ShouldReadACalendarFromOtherApps()
    {
        string calendar = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nSUMMARY:Riunione\r\nDTSTART:20261001T090000\r\n" +
                          "DTEND:20261001T100000\r\nLOCATION:Sala 2\r\nEND:VEVENT\r\nEND:VCALENDAR";

        Assert.True(Payloads.TryParseEvent(calendar, out CalendarEntry entry));
        Assert.Equal(new CalendarEntry("Riunione", "", "Sala 2", new DateTime(2026, 10, 1, 9, 0, 0), new DateTime(2026, 10, 1, 10, 0, 0), false), entry);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Cena")]
    [InlineData("Cena;;Milano;domani sera")]
    [InlineData("BEGIN:VEVENT\r\nSUMMARY:Senza data\r\nEND:VEVENT")]
    public void Event_WithoutAValidStart_ShouldNotBeRead(string text)
    {
        Assert.False(Payloads.TryParseEvent(text, out CalendarEntry _));
    }

    #endregion

    #region Detection and preparation

    [Theory]
    [InlineData("mailto:info@example.com?subject=Ciao", PayloadMode.Mail)]
    [InlineData("MATMSG:TO:info@example.com;;", PayloadMode.Mail)]
    [InlineData("info@example.com;Oggetto;Messaggio", PayloadMode.Mail)]
    [InlineData("sms:+393331234567?body=Ciao", PayloadMode.SMS)]
    [InlineData("SMSTO:+393331234567:Ciao", PayloadMode.SMS)]
    [InlineData("https://wa.me/393331234567?text=Ciao", PayloadMode.WhatsApp)]
    [InlineData("tel:+39021234567", PayloadMode.Phone)]
    [InlineData("geo:45.4642,9.19", PayloadMode.Geolocation)]
    [InlineData("BEGIN:VCARD\r\nVERSION:3.0\r\nN:Rossi;Mario;;;\r\nEND:VCARD", PayloadMode.ContactData)]
    [InlineData("MECARD:N:Rossi,Mario;;", PayloadMode.ContactData)]
    [InlineData("BEGIN:VEVENT\r\nSUMMARY:Cena\r\nEND:VEVENT", PayloadMode.Event)]
    [InlineData("Scrivimi a mario@example.com", PayloadMode.Text)]
    public void Detection_ShouldRecognizeCompleteFormats(string text, PayloadMode expected)
    {
        Assert.Equal(expected, QrGenerator.DetectPayloadMode(text));
    }

    [Theory]
    [InlineData(PayloadMode.Mail, "mailto:info@example.com?subject=Ciao%20Mario")]
    [InlineData(PayloadMode.SMS, "SMSTO:+393331234567:Ciao")]
    [InlineData(PayloadMode.WhatsApp, "https://wa.me/393331234567")]
    [InlineData(PayloadMode.Phone, "tel:+39021234567")]
    [InlineData(PayloadMode.Geolocation, "geo:45.4642,9.19")]
    [InlineData(PayloadMode.ContactData, "BEGIN:VCARD\r\nVERSION:3.0\r\nN:Rossi;Mario;;;\r\nADR:;;Via Roma 1;;;;\r\nEND:VCARD")]
    [InlineData(PayloadMode.Event, "BEGIN:VEVENT\r\nSUMMARY:Cena\r\nDTSTART:20261001T180000\r\nEND:VEVENT")]
    public void CompleteFormats_ShouldBeEncodedAsWritten(PayloadMode mode, string text)
    {
        Assert.Equal(text, QrGenerator.PreparePayload(text, mode));
    }

    [Theory]
    [InlineData(PayloadMode.Mail, "info@example.com;Ciao", "mailto:info@example.com?subject=Ciao")]
    [InlineData(PayloadMode.SMS, "333 1234567;Ciao", "sms:3331234567?body=Ciao")]
    [InlineData(PayloadMode.WhatsApp, "+39 333 1234567;Ciao", "https://wa.me/393331234567?text=Ciao")]
    [InlineData(PayloadMode.ContactData, "Mario;Rossi", "BEGIN:VCARD\r\nVERSION:3.0\r\nN:Rossi;Mario;;;\r\nFN:Mario Rossi\r\nEND:VCARD")]
    [InlineData(PayloadMode.Event, "Cena;;;2026-10-01 18:00", "BEGIN:VEVENT\r\nSUMMARY:Cena\r\nDTSTART:20261001T180000\r\nDTEND:20261001T190000\r\nEND:VEVENT")]
    public void SimplifiedFormats_ShouldBeExpanded(PayloadMode mode, string text, string expected)
    {
        Assert.Equal(expected, QrGenerator.PreparePayload(text, mode));
    }

    [Fact]
    public void EventWithoutDate_ShouldBeRejected()
    {
        Assert.Throws<ArgumentException>(() => QrGenerator.PreparePayload("Cena;;Milano", PayloadMode.Event));
    }

    #endregion
}
