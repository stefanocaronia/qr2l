using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Material.Icons;
using Material.Icons.Avalonia;
using qr2l.Core;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using AvaloniaColor = Avalonia.Media.Color;

namespace qr2l.GUI;

public partial class MainWindow : Window
{
    #region Constants and Fields

    private const int RefreshDelayMs = 100;
    private const string DonateUrl = "https://paypal.me/stefanocaronia";
    private const string RepoUrl = "https://github.com/stefanocaronia/qr2l";
    private const string FormSetting = "form";

    private readonly DispatcherTimer debounceTimer;
    private readonly ColorView fgColorView;
    private readonly ColorView bgColorView;
    private readonly Flyout fgColorFlyout;
    private readonly Flyout bgColorFlyout;
    private byte[]? pngData;
    private byte[]? logo;

    // Tipi di contenuto nel menu della barra di stato, nell'ordine in cui compaiono
    private static readonly PayloadMode[] SelectableModes = [
        PayloadMode.Auto,
        PayloadMode.Text,
        PayloadMode.Url,
        PayloadMode.Mail,
        PayloadMode.Phone,
        PayloadMode.SMS,
        PayloadMode.WhatsApp,
        PayloadMode.WiFi,
        PayloadMode.Geolocation,
        PayloadMode.ContactData,
        PayloadMode.Event
    ];

    private readonly MenuFlyout modeMenu = new() { Placement = PlacementMode.TopEdgeAlignedLeft };
    private readonly MenuFlyout languageMenu = new() { Placement = PlacementMode.TopEdgeAlignedRight };
    private PayloadMode selectedMode = PayloadMode.Auto;
    private PayloadMode detectedMode = PayloadMode.Text;
    private bool invalidContent;

    // Modulo mostrato (null se nessuno) e stato della sincronizzazione con la casella di testo
    private PayloadMode? visibleForm;
    private bool showForm = UserSettings.Get(FormSetting) != "off";
    private bool syncing;
    private bool textEditedByUser;
    private TimeSpan eventDuration = TimeSpan.FromHours(1);

    #endregion

    public MainWindow()
    {
        InitializeComponent();

        Title = Project.Title;

        debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(RefreshDelayMs) };
        debounceTimer.Tick += OnDebounceTick;

        fgColorView = CreateColorView(Colors.Black);
        bgColorView = CreateColorView(Colors.White);
        fgColorFlyout = CreateColorFlyout(fgColorView);
        bgColorFlyout = CreateColorFlyout(bgColorView);
        modeButton.Flyout = modeMenu;
        languageButton.Flyout = languageMenu;
        BuildLanguageMenu();
        formButton.IsChecked = showForm;
        PaintSwatch(fgSwatch, fgColorView.Color);
        PaintSwatch(bgSwatch, bgColorView.Color);

        WireForms();
        Resized += (_, _) => KeepOnScreen();

