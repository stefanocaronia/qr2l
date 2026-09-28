using System.Globalization;
using System.Text;

namespace qr2l.Core;

/// <summary>
/// I dati di una rete WiFi, come li legge un telefono da un codice QR.
/// </summary>
public readonly record struct WiFiNetwork(string Ssid, string Password, WiFiAuthenticationType Authentication, bool Hidden);

/// <summary>
/// Un'email da scrivere: destinatario, oggetto e messaggio.
/// </summary>
public readonly record struct MailMessage(string Address, string Subject, string Body);

/// <summary>
/// Un messaggio per un numero di telefono, via SMS o WhatsApp.
/// </summary>
public readonly record struct TextMessage(string Number, string Message);

/// <summary>
/// Un contatto da biglietto da visita.
/// </summary>
public readonly record struct ContactCard(string FirstName, string LastName, string Phone, string Email, string Organization, string Website);

/// <summary>
/// Un evento di calendario. Per gli eventi di tutto il giorno contano solo le date, e la fine è l'ultimo giorno compreso.
/// </summary>
public readonly record struct CalendarEntry(string Title, string Description, string Location, DateTime Start, DateTime End, bool AllDay);

/// <summary>
/// Scrittura (Build) e lettura (TryParse) dei contenuti con più campi, che tengono allineati testo e moduli.
/// TryParse legge formati completi e semplificati e riesce con ogni testo non vuoto, tranne un evento senza data.
/// </summary>
public static class Payloads
{
    #region WiFi

    /// <summary>
    /// Compone il testo che i telefoni leggono per collegarsi a una rete:
    /// <c>WIFI:T:protezione;S:rete;P:password;H:true;;</c>, dove H compare solo per le reti nascoste.
    /// </summary>
    public static string BuildWiFi(WiFiNetwork network)
    {
        // Senza password la rete è aperta, qualunque protezione sia stata indicata
        WiFiAuthenticationType effective = network.Password.Length == 0 ? WiFiAuthenticationType.NoPassword : network.Authentication;

        string type = effective switch {
            WiFiAuthenticationType.WEP => "WEP",
            WiFiAuthenticationType.NoPassword => "nopass",
            var _ => "WPA"
        };

        var payload = new StringBuilder($"WIFI:T:{type};S:{EscapeWiFiValue(network.Ssid)};");

        if (effective != WiFiAuthenticationType.NoPassword) {
            payload.Append($"P:{EscapeWiFiValue(network.Password)};");
        }

        if (network.Hidden) {
            payload.Append("H:true;");
        }

        return payload.Append(';').ToString();
    }

    /// <summary>
    /// Rilegge una rete dal formato completo o da quello semplificato "rete;password", con o senza "WIFI:" davanti.
    /// </summary>
    public static bool TryParseWiFi(string text, out WiFiNetwork network)
    {
        network = default;
        text = text.Trim();

        if (text.Length == 0) {
            return false;
        }

        if (!IsCompleteWiFiFormat(text)) {
            string body = text.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase) ? text[5..] : text;
            (string ssid, string password) = SplitSimplifiedWiFi(body);
            WiFiAuthenticationType simple = password.Length == 0 ? WiFiAuthenticationType.NoPassword : WiFiAuthenticationType.WPA;

            network = new WiFiNetwork(ssid, password, simple, false);
            return true;
        }

        string parsedSsid = string.Empty;
        string parsedPassword = string.Empty;
        var authentication = WiFiAuthenticationType.NoPassword;
        var hidden = false;

        foreach (string field in SplitUnescaped(text[5..], ';')) {
            if (field.Length < 2 || field[1] != ':') {
                continue;
            }

            string value = Unescape(field[2..], false);

            switch (char.ToUpperInvariant(field[0])) {
                case 'T':
                    authentication = value.ToUpperInvariant() switch {
                        "WEP" => WiFiAuthenticationType.WEP,
                        "" or "NOPASS" => WiFiAuthenticationType.NoPassword,
                        var _ => WiFiAuthenticationType.WPA
                    };
                    break;

                case 'S':
                    parsedSsid = value;
                    break;

                case 'P':
                    parsedPassword = value;
                    break;

                case 'H':
                    hidden = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }

        network = new WiFiNetwork(parsedSsid, parsedPassword, authentication, hidden);
        return true;
    }

