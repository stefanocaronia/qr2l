using System.Diagnostics;
using qr2l.Core;
using Timer = System.Windows.Forms.Timer;

namespace qr2l.GUI;

public sealed partial class Form1 : Form
{
    #region Constants and Fields

    private const float RefreshDelay = 0.1f;
    private const string DonateUrl = "https://paypal.me/stefanocaronia";
    private const string LicenseUrl = "https://creativecommons.org/licenses/by-sa/4.0/";
    private const string RepoUrl = "https://github.com/stefanocaronia/qr2l";

    private readonly Timer debounceTimer;
    private byte[]? pngData;
    private string? svgData;

    private bool suppressLanguageEvent;

    private Color fgColor = Color.Black;
    private Color bgColor = Color.White;
    private Bitmap? logoBitmap;

    #endregion

    public Form1()
    {
        InitializeComponent();

        Text = Project.Title;

        UseWaitCursor = false;
        mainGrid.UseWaitCursor = false;
        textQrCode.UseWaitCursor = false;
        pictureQrCode.UseWaitCursor = false;

        debounceTimer = new Timer();
        debounceTimer.Interval = (int)(1000f * RefreshDelay);
        debounceTimer.Tick += OnDebounceTimerTick;

        panelFg.BackColor = fgColor;
        panelBg.BackColor = bgColor;

        panelFg.Cursor = Cursors.Hand;
        panelBg.Cursor = Cursors.Hand;
        logoPath.Cursor = Cursors.Hand;

        saveAsDialog.Filter = BuildFilterFromEnum();

        Localization.Initialize();
        PopulateLanguages();
        ApplyLanguage();

        RefreshButtonStates();
    }


    #region Localization

    private void PopulateLanguages()
    {
        suppressLanguageEvent = true;

        languageSelector.Items.Clear();

        foreach (KeyValuePair<string, string> language in Localization.LanguageNames) {
            var item = new LanguageItem(language.Key, language.Value);
            languageSelector.Items.Add(item);

            if (language.Key == Localization.CurrentLanguage) {
                languageSelector.SelectedItem = item;
            }
        }

        suppressLanguageEvent = false;
    }

    private void languageSelector_SelectedIndexChanged(object sender, EventArgs e)
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
        tooltip.SetToolTip(saveButton, Localization.T("tip_save"));
        tooltip.SetToolTip(copyAsImageButton, Localization.T("tip_copy_image"));
        tooltip.SetToolTip(copyAsSvgButton, Localization.T("tip_copy_svg"));
        tooltip.SetToolTip(donateButton, Localization.T("tip_donate"));
        tooltip.SetToolTip(helpButton, Localization.T("tip_help"));
        tooltip.SetToolTip(languageSelector, Localization.T("tip_language"));
        tooltip.SetToolTip(buttonRepo, Localization.T("tip_repo"));
        tooltip.SetToolTip(panelFg, Localization.T("tip_fg"));
        tooltip.SetToolTip(panelBg, Localization.T("tip_bg"));
        tooltip.SetToolTip(textQrCode, Localization.T("tip_text"));
        tooltip.SetToolTip(pictureQrCode, Localization.T("tip_picture"));
        tooltip.SetToolTip(logoPath, Localization.T(logoBitmap != null ? "logo_remove" : "logo_set"));

        textQrCode.PlaceholderText = Localization.T("tip_text");
        logoPath.Text = Localization.T(logoBitmap != null ? "logo_remove" : "logo_set");

        saveAsToolStripMenuItem.Text = Localization.T("menu_save_as");
        copyAsImageToolStripMenuItem.Text = Localization.T("menu_copy_image");
        copyAsSVGToolStripMenuItem.Text = Localization.T("menu_copy_svg");

