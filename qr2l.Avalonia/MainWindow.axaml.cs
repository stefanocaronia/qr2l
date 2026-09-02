using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Material.Icons;
using qr2l.Core;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using AvaloniaColor = Avalonia.Media.Color;

namespace qr2l.Avalonia;

public partial class MainWindow : Window
{
    #region Constants and Fields

    private const int RefreshDelayMs = 100;
    private const string DonateUrl = "https://paypal.me/stefanocaronia";
    private const string RepoUrl = "https://github.com/stefanocaronia/qr2l";

    private readonly DispatcherTimer debounceTimer;
    private readonly ColorView fgColorView;
    private readonly ColorView bgColorView;
    private readonly Flyout fgColorFlyout;
    private readonly Flyout bgColorFlyout;
    private byte[]? pngData;
    private string? svgData;
    private byte[]? logo;
    private bool suppressLanguageEvent;

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

        PopulateLanguages();
        ApplyLanguage();
        ApplyThemeIcon();
        RefreshButtonStates();
    }

    #region Generation

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
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
        fgSwatch.Background = new SolidColorBrush(fgColorView.Color);
        bgSwatch.Background = new SolidColorBrush(bgColorView.Color);
        Generate();
    }

    private void Generate()
    {
        string text = qrText.Text?.Trim() ?? string.Empty;

        if (text.Length == 0) {
            ClearImageData();
            detectedMode.Text = PayloadMode.Text.ToString();
            RefreshButtonStates();
            return;
        }

        try {
            pngData = QrGenerator.Generate(text, ExportFormat.Png, CreateOptions());
            svgData = QrGenerator.GenerateSvgString(text, CreateOptions());

            (preview.Source as IDisposable)?.Dispose();
            using var stream = new MemoryStream(pngData);
            preview.Source = new AvaloniaBitmap(stream);
            previewHost.Background = new SolidColorBrush(bgColorView.Color);
        } catch {
            ClearImageData();
        }

        detectedMode.Text = QrGenerator.DetectPayloadMode(text).ToString();
        RefreshButtonStates();
    }

    private QrCodeOptions CreateOptions()
    {
        return new QrCodeOptions {
            darkColor = ToQrColor(fgColorView.Color),
            lightColor = ToQrColor(bgColorView.Color),
            logo = logo,
            pixelsPerModule = 20
        };
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
        svgData = null;
    }

    private void RefreshButtonStates()
    {
        bool hasData = pngData != null;

        saveButton.IsEnabled = hasData;
        copyImageButton.IsEnabled = hasData;
        copySvgButton.IsEnabled = svgData != null;
        previewPlaceholder.IsVisible = !hasData;
    }

    #endregion

    #region Actions

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        string text = qrText.Text?.Trim() ?? string.Empty;

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
            await ShowMessageAsync(Localization.T("msg_copied_image_title"), Localization.T("msg_copied_image"));
        } catch (Exception ex) {
            await ShowMessageAsync(Localization.T("err_title"), $"{Localization.T("err_copy_image")} {ex.Message}");
        }
    }

    private async void OnCopySvgClick(object? sender, RoutedEventArgs e)
    {
        if (svgData == null || Clipboard == null) {
            return;
        }

        try {
            await Clipboard.SetTextAsync(svgData);
            await ShowMessageAsync(Localization.T("msg_copied_svg_title"), Localization.T("msg_copied_svg"));
        } catch (Exception ex) {
            await ShowMessageAsync(Localization.T("err_title"), $"{Localization.T("err_copy_svg")} {ex.Message}");
        }
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

    private sealed record LanguageItem(string Code, string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    private void PopulateLanguages()
    {
        suppressLanguageEvent = true;

        List<LanguageItem> items = Localization.LanguageNames
            .Select(pair => new LanguageItem(pair.Key, pair.Value))
            .ToList();

        languageSelector.ItemsSource = items;
        languageSelector.SelectedItem = items.FirstOrDefault(item => item.Code == Localization.CurrentLanguage);

        suppressLanguageEvent = false;
    }

    private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (suppressLanguageEvent || languageSelector.SelectedItem is not LanguageItem item) {
            return;
        }

        Localization.SetLanguage(item.Code);
        ApplyLanguage();
    }

    /// <summary>
    /// Applica la lingua corrente a tutti i testi dell'interfaccia.
    /// </summary>
    private void ApplyLanguage()
    {
        ToolTip.SetTip(saveButton, Localization.T("tip_save"));
        ToolTip.SetTip(copyImageButton, Localization.T("tip_copy_image"));
        ToolTip.SetTip(copySvgButton, Localization.T("tip_copy_svg"));
        ToolTip.SetTip(donateButton, Localization.T("tip_donate"));
        ToolTip.SetTip(helpButton, Localization.T("tip_help"));
        ToolTip.SetTip(languageSelector, Localization.T("tip_language"));
        ToolTip.SetTip(themeButton, Localization.T("tip_theme"));
        ToolTip.SetTip(repoButton, Localization.T("tip_repo"));
        ToolTip.SetTip(fgColorButton, Localization.T("tip_fg"));
        ToolTip.SetTip(bgColorButton, Localization.T("tip_bg"));
        ToolTip.SetTip(qrText, Localization.T("tip_text"));

        qrText.PlaceholderText = Localization.T("placeholder");
        previewPlaceholder.Text = Localization.T("waiting");
        helpText.Text = BuildHelpText();

        ApplyLogoState();
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
        themeIcon.Foreground = App.IsDark ? Brushes.Goldenrod : new SolidColorBrush(AvaloniaColor.Parse("#6C7AE0"));
    }

    private static string BuildHelpText()
    {
        string[] lines = [
            $"*** {Project.Title} ***",
            "",
            $"- {Localization.T("help_p1")}",
            $"- {Localization.T("help_p2")}",
            $"- {Localization.T("help_p3")}",
            "",
            Localization.T("help_p4"),
            Localization.T("help_p5"),
            "",
            Localization.T("help_formats"),
            $"• {Localization.T("fmt_url")}: http://, https://, ftp://, www., domain.com",
            $"• {Localization.T("fmt_mail")}: user@domain.com;subject;body",
            $"• {Localization.T("fmt_phone")}: +1234567890",
            $"• {Localization.T("fmt_sms")}: 1234567890;message",
            $"• {Localization.T("fmt_whatsapp")}: +1234567890;message",
            $"• {Localization.T("fmt_wifi")}: WIFI:NetworkName;password",
            $"• {Localization.T("fmt_geo")}: 45.4642,9.1900",
            $"• {Localization.T("fmt_contact")}: FirstName;LastName;Phone;Email",
            $"• {Localization.T("fmt_event")}: Title;Description;Location;StartDate;EndDate",
            "",
            Localization.T("help_note")
        ];

        return string.Join(Environment.NewLine, lines);
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