    /// <summary>
    /// Il formato completo ha campi con etichetta (T:, S:, P:, H:), in qualunque ordine.
    /// </summary>
    internal static bool IsCompleteWiFiFormat(string text)
    {
        if (!text.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase) || text.Length < 7) {
            return false;
        }

        char key = char.ToUpperInvariant(text[5]);
        return text[6] == ':' && key is 'T' or 'S' or 'P' or 'H';
    }

    /// <summary>
    /// Formato semplificato "rete;password": la rete arriva fino al primo punto e virgola e tutto
    /// il resto è la password, che quindi può contenere a sua volta dei punti e virgola.
    /// </summary>
    internal static (string Ssid, string Password) SplitSimplifiedWiFi(string text)
    {
        int separator = text.IndexOf(';');
        string ssid = (separator < 0 ? text : text[..separator]).Trim();
        string password = separator < 0 ? string.Empty : text[(separator + 1)..].Trim();
        return (ssid, password);
    }

    /// <summary>
    /// Nel formato WiFi i caratteri \ ; , : e " fanno da separatori: dentro nome e password
    /// vanno preceduti da una barra rovesciata, altrimenti il telefono legge un valore sbagliato.
    /// </summary>
    private static string EscapeWiFiValue(string value)
    {
        var escaped = new StringBuilder(value.Length);

        foreach (char character in value) {
            if (character is '\\' or ';' or ',' or ':' or '"') {
                escaped.Append('\\');
            }

            escaped.Append(character);
        }

        return escaped.ToString();
    }

    #endregion

    #region Mail

    /// <summary>
    /// <c>mailto:indirizzo?subject=oggetto&amp;body=messaggio</c>, con oggetto e messaggio solo se presenti.
    /// </summary>
    public static string BuildMail(MailMessage mail)
    {
        var query = new List<string>();

        if (mail.Subject.Length > 0) {
            query.Add("subject=" + Uri.EscapeDataString(mail.Subject));
        }

        if (mail.Body.Length > 0) {
            query.Add("body=" + Uri.EscapeDataString(mail.Body));
        }

        return "mailto:" + mail.Address.Trim() + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
    }

    /// <summary>
    /// Rilegge un'email da un link mailto:, dal formato MATMSG o da "indirizzo;oggetto;messaggio".
    /// </summary>
    public static bool TryParseMail(string text, out MailMessage mail)
    {
        mail = default;
        text = text.Trim();

        if (text.Length == 0) {
            return false;
        }

        if (text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) {
            string rest = text[7..];
            int question = rest.IndexOf('?');
            Dictionary<string, string> query = ParseQuery(question < 0 ? string.Empty : rest[(question + 1)..]);

            mail = new MailMessage(
                Uri.UnescapeDataString(question < 0 ? rest : rest[..question]),
                query.GetValueOrDefault("subject", string.Empty),
                query.GetValueOrDefault("body", string.Empty));
            return true;
        }

        if (text.StartsWith("MATMSG:", StringComparison.OrdinalIgnoreCase)) {
            Dictionary<string, string> fields = ReadLabeledFields(text[7..]);

            mail = new MailMessage(
                fields.GetValueOrDefault("TO", string.Empty),
                fields.GetValueOrDefault("SUB", string.Empty),
                fields.GetValueOrDefault("BODY", string.Empty));
            return true;
        }

        // Semplificato: il messaggio è tutto quello che segue l'oggetto, punti e virgola compresi
        string[] parts = text.Split(';', 3);

        mail = new MailMessage(
            parts[0].Trim(),
            parts.Length > 1 ? parts[1].Trim() : string.Empty,
            parts.Length > 2 ? parts[2].Trim() : string.Empty);
        return true;
    }

    /// <summary>
    /// Un indirizzo email plausibile: una sola chiocciola, niente spazi, un dominio con il punto.
    /// </summary>
    internal static bool LooksLikeEmail(string text)
    {
        int at = text.IndexOf('@');

        return at > 0 &&
               at == text.LastIndexOf('@') &&
               !text.Any(char.IsWhiteSpace) &&
               text.IndexOf('.', at) > at + 1 &&
               !text.EndsWith('.');
    }

    #endregion

    #region SMS and WhatsApp

    /// <summary>
    /// <c>sms:numero?body=messaggio</c>. Il numero perde spazi e separatori, che romperebbero il link.
    /// </summary>
    public static string BuildSms(TextMessage sms)
    {
        string number = NormalizePhone(sms.Number);
        return sms.Message.Length > 0 ? $"sms:{number}?body={Uri.EscapeDataString(sms.Message)}" : $"sms:{number}";
    }

    /// <summary>
    /// Rilegge un SMS da un link sms:, dal formato SMSTO o da "numero;messaggio".
    /// </summary>
    public static bool TryParseSms(string text, out TextMessage sms)
    {
        sms = default;
        text = text.Trim();

        if (text.Length == 0) {
            return false;
        }

        if (text.StartsWith("SMSTO:", StringComparison.OrdinalIgnoreCase)) {
            string rest = text[6..];
            int colon = rest.IndexOf(':');

            sms = new TextMessage(colon < 0 ? rest : rest[..colon], colon < 0 ? string.Empty : rest[(colon + 1)..]);
            return true;
        }

        if (text.StartsWith("sms:", StringComparison.OrdinalIgnoreCase)) {
            // A seconda del sistema il messaggio segue ?body=, &body= o ;body=
            string rest = text[4..];
            int separator = rest.IndexOfAny(['?', '&', ';']);
            Dictionary<string, string> query = ParseQuery(separator < 0 ? string.Empty : rest[(separator + 1)..]);

            sms = new TextMessage(Uri.UnescapeDataString(separator < 0 ? rest : rest[..separator]), query.GetValueOrDefault("body", string.Empty));
            return true;
        }

        (string number, string message) = SplitFirst(text);
        sms = new TextMessage(number, message);
        return true;
    }

    /// <summary>
    /// <c>https://wa.me/numero?text=messaggio</c>. WhatsApp vuole il numero internazionale, di sole cifre.
    /// </summary>
    public static string BuildWhatsApp(TextMessage message)
    {
        string digits = new(message.Number.Where(char.IsAsciiDigit).ToArray());
        string link = $"https://wa.me/{digits}";
        return message.Message.Length > 0 ? $"{link}?text={Uri.EscapeDataString(message.Message)}" : link;
    }

    /// <summary>
    /// Rilegge un messaggio da un link wa.me, api.whatsapp.com o whatsapp://, oppure da "+numero;messaggio".
    /// </summary>
    public static bool TryParseWhatsApp(string text, out TextMessage message)
    {
        message = default;
        text = text.Trim();

        if (text.Length == 0) {
            return false;
        }

        if (IsWhatsAppLink(text) && Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)) {
            Dictionary<string, string> query = ParseQuery(uri.Query.TrimStart('?'));
            string number = uri.Host.Equals("wa.me", StringComparison.OrdinalIgnoreCase)
                ? uri.AbsolutePath.Trim('/')
                : query.GetValueOrDefault("phone", string.Empty);

            number = number.TrimStart('+');
            message = new TextMessage(number.Length > 0 ? "+" + number : string.Empty, query.GetValueOrDefault("text", string.Empty));
            return true;
        }

        (string simpleNumber, string simpleMessage) = SplitFirst(text);
        message = new TextMessage(simpleNumber, simpleMessage);
        return true;
    }

    internal static bool IsWhatsAppLink(string text)
    {
        return StartsWithAny(text, "https://wa.me/", "http://wa.me/", "https://api.whatsapp.com/send", "http://api.whatsapp.com/send", "whatsapp://send");
    }

    /// <summary>
    /// Toglie spazi e separatori di lettura dal numero, lasciando le cifre e l'eventuale + iniziale.
    /// </summary>
    internal static string NormalizePhone(string number)
    {
        var normalized = new StringBuilder();

        foreach (char character in number.Trim()) {
            if (char.IsAsciiDigit(character) || (character == '+' && normalized.Length == 0)) {
                normalized.Append(character);
            }
        }

        return normalized.ToString();
    }

    #endregion

    #region Contact

    /// <summary>
    /// Biglietto da visita in formato vCard 3.0, con virgole, punti e virgola e a capo protetti come vuole il formato.
    /// </summary>
    public static string BuildContact(ContactCard card)
    {
        string first = card.FirstName.Trim();
        string last = card.LastName.Trim();
        string fullName = $"{first} {last}".Trim();

        if (fullName.Length == 0) {
            fullName = card.Organization.Trim();
        }

        var lines = new List<string> {
            "BEGIN:VCARD",
            "VERSION:3.0",
            $"N:{EscapeText(last)};{EscapeText(first)};;;",
            $"FN:{EscapeText(fullName)}"
        };

        AddIfPresent(lines, "ORG", EscapeText(card.Organization.Trim()));
        AddIfPresent(lines, "TEL", card.Phone.Trim());
        AddIfPresent(lines, "EMAIL", card.Email.Trim());
        AddIfPresent(lines, "URL", card.Website.Trim());
        lines.Add("END:VCARD");

        return string.Join("\r\n", lines);
    }

    /// <summary>
    /// Rilegge un contatto da una vCard, da una MECARD o da "nome;cognome;telefono;email;azienda;sito".
    /// </summary>
    public static bool TryParseContact(string text, out ContactCard card)
    {
        card = default;
        text = text.Trim();

        if (text.Length == 0) {
            return false;
        }

        if (text.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase)) {
            card = ParseVCard(text);
            return true;
        }

        if (text.StartsWith("MECARD:", StringComparison.OrdinalIgnoreCase)) {
            Dictionary<string, string> fields = ReadLabeledFields(text[7..]);
            string[] name = fields.GetValueOrDefault("N", string.Empty).Split(',', 2);

            card = new ContactCard(
                name.Length > 1 ? name[1].Trim() : string.Empty,
                name[0].Trim(),
                fields.GetValueOrDefault("TEL", string.Empty),
                fields.GetValueOrDefault("EMAIL", string.Empty),
                fields.GetValueOrDefault("ORG", string.Empty),
                fields.GetValueOrDefault("URL", string.Empty));
            return true;
        }

        string[] parts = text.Split(';');
        string Part(int index) => parts.Length > index ? parts[index].Trim() : string.Empty;

        card = new ContactCard(Part(0), Part(1), Part(2), Part(3), Part(4), Part(5));
        return true;
    }

    private static ContactCard ParseVCard(string text)
    {
        string first = string.Empty, last = string.Empty, formatted = string.Empty;
        string phone = string.Empty, email = string.Empty, organization = string.Empty, website = string.Empty;

        // Di telefono, email e sito si tiene il primo: il modulo ne ha uno solo
        foreach ((string name, string value) in ReadProperties(text)) {
            switch (name) {
                case "N": {
                    List<string> parts = SplitUnescaped(value, ';');
                    last = parts.Count > 0 ? Unescape(parts[0], true) : string.Empty;
                    first = parts.Count > 1 ? Unescape(parts[1], true) : string.Empty;
                    break;
                }

                case "FN":
                    formatted = Unescape(value, true);
                    break;

                case "ORG":
                    organization = Unescape(SplitUnescaped(value, ';').FirstOrDefault() ?? string.Empty, true);
                    break;

                case "TEL" when phone.Length == 0:
                    phone = value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ? value[4..] : value;
                    break;

                case "EMAIL" when email.Length == 0:
                    email = value;
                    break;

                case "URL" when website.Length == 0:
                    website = Unescape(value, false);
                    break;
            }
        }

        // Solo FN, senza N: il cognome è l'ultima parola
        if (first.Length == 0 && last.Length == 0 && formatted.Length > 0 && formatted != organization) {
            int space = formatted.LastIndexOf(' ');
            first = space < 0 ? formatted : formatted[..space];
            last = space < 0 ? string.Empty : formatted[(space + 1)..];
        }

        return new ContactCard(first, last, phone, email, organization, website);
    }

    #endregion

    #region Event

    /// <summary>
    /// Evento in formato iCalendar, con orari locali. Per gli eventi di tutto il giorno si indicano solo le date,
    /// e la fine è il giorno dopo l'ultimo, come vuole il formato.
    /// </summary>
    public static string BuildEvent(CalendarEntry entry)
    {
        var lines = new List<string> { "BEGIN:VEVENT", $"SUMMARY:{EscapeText(entry.Title.Trim())}" };

        AddIfPresent(lines, "DESCRIPTION", EscapeText(entry.Description.Trim()));
        AddIfPresent(lines, "LOCATION", EscapeText(entry.Location.Trim()));

        if (entry.AllDay) {
            DateTime end = entry.End.Date < entry.Start.Date ? entry.Start.Date : entry.End.Date;
            lines.Add($"DTSTART;VALUE=DATE:{FormatDate(entry.Start)}");
            lines.Add($"DTEND;VALUE=DATE:{FormatDate(end.AddDays(1))}");
        } else {
            DateTime end = entry.End < entry.Start ? entry.Start : entry.End;
            lines.Add($"DTSTART:{FormatDateTime(entry.Start)}");
            lines.Add($"DTEND:{FormatDateTime(end)}");
        }

        lines.Add("END:VEVENT");
        return string.Join("\r\n", lines);
    }

    /// <summary>
    /// Rilegge un evento da iCalendar (anche dentro un VCALENDAR) o da "titolo;descrizione;luogo;inizio;fine".
    /// Senza una data d'inizio valida non c'è evento.
    /// </summary>
    public static bool TryParseEvent(string text, out CalendarEntry entry)
    {
        entry = default;
        text = text.Trim();

        if (StartsWithAny(text, "BEGIN:VEVENT", "BEGIN:VCALENDAR")) {
            return TryParseVEvent(text, out entry);
        }

        string[] fields = text.Split(';');

        if (fields.Length < 4 || !TryParseLooseDate(fields[3], out DateTime start)) {
            return false;
        }

        // Una data senza ora vale per tutto il giorno
        bool allDay = !fields[3].Contains(':');
        DateTime end = fields.Length > 4 && TryParseLooseDate(fields[4], out DateTime parsedEnd)
            ? parsedEnd
            : allDay ? start : start.AddHours(1);

        entry = new CalendarEntry(fields[0].Trim(), fields[1].Trim(), fields[2].Trim(), start, end, allDay);
        return true;
    }

    private static bool TryParseVEvent(string text, out CalendarEntry entry)
    {
        entry = default;

        string title = string.Empty, description = string.Empty, location = string.Empty;
        (DateTime Value, bool DateOnly)? start = null, end = null;

        foreach ((string name, string value) in ReadProperties(text)) {
            switch (name) {
                case "SUMMARY":
                    title = Unescape(value, true);
                    break;

                case "DESCRIPTION":
                    description = Unescape(value, true);
                    break;

                case "LOCATION":
                    location = Unescape(value, true);
                    break;

                case "DTSTART":
                    start = ParseCalendarDate(value);
                    break;

                case "DTEND":
                    end = ParseCalendarDate(value);
                    break;
            }
        }

        if (start is not { } begin) {
            return false;
        }

        DateTime last;

        if (begin.DateOnly) {
            // Nel formato la fine è esclusa: l'ultimo giorno compreso è quello prima
            last = end is { } finish ? finish.Value.Date.AddDays(-1) : begin.Value;

            if (last < begin.Value) {
                last = begin.Value;
            }
        } else {
            last = end?.Value ?? begin.Value.AddHours(1);
        }

        entry = new CalendarEntry(title, description, location, begin.Value, last, begin.DateOnly);
        return true;
    }

    /// <summary>
    /// Date iCalendar: 20261001 (giorno intero), 20261001T180000 (ora locale) o con la Z finale (UTC).
    /// </summary>
    private static (DateTime Value, bool DateOnly)? ParseCalendarDate(string value)
    {
        value = value.Trim();

        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)) {
            return (date, true);
        }

        bool utc = value.EndsWith('Z');
        string local = utc ? value[..^1] : value;

        if (DateTime.TryParseExact(local, ["yyyyMMdd'T'HHmmss", "yyyyMMdd'T'HHmm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime moment)) {
            return (utc ? DateTime.SpecifyKind(moment, DateTimeKind.Utc).ToLocalTime() : moment, false);
        }

        return null;
    }

    /// <summary>
    /// Nel formato semplificato vale prima la forma internazionale (2026-10-01 18:00), poi quella della lingua del sistema.
    /// </summary>
    private static bool TryParseLooseDate(string text, out DateTime value)
    {
        text = text.Trim();
        string[] iso = ["yyyy-MM-dd HH:mm", "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd"];

        return DateTime.TryParseExact(text, iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out value) ||
               DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static string FormatDateTime(DateTime value)
    {
        return value.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
    }

    #endregion

    #region Text helpers

    internal static bool StartsWithAny(string text, params ReadOnlySpan<string> prefixes)
    {
        ReadOnlySpan<char> trimmed = text.AsSpan().TrimStart();

        foreach (string prefix in prefixes) {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Nei valori di testo di vCard e iCalendar \ ; , e gli a capo vanno protetti con una barra rovesciata.
    /// </summary>
    internal static string EscapeText(string value)
    {
        return value.Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }

    /// <summary>
    /// Toglie le barre rovesciate di protezione: \X diventa X, e \n diventa un a capo se il formato lo prevede.
    /// </summary>
    internal static string Unescape(string value, bool newlines)
    {
        var result = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++) {
            if (value[i] == '\\' && i + 1 < value.Length) {
                char next = value[++i];
                result.Append(newlines && next is 'n' or 'N' ? '\n' : next);
            } else {
                result.Append(value[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Divide sui separatori non protetti da una barra rovesciata, lasciando le barre al loro posto.
    /// </summary>
    internal static List<string> SplitUnescaped(string text, char separator)
    {
        var fields = new List<string>();
        var current = new StringBuilder();

        for (var i = 0; i < text.Length; i++) {
            char character = text[i];

            if (character == '\\' && i + 1 < text.Length) {
                current.Append(character).Append(text[++i]);
            } else if (character == separator) {
                fields.Add(current.ToString());
                current.Clear();
            } else {
                current.Append(character);
            }
        }

        if (current.Length > 0) {
            fields.Add(current.ToString());
        }

        return fields;
    }

    /// <summary>
    /// I campi con etichetta dei formati MECARD e MATMSG: "ETICHETTA:valore;" ripetuto. Vale il primo di ogni etichetta.
    /// </summary>
    private static Dictionary<string, string> ReadLabeledFields(string body)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string field in SplitUnescaped(body, ';')) {
            int colon = field.IndexOf(':');

            if (colon > 0) {
                fields.TryAdd(field[..colon].Trim(), Unescape(field[(colon + 1)..], false));
            }
        }

        return fields;
    }

    /// <summary>
    /// Le proprietà di vCard e iCalendar: nome (senza parametri, in maiuscolo) e valore, con le righe spezzate ricongiunte.
    /// </summary>
    private static IEnumerable<(string Name, string Value)> ReadProperties(string text)
    {
        var unfolded = new List<string>();

        foreach (string raw in text.Replace("\r\n", "\n").Split('\n')) {
            if (raw.Length > 0 && raw[0] is ' ' or '\t' && unfolded.Count > 0) {
                unfolded[^1] += raw[1..];
            } else if (raw.Length > 0) {
                unfolded.Add(raw);
            }
        }

        foreach (string line in unfolded) {
            int colon = line.IndexOf(':');

            if (colon <= 0) {
                continue;
            }

            string head = line[..colon];
            int parameters = head.IndexOf(';');
            string name = (parameters < 0 ? head : head[..parameters]).Trim().ToUpperInvariant();

            yield return (name, line[(colon + 1)..].TrimEnd());
        }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            int equals = pair.IndexOf('=');

            if (equals > 0) {
                values.TryAdd(pair[..equals], Uri.UnescapeDataString(pair[(equals + 1)..]));
            }
        }

        return values;
    }

    private static (string Head, string Tail) SplitFirst(string text)
    {
        int separator = text.IndexOf(';');
        return separator < 0 ? (text.Trim(), string.Empty) : (text[..separator].Trim(), text[(separator + 1)..].Trim());
    }

    private static void AddIfPresent(List<string> lines, string name, string value)
    {
        if (value.Length > 0) {
            lines.Add($"{name}:{value}");
        }
    }

    #endregion
}