        ApplyLanguage();
        ApplyThemeIcon();
        RefreshButtonStates();
    }

    #region Generation

    private void OnTextChanging(object? sender, TextChangingEventArgs e)
    {
        // TextChanging arriva subito (TextChanged invece è accodato): solo qui si sa se ha scritto l'utente o un modulo
        textEditedByUser |= !syncing;
        debounceTimer.Stop();
        debounceTimer.Start();
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        debounceTimer.Stop();
        Generate();
    }

    private ColorView CreateColorView(AvaloniaColor initial)
    {
        var view = new ColorView {
            Color = initial,
            IsAlphaEnabled = false,
            IsAlphaVisible = false
        };

        view.ColorChanged += OnColorChanged;
        return view;
    }

    /// <summary>
    /// Il selettore si apre a fianco della finestra, non sotto il pulsante:
    /// cosi' l'anteprima resta visibile mentre il colore cambia in tempo reale.
    /// </summary>
    private static Flyout CreateColorFlyout(ColorView view)
    {
        return new Flyout {
            Content = view,
            Placement = PlacementMode.RightEdgeAlignedTop
        };
    }

    private void OnFgColorClick(object? sender, RoutedEventArgs e)
    {
        fgColorFlyout.ShowAt(root);
    }

    private void OnBgColorClick(object? sender, RoutedEventArgs e)
    {
        bgColorFlyout.ShowAt(root);
    }

    private void OnColorChanged(object? sender, ColorChangedEventArgs e)
    {
        PaintSwatch(fgSwatch, fgColorView.Color);
        PaintSwatch(bgSwatch, bgColorView.Color);
        Generate();
    }

    /// <summary>
    /// Il riquadro prende il colore scelto; icona e scritta il bianco o il nero, quello che si legge meglio.
    /// </summary>
    private static void PaintSwatch(Border swatch, AvaloniaColor color)
    {
        swatch.Background = new SolidColorBrush(color);
        TextElement.SetForeground(swatch, color.R * 299 + color.G * 587 + color.B * 114 > 128_000 ? Brushes.Black : Brushes.White);
    }

    /// <summary>
    /// Il testo da codificare: la casella è l'unica fonte, i moduli ci scrivono dentro.
    /// </summary>
    private string CurrentText => qrText.Text?.Trim() ?? string.Empty;

    private void Generate()
    {
        string text = CurrentText;

        detectedMode = text.Length == 0 ? PayloadMode.Text : QrGenerator.DetectPayloadMode(text);
        invalidContent = false;

        if (text.Length == 0) {
            ClearImageData();
        } else {
            try {
                pngData = QrGenerator.Generate(text, ExportFormat.Png, CreateOptions());

                (preview.Source as IDisposable)?.Dispose();
                using var stream = new MemoryStream(pngData);
                preview.Source = new AvaloniaBitmap(stream);
                previewHost.Background = new SolidColorBrush(bgColorView.Color);
            } catch {
                // Succede quando il testo non ha la forma richiesta dal tipo scelto, per esempio un evento senza date
                ClearImageData();
                invalidContent = true;
            }
        }

        UpdateModeLabel();
        UpdatePlaceholder();
        RefreshButtonStates();
        RefreshForm();
    }

    private QrCodeOptions CreateOptions()
    {
        return new QrCodeOptions {
            darkColor = ToQrColor(fgColorView.Color),
            lightColor = ToQrColor(bgColorView.Color),
            logo = logo,
            pixelsPerModule = 20,
            payloadMode = selectedMode
        };
    }

    private void UpdatePlaceholder()
    {
        previewPlaceholder.Text = Localization.T(invalidContent ? "invalid_content" : "waiting");
    }

    private static QrColor ToQrColor(AvaloniaColor color)
    {
        return new QrColor(color.R, color.G, color.B);
    }

    private void ClearImageData()
    {
        (preview.Source as IDisposable)?.Dispose();
        preview.Source = null;
        previewHost.Background = Brushes.Transparent;
        pngData = null;
    }

    private void RefreshButtonStates()
    {
        bool hasData = pngData != null;

        saveButton.IsEnabled = hasData;
        copyImageButton.IsEnabled = hasData;
        copySvgButton.IsEnabled = hasData;
        previewPlaceholder.IsVisible = !hasData;
    }

    /// <summary>
    /// Quando il modulo allunga la finestra, la si alza quanto basta per non farla uscire dallo schermo.
    /// </summary>
    private void KeepOnScreen()
    {
        if (Screens.ScreenFromWindow(this) is not { } screen || FrameSize is not { } frame) {
            return;
        }

        int overflow = Position.Y + (int)Math.Ceiling(frame.Height * DesktopScaling) - screen.WorkingArea.Bottom;

        if (overflow > 0) {
            Position = new PixelPoint(Position.X, Math.Max(screen.WorkingArea.Y, Position.Y - overflow));
        }
    }

    #endregion

    #region Content type

    /// <summary>
    /// In automatico l'etichetta mostra il tipo rilevato, attenuato; dopo una scelta manuale, il tipo scelto a piena intensità.
    /// </summary>
    private void UpdateModeLabel()
    {
        bool automatic = selectedMode == PayloadMode.Auto;
        PayloadMode shown = automatic ? detectedMode : selectedMode;

        modeLabel.Text = ModeName(shown);
        SetModeIcon(modeIcon, shown);
        modeShown.Opacity = automatic ? 0.55 : 1.0;
    }

    /// <summary>
    /// Le voci si creano solo quando cambia la lingua: rifarle durante un clic impedirebbe al menu di chiudersi.
    /// </summary>
    private void BuildModeMenu()
    {
        modeMenu.Items.Clear();

        foreach (PayloadMode mode in SelectableModes) {
            var icon = new MaterialIcon { Width = 16, Height = 16 };
            SetModeIcon(icon, mode);
            AddChoice(modeMenu, ModeName(mode), mode, () => SelectMode(mode), icon);

            // L'automatico resta separato dai tipi specifici
            if (mode == PayloadMode.Auto) {
                modeMenu.Items.Add(new Separator());
            }
        }

        CheckChoice(modeMenu, selectedMode);
    }

    /// <summary>
    /// Voce di un menu a scelta singola. L'icona va nell'intestazione: la colonna delle icone ha già il segno di scelta.
    /// </summary>
    private static void AddChoice(MenuFlyout menu, string text, object value, Action choose, Control? icon = null)
    {
        var item = new MenuItem {
            Header = icon == null ? text : new StackPanel {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { icon, new TextBlock { Text = text } }
            },
            Tag = value,
            ToggleType = MenuItemToggleType.Radio
        };

        // Con icona e testo nell'intestazione, il nome per i lettori di schermo va dato a parte
        AutomationProperties.SetName(item, text);
        item.Click += (_, _) => choose();
        menu.Items.Add(item);
    }

    private static void CheckChoice(MenuFlyout menu, object value)
    {
        foreach (MenuItem item in menu.Items.OfType<MenuItem>()) {
            item.IsChecked = Equals(item.Tag, value);
        }
    }

    /// <summary>
    /// Ogni tipo di contenuto ha la sua icona colorata, nel menu e nella barra di stato.
    /// Qui le icone restano nel loro quadrato Material, così hanno tutte le stesse proporzioni.
    /// </summary>
    private static void SetModeIcon(MaterialIcon icon, PayloadMode mode)
    {
        (MaterialIconKind kind, string color) = mode switch {
            PayloadMode.Auto => (MaterialIconKind.AutoFix, "#8E5BD0"),
            PayloadMode.Url => (MaterialIconKind.Link, "#2F6FD6"),
            PayloadMode.Mail => (MaterialIconKind.Email, "#E0453A"),
            PayloadMode.Phone => (MaterialIconKind.Phone, "#2E9E5B"),
            PayloadMode.SMS => (MaterialIconKind.MessageText, "#1E9EA8"),
            PayloadMode.WhatsApp => (MaterialIconKind.Whatsapp, "#25A55F"),
            PayloadMode.WiFi => (MaterialIconKind.Wifi, "#6C7AE0"),
            PayloadMode.Geolocation => (MaterialIconKind.MapMarker, "#E07B2E"),
            PayloadMode.ContactData => (MaterialIconKind.CardAccountDetails, "#B0851E"),
            PayloadMode.Event => (MaterialIconKind.CalendarMonth, "#D14B8F"),
            var _ => (MaterialIconKind.Text, "#7A7F87")
        };

        icon.Kind = kind;
        icon.Foreground = SolidColorBrush.Parse(color);
    }

    /// <summary>
    /// Cambia tipo di contenuto. Chi sceglie un tipo con più campi vuole compilarli: il modulo si apre.
    /// </summary>
    private void SelectMode(PayloadMode mode)
    {
        selectedMode = mode;

        if (HasForm(mode) && !showForm) {
            SetShowForm(true);
        }

        CheckChoice(modeMenu, mode);
        UpdateTextPlaceholder();
        Generate();
    }

    /// <summary>
    /// Il segnaposto della casella mostra il formato atteso dal tipo scelto: la sintassi compare solo quando serve.
    /// </summary>
    private void UpdateTextPlaceholder()
    {
        qrText.PlaceholderText = selectedMode switch {
            PayloadMode.Url => "https://example.com",
            PayloadMode.Mail => Localization.T("ph_mail"),
            PayloadMode.Phone => Localization.T("ph_phone"),
            PayloadMode.SMS => Localization.T("ph_sms"),
            PayloadMode.WhatsApp => Localization.T("ph_whatsapp"),
            PayloadMode.Geolocation => Localization.T("ph_geo"),
            PayloadMode.ContactData => Localization.T("ph_contact"),
            PayloadMode.Event => Localization.T("ph_event"),
            var _ => Localization.T("placeholder")
        };
    }

    private static string ModeName(PayloadMode mode)
    {
        return mode switch {
            PayloadMode.Auto => Localization.T("mode_auto"),
            PayloadMode.Text => Localization.T("mode_text"),
            PayloadMode.Url => Localization.T("fmt_url"),
            PayloadMode.Mail => Localization.T("fmt_mail"),
            PayloadMode.Phone => Localization.T("fmt_phone"),
            PayloadMode.SMS => Localization.T("fmt_sms"),
            PayloadMode.WhatsApp => Localization.T("fmt_whatsapp"),
            PayloadMode.WiFi => Localization.T("fmt_wifi"),
            PayloadMode.Geolocation => Localization.T("fmt_geo"),
            PayloadMode.ContactData => Localization.T("fmt_contact"),
            PayloadMode.Event => Localization.T("fmt_event"),
            var _ => mode.ToString()
        };
    }

    #endregion

    #region Forms

    private static bool HasForm(PayloadMode mode)
    {
        return mode is PayloadMode.WiFi or PayloadMode.Mail or PayloadMode.SMS or PayloadMode.WhatsApp
            or PayloadMode.ContactData or PayloadMode.Event;
    }

    /// <summary>
    /// Ogni modifica a un campo riscrive subito il testo; cambiando l'inizio di un evento, la fine lo segue.
    /// </summary>
    private void WireForms()
    {
        foreach (TextBox field in formHost.GetLogicalDescendants().OfType<TextBox>()) {
            field.TextChanging += (_, _) => OnFormEdited();
        }

        wifiSecurity.SelectionChanged += (_, _) => OnFormEdited();
        wifiHidden.IsCheckedChanged += (_, _) => OnFormEdited();
        eventAllDay.IsCheckedChanged += (_, _) => OnAllDayChanged();
        eventStartTime.SelectedTimeChanged += (_, _) => OnEventStartEdited();
        eventEndTime.SelectedTimeChanged += (_, _) => OnEventEndEdited();
        WatchDate(eventStartDate, OnEventStartEdited);
        WatchDate(eventEndDate, OnEventEndEdited);
    }

    /// <summary>
    /// La data si segue dalla proprietà, che cambia subito: SelectedDateChanged a volte arriva in ritardo.
    /// </summary>
    private static void WatchDate(CalendarDatePicker picker, Action changed)
    {
        picker.PropertyChanged += (_, e) => {
            if (e.Property == CalendarDatePicker.SelectedDateProperty) {
                changed();
            }
        };
    }

    private void OnFormButtonClick(object? sender, RoutedEventArgs e)
    {
        SetShowForm(formButton.IsChecked == true);
        RefreshForm();
    }

    private void SetShowForm(bool value)
    {
        showForm = value;
        formButton.IsChecked = value;
        UserSettings.Set(FormSetting, value ? "on" : "off");
    }

    /// <summary>
    /// Mostra il modulo del tipo corrente, scelto o rilevato, se ne ha uno e non è nascosto.
    /// Il modulo si riempie dal testo quando compare e ogni volta che il testo viene modificato a mano.
    /// </summary>
    private void RefreshForm()
    {
        PayloadMode type = selectedMode == PayloadMode.Auto ? detectedMode : selectedMode;
        bool available = HasForm(type);
        PayloadMode? form = available && showForm ? type : null;

        formButton.IsEnabled = available;
        ToolTip.SetTip(formButton, Localization.T(available ? "tip_form" : "tip_form_none"));

        if (form != visibleForm) {
            visibleForm = form;
            ShowForm();
            FillForm(true);
        } else if (textEditedByUser) {
            FillForm(false);
        }

        textEditedByUser = false;
    }

    private void ShowForm()
    {
        formHost.IsVisible = visibleForm != null;
        wifiForm.IsVisible = visibleForm == PayloadMode.WiFi;
        mailForm.IsVisible = visibleForm == PayloadMode.Mail;
        messageForm.IsVisible = visibleForm is PayloadMode.SMS or PayloadMode.WhatsApp;
        contactForm.IsVisible = visibleForm == PayloadMode.ContactData;
        eventForm.IsVisible = visibleForm == PayloadMode.Event;

        // WhatsApp vuole il numero con il prefisso internazionale
        messageNumber.Tag = visibleForm == PayloadMode.WhatsApp ? "field_intl_phone" : "field_phone";
        ApplyText(messageNumber);
    }

    /// <summary>
    /// Riporta il testo nei campi. Se il testo non si legge i campi restano com'erano,
    /// tranne quando il modulo è appena comparso o il testo è vuoto: allora ripartono da zero.
    /// </summary>
    private void FillForm(bool reset)
    {
        string text = CurrentText;
        reset |= text.Length == 0;
        syncing = true;

        switch (visibleForm) {
            case PayloadMode.WiFi when Payloads.TryParseWiFi(text, out WiFiNetwork network) || reset:
                SetWiFi(network);
                break;

            case PayloadMode.Mail when Payloads.TryParseMail(text, out MailMessage mail) || reset:
                SetMail(mail);
                break;

            case PayloadMode.SMS when Payloads.TryParseSms(text, out TextMessage sms) || reset:
                SetMessage(sms);
                break;

            case PayloadMode.WhatsApp when Payloads.TryParseWhatsApp(text, out TextMessage message) || reset:
                SetMessage(message);
                break;

            case PayloadMode.ContactData when Payloads.TryParseContact(text, out ContactCard card) || reset:
                SetContact(card);
                break;

            case PayloadMode.Event when Payloads.TryParseEvent(text, out CalendarEntry entry):
                SetEvent(entry);
                break;

            case PayloadMode.Event when reset:
                SetEvent(NewEvent());
                break;
        }

        syncing = false;
    }

    /// <summary>
    /// Un campo modificato dall'utente riscrive il testo nel formato completo del tipo.
    /// </summary>
    private void OnFormEdited()
    {
        if (syncing || visibleForm is not { } form) {
            return;
        }

        syncing = true;

        qrText.Text = form switch {
            PayloadMode.WiFi => Payloads.BuildWiFi(new WiFiNetwork(
                Field(wifiSsid),
                Field(wifiPassword),
                wifiSecurity.SelectedIndex == 1 ? WiFiAuthenticationType.WEP : WiFiAuthenticationType.WPA,
                wifiHidden.IsChecked == true)),
            PayloadMode.Mail => Payloads.BuildMail(new MailMessage(Field(mailAddress), Field(mailSubject), Field(mailBody))),
            PayloadMode.SMS => Payloads.BuildSms(new TextMessage(Field(messageNumber), Field(messageText))),
            PayloadMode.WhatsApp => Payloads.BuildWhatsApp(new TextMessage(Field(messageNumber), Field(messageText))),
            PayloadMode.ContactData => Payloads.BuildContact(new ContactCard(
                Field(contactFirstName),
                Field(contactLastName),
                Field(contactPhone),
                Field(contactEmail),
                Field(contactOrganization),
                Field(contactWebsite))),
            var _ => Payloads.BuildEvent(new CalendarEntry(
                Field(eventTitle),
                Field(eventDescription),
                Field(eventLocation),
                EventStart,
                EventEnd,
                eventAllDay.IsChecked == true))
        };

        syncing = false;
    }

    private static string Field(TextBox field)
    {
        return field.Text?.Trim() ?? string.Empty;
    }

    private void SetWiFi(WiFiNetwork network)
    {
        wifiSsid.Text = network.Ssid;
        wifiPassword.Text = network.Password;
        wifiSecurity.SelectedIndex = network.Authentication == WiFiAuthenticationType.WEP ? 1 : 0;
        wifiHidden.IsChecked = network.Hidden;
    }

    private void SetMail(MailMessage mail)
    {
        mailAddress.Text = mail.Address;
        mailSubject.Text = mail.Subject;
        mailBody.Text = mail.Body;
    }

    private void SetMessage(TextMessage message)
    {
        messageNumber.Text = message.Number;
        messageText.Text = message.Message;
    }

    private void SetContact(ContactCard card)
    {
        contactFirstName.Text = card.FirstName;
        contactLastName.Text = card.LastName;
        contactPhone.Text = card.Phone;
        contactEmail.Text = card.Email;
        contactOrganization.Text = card.Organization;
        contactWebsite.Text = card.Website;
    }

    private DateTime EventStart => Moment(eventStartDate, eventStartTime);

    private DateTime EventEnd => Moment(eventEndDate, eventEndTime);

    private static DateTime Moment(CalendarDatePicker date, TimePicker time)
    {
        return (date.SelectedDate ?? DateTime.Today).Date + (time.SelectedTime ?? TimeSpan.Zero);
    }

    private static TimeSpan Duration(DateTime start, DateTime end)
    {
        return end > start ? end - start : TimeSpan.Zero;
    }

    /// <summary>
    /// Un evento nuovo parte dalla prossima ora piena e dura un'ora.
    /// </summary>
    private static CalendarEntry NewEvent()
    {
        DateTime start = DateTime.Today.AddHours(DateTime.Now.Hour + 1);
        return new CalendarEntry(string.Empty, string.Empty, string.Empty, start, start.AddHours(1), false);
    }

    private void SetEvent(CalendarEntry entry)
    {
        eventTitle.Text = entry.Title;
        eventLocation.Text = entry.Location;
        eventDescription.Text = entry.Description;
        eventAllDay.IsChecked = entry.AllDay;
        eventStartDate.SelectedDate = entry.Start.Date;
        eventEndDate.SelectedDate = entry.End.Date;

        // Per un evento di tutto il giorno gli orari restano quelli di prima, pronti se si toglie la spunta
        if (!entry.AllDay) {
            eventStartTime.SelectedTime = entry.Start.TimeOfDay;
            eventEndTime.SelectedTime = entry.End.TimeOfDay;
        }

        eventDuration = Duration(entry.Start, entry.End);
    }

    /// <summary>
    /// Spostando l'inizio, la fine lo segue mantenendo la durata, come nei calendari.
    /// </summary>
    private void OnEventStartEdited()
    {
        if (syncing) {
            return;
        }

        syncing = true;
        DateTime end = EventStart + eventDuration;
        eventEndDate.SelectedDate = end.Date;
        eventEndTime.SelectedTime = end.TimeOfDay;
        syncing = false;

        OnFormEdited();
    }

    private void OnEventEndEdited()
    {
        if (syncing) {
            return;
        }

        eventDuration = Duration(EventStart, EventEnd);
        OnFormEdited();
    }

    private void OnAllDayChanged()
    {
        bool timed = eventAllDay.IsChecked != true;

        eventStartTime.IsVisible = timed;
        eventEndTime.IsVisible = timed;
        OnFormEdited();
    }

    #endregion

    #region Actions

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        string text = CurrentText;

        if (text.Length == 0) {
            return;
        }

        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = Localization.T("dlg_save_as"),
            SuggestedFileName = "qrcode",
            DefaultExtension = "png",
            FileTypeChoices = BuildFileTypes()
        });

        if (file == null) {
            return;
        }

        string extension = Path.GetExtension(file.Name).TrimStart('.').ToLowerInvariant();
        ExportFormat format = Enum.GetValues<ExportFormat>()
            .FirstOrDefault(f => f.GetExtension() == extension);

        try {
            byte[] data = QrGenerator.Generate(text, format, CreateOptions());
            await using Stream stream = await file.OpenWriteAsync();
            await stream.WriteAsync(data);
            FlashDone(saveIcon, MaterialIconKind.ContentSave);
        } catch (Exception ex) {
            await ShowMessageAsync(Localization.T("err_title"), $"{Localization.T("err_export")} {format}: {ex.Message}");
        }
    }

    private static List<FilePickerFileType> BuildFileTypes()
    {
        return Enum.GetValues<ExportFormat>()
            .Select(format => new FilePickerFileType(format.ToString()) {
                Patterns = [$"*.{format.GetExtension()}"]
            })
            .ToList();
    }

    private async void OnCopyImageClick(object? sender, RoutedEventArgs e)
    {
        if (pngData == null || Clipboard == null) {
            return;
        }

        try {
            using var stream = new MemoryStream(pngData);
            using var bitmap = new AvaloniaBitmap(stream);
            await Clipboard.SetBitmapAsync(bitmap);
            FlashDone(copyImageIcon, MaterialIconKind.ContentCopy);
        } catch (Exception ex) {
            await ShowMessageAsync(Localization.T("err_title"), $"{Localization.T("err_copy_image")} {ex.Message}");
        }
    }

    /// <summary>
    /// L'SVG si genera solo quando serve, invece che a ogni tasto premuto.
    /// </summary>
    private async void OnCopySvgClick(object? sender, RoutedEventArgs e)
    {
        if (pngData == null || Clipboard == null) {
            return;
        }

        try {
            await Clipboard.SetTextAsync(QrGenerator.GenerateSvgString(CurrentText, CreateOptions()));
            FlashDone(copySvgIcon, MaterialIconKind.Xml);
        } catch (Exception ex) {
            await ShowMessageAsync(Localization.T("err_title"), $"{Localization.T("err_copy_svg")} {ex.Message}");
        }
    }

    /// <summary>
    /// Conferma senza finestre da chiudere: l'icona del pulsante diventa per un attimo una spunta.
    /// </summary>
    private static void FlashDone(Glyph icon, MaterialIconKind kind)
    {
        icon.Kind = MaterialIconKind.Check;
        DispatcherTimer.RunOnce(() => icon.Kind = kind, TimeSpan.FromSeconds(1.2));
    }

    private async void OnLogoClick(object? sender, RoutedEventArgs e)
    {
        if (logo != null) {
            logo = null;
        } else {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = Localization.T("logo_set"),
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });

            if (files.Count == 0) {
                return;
            }

            try {
                await using Stream stream = await files[0].OpenReadAsync();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                byte[] bytes = buffer.ToArray();

                // Verifica subito che il file sia un'immagine decodificabile, prima di adottarlo come logo
                QrGenerator.Generate("qr2l", ExportFormat.Png, new QrCodeOptions { logo = bytes });
                logo = bytes;
            } catch (Exception ex) {
                await ShowMessageAsync(Localization.T("err_title"), ex.Message);
                return;
            }
        }

        ApplyLogoState();
        Generate();
    }

    private void OnThemeClick(object? sender, RoutedEventArgs e)
    {
        App.ToggleTheme();
        ApplyThemeIcon();
    }

    private void OnDonateClick(object? sender, RoutedEventArgs e)
    {
        OpenUrl(DonateUrl);
    }

    private void OnRepoClick(object? sender, RoutedEventArgs e)
    {
        OpenUrl(RepoUrl);
    }

    private async void OpenUrl(string url)
    {
        try {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        } catch (Exception ex) {
            await ShowMessageAsync(Localization.T("err_title"), $"{Localization.T("err_open_url")}\n\n{ex.Message}");
        }
    }

    #endregion

    #region Localization and theme

    private void BuildLanguageMenu()
    {
        foreach ((string code, string name) in Localization.LanguageNames) {
            AddChoice(languageMenu, name, code, () => {
                Localization.SetLanguage(code);
                ApplyLanguage();
            });
        }
    }

    /// <summary>
    /// Applica la lingua corrente a tutti i testi dell'interfaccia.
    /// </summary>
    private void ApplyLanguage()
    {
        foreach (Control control in root.GetLogicalDescendants().OfType<Control>()) {
            ApplyText(control);
        }

        ToolTip.SetTip(qrText, Localization.T("tip_text"));
        languageLabel.Text = Localization.CurrentLanguage.ToUpperInvariant();

        CheckChoice(languageMenu, Localization.CurrentLanguage);
        BuildModeMenu();
        UpdateTextPlaceholder();
        ApplyHelp();
        UpdateModeLabel();
        UpdatePlaceholder();
        ApplyLogoState();
        RefreshForm();
    }

    /// <summary>
    /// Il Tag di un controllo è la chiave del suo testo: segnaposto per i campi, testo per etichette e caselle,
    /// suggerimento per il resto. I pulsanti con una scorciatoia la mostrano nel suggerimento.
    /// </summary>
    private static void ApplyText(Control control)
    {
        if (control.Tag is not string key) {
            return;
        }

        string text = Localization.T(key);

        switch (control) {
            case TextBox field:
                field.PlaceholderText = text;
                ToolTip.SetTip(field, text);
                break;

            case CheckBox check:
                check.Content = text;
                break;

            case TextBlock label:
                label.Text = text;
                break;

            default:
                ToolTip.SetTip(control, control is Button { HotKey: { } hotKey } ? $"{text} ({hotKey})" : text);
                break;
        }
    }

    private void ApplyLogoState()
    {
        bool hasLogo = logo != null;

        logoLabel.Text = Localization.T(hasLogo ? "logo_remove" : "logo_set");
        logoIcon.Kind = hasLogo ? MaterialIconKind.ImageRemove : MaterialIconKind.Image;
        ToolTip.SetTip(logoButton, logoLabel.Text);
    }

    private void ApplyThemeIcon()
    {
        // In tema scuro si mostra il sole (per passare al chiaro), e viceversa
        themeIcon.Kind = App.IsDark ? MaterialIconKind.WhiteBalanceSunny : MaterialIconKind.WeatherNight;
        themeIcon.Fill = App.IsDark ? Brushes.Goldenrod : SolidColorBrush.Parse("#6C7AE0");
    }

    #endregion

    #region Help

    /// <summary>
    /// Help breve: due righe su come funziona, esempi cliccabili che riempiono il contenuto,
    /// il link alla guida completa e la versione.
    /// </summary>
    private void ApplyHelp()
    {
        helpIntro.Text = Localization.T("help_intro");
        helpExamplesTitle.Text = Localization.T("help_examples");
        helpGuide.Content = Localization.T("help_guide");
        helpVersion.Text = $"qr2l {Project.Version}";

        helpExamples.Children.Clear();
        AddHelpExample(PayloadMode.Url, PayloadMode.Auto, "https://github.com/stefanocaronia/qr2l");
        AddHelpExample(PayloadMode.Mail, PayloadMode.Auto, "info@example.com");
        AddHelpExample(PayloadMode.Phone, PayloadMode.Auto, "+39 02 1234567");
        AddHelpExample(PayloadMode.Geolocation, PayloadMode.Auto, "45.4642,9.1900");
        AddHelpExample(PayloadMode.WiFi, PayloadMode.WiFi, "WIFI:T:WPA;S:MyWiFi;P:password123;;");
    }

    private void AddHelpExample(PayloadMode shown, PayloadMode mode, string text)
    {
        var link = new HyperlinkButton {
            Content = ModeName(shown),
            Padding = new Thickness(0, 0, 12, 0)
        };

        link.Click += (_, _) => {
            helpButton.Flyout?.Hide();
            qrText.Text = text;
            SelectMode(mode);
        };

        helpExamples.Children.Add(link);
    }

    private void OnGuideClick(object? sender, RoutedEventArgs e)
    {
        helpButton.Flyout?.Hide();
        OpenUrl($"{RepoUrl}#readme");
    }

    #endregion

    #region Dialogs

    /// <summary>
    /// Avalonia non ha un MessageBox: una piccola finestra modale con testo e pulsante di chiusura.
    /// </summary>
    private async Task ShowMessageAsync(string title, string message)
    {
        var okButton = new Button {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 80
        };

        var dialog = new Window {
            Title = title,
            Width = 400,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel {
                Margin = new Thickness(16),
                Spacing = 12,
                Children = {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    okButton
                }
            }
        };

        okButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(this);
    }

    #endregion
}