        saveAsDialog.Title = Localization.T("dlg_save_as");
    }

    private sealed record LanguageItem(string Code, string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    #endregion

    private void RefreshButtonStates()
    {
        bool hasData = pngData != null || svgData != null;

        saveButton.Enabled = hasData;
        copyAsImageButton.Enabled = hasData;
        copyAsSvgButton.Enabled = hasData;
        imageMenu.Enabled = hasData;
    }

    private void OnTextQrCodeChanged(object sender, EventArgs e)
    {
        debounceTimer.Stop();
        debounceTimer.Start();
    }

    private void OnDebounceTimerTick(object? sender, EventArgs e)
    {
        debounceTimer.Stop();
        GenerateQrPreview();
    }

    private void GenerateQrPreview()
    {
        string text = textQrCode.Text.Trim();

        if (string.IsNullOrEmpty(text)) {
            ClearImageData();
            RefreshButtonStates();
            return;
        }

        try {
            QrCodeOptions options = CreateQrCodeOptions();
            GeneratePngPreview(text, options);
            GenerateSvgData(text, options);
        } catch {
            ClearImageData();
        }

        detectedMode.Text = QrGenerator.DetectPayloadMode(text).ToString();
        RefreshButtonStates();
    }

    private QrCodeOptions CreateQrCodeOptions()
    {
        return new QrCodeOptions {
            payloadMode = PayloadMode.Auto,
            darkColor = fgColor,
            lightColor = bgColor,
            logo = logoBitmap,
            shape = PixelShape.Square,
            pixelsPerModule = 20
        };
    }

    private void GeneratePngPreview(string text, QrCodeOptions options)
    {
        pngData = QrGenerator.Generate(text, ExportFormat.Png, options);
        using var memoryStream = new MemoryStream(pngData);
        pictureQrCode.Image = Image.FromStream(memoryStream);
    }

    private void GenerateSvgData(string text, QrCodeOptions options)
    {
        svgData = QrGenerator.GenerateSvgString(text, options);
    }

    private void ClearImageData()
    {
        pictureQrCode.Image = null;
        pngData = null;
        svgData = null;
    }

    private void CopyImageToClipboard()
    {
        if (pngData == null) {
            return;
        }

        try {
            using var ms = new MemoryStream(pngData);
            using Image image = Image.FromStream(ms);
            Clipboard.SetImage(image);
            MessageBox.Show(Localization.T("msg_copied_image"), Localization.T("msg_copied_image_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
            MessageBox.Show($"{Localization.T("err_copy_image")} {ex.Message}", Localization.T("err_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopySvgToClipboard()
    {
        if (svgData == null) {
            return;
        }

        try {
            Clipboard.SetText(svgData);
            MessageBox.Show(Localization.T("msg_copied_svg"), Localization.T("msg_copied_svg_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
            MessageBox.Show($"{Localization.T("err_copy_svg")} {ex.Message}", Localization.T("err_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveAs()
    {
        using var dialog = new SaveFileDialog();
        dialog.Title = Localization.T("dlg_save_as");
        dialog.Filter = BuildFilterFromEnum();
        dialog.AddExtension = true;
        dialog.OverwritePrompt = true;

        if (dialog.ShowDialog() != DialogResult.OK) {
            return;
        }

        // estrai estensione
        string ext = Path.GetExtension(dialog.FileName).TrimStart('.').ToLower();

        // trova il formato corrispondente
        ExportFormat format = Enum.GetValues<ExportFormat>()
            .FirstOrDefault(f => f.GetExtension().Equals(ext, StringComparison.OrdinalIgnoreCase));

        // esporta
        ExportByFormat(dialog.FileName, format);
    }

    private void ExportByFormat(string path, ExportFormat format)
    {
        string text = textQrCode.Text.Trim();

        if (string.IsNullOrEmpty(text)) {
            MessageBox.Show(Localization.T("err_no_text"), Localization.T("err_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try {
            QrCodeOptions options = CreateQrCodeOptions();
            byte[] data = QrGenerator.Generate(text, format, options);
            File.WriteAllBytes(path, data);
        } catch (Exception ex) {
            MessageBox.Show($"{Localization.T("err_export")} {format}: {ex.Message}", Localization.T("err_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void panelFg_Click(object sender, EventArgs e)
    {
        if (colorDialog.ShowDialog() != DialogResult.OK) {
            return;
        }

        fgColor = colorDialog.Color;
        panelFg.BackColor = fgColor;
        GenerateQrPreview();
    }

    private void panelBg_Click(object sender, EventArgs e)
    {
        if (colorDialog.ShowDialog() != DialogResult.OK) {
            return;
        }

        bgColor = colorDialog.Color;
        panelBg.BackColor = bgColor;
        GenerateQrPreview();
    }

    private void logoPath_Click(object sender, EventArgs e)
    {
        if (logoBitmap != null) {
            ClearLogo();
        } else {
            OpenFileDialog? dialog = openFileDialog;

            dialog.Title = Localization.T("logo_set");
            dialog.Filter = $"{Localization.T("dlg_image_files")}|*.png;*.jpg;*.jpeg;*.bmp;*.gif";

            if (dialog.ShowDialog() == DialogResult.OK) {
                logoBitmap = new Bitmap(dialog.FileName);
                logoPath.Text = Localization.T("logo_remove");
                tooltip.SetToolTip(logoPath, Localization.T("logo_remove"));
            }
        }

        GenerateQrPreview();
    }

    private void ClearLogo()
    {
        logoBitmap = null;
        logoPath.Text = Localization.T("logo_set");
        tooltip.SetToolTip(logoPath, Localization.T("logo_set"));
    }

    private void imageMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
    {
        imageMenu.Hide();

        if (e.ClickedItem == saveAsToolStripMenuItem) {
            SaveAs();
        } else if (e.ClickedItem == copyAsImageToolStripMenuItem) {
            CopyImageToClipboard();
        } else if (e.ClickedItem == copyAsSVGToolStripMenuItem) {
            CopySvgToClipboard();
        }
    }

    private static string BuildFilterFromEnum()
    {
        ExportFormat[] formats = Enum.GetValues<ExportFormat>();

        IEnumerable<string> parts = formats.Select(f =>
        {
            string ext = f.GetExtension();
            return $"{f} (*.{ext})|*.{ext}";
        });

        return string.Join("|", parts);
    }

    private void saveButton_Click(object sender, EventArgs e)
    {
        SaveAs();
    }

    private void copyAsImageButton_Click(object sender, EventArgs e)
    {
        CopyImageToClipboard();
    }

    private void copyAsSvgButton_Click(object sender, EventArgs e)
    {
        CopySvgToClipboard();
    }

    private void helpButton_Click(object sender, EventArgs e)
    {
        string helpText = $"*** {Project.Title} ***\n\n" +
            $"- {Localization.T("help_p1")}\n" +
            $"- {Localization.T("help_p2")}\n" +
            $"- {Localization.T("help_p3")}\n" +
            "\n" +
            $"{Localization.T("help_p4")}\n" +
            $"{Localization.T("help_p5")}\n\n" +
            $"{Localization.T("help_formats")}\n" +
            $"\u2022 {Localization.T("fmt_url")}: http://, https://, ftp://, www., domain.com\n" +
            $"\u2022 {Localization.T("fmt_mail")}: user@domain.com;subject;body\n" +
            $"\u2022 {Localization.T("fmt_phone")}: +1234567890\n" +
            $"\u2022 {Localization.T("fmt_sms")}: 1234567890;message\n" +
            $"\u2022 {Localization.T("fmt_whatsapp")}: +1234567890;message\n" +
            $"\u2022 {Localization.T("fmt_wifi")}: WIFI:NetworkName;password\n" +
            $"\u2022 {Localization.T("fmt_geo")}: 45.4642,9.1900\n" +
            $"\u2022 {Localization.T("fmt_contact")}: FirstName;LastName;Phone;Email\n" +
            $"\u2022 {Localization.T("fmt_event")}: Title;Description;Location;StartDate;EndDate\n" +
            "\n" +
            Localization.T("help_note");

        MessageBox.Show(helpText, Localization.T("help_title"), MessageBoxButtons.OK, MessageBoxIcon.Question);
    }

    private void donateButton_Click(object sender, EventArgs e)
    {
        try {
            var psi = new ProcessStartInfo {
                FileName = DonateUrl,
                UseShellExecute = true
            };

            Process.Start(psi);
        } catch (Exception ex) {
            MessageBox.Show($"{Localization.T("err_open_url")}\n\n{ex.Message}", Localization.T("err_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void buttonRepo_Click(object sender, EventArgs e)
    {
        var psi = new ProcessStartInfo {
            FileName = RepoUrl,
            UseShellExecute = true
        };

        Process.Start(psi);
    }
}