using System.Diagnostics;
using qr2l.Core;
using Timer = System.Windows.Forms.Timer;

namespace qr2l.GUI;

public partial class Form1 : Form
{
    #region Constants and Fields

    private const float RefreshDelay = 0.1f;
    private const string DonateUrl = "https://paypal.me/stefanocaronia";
    private const string LicenseUrl = "https://creativecommons.org/licenses/by-sa/4.0/";
    private const string RepoUrl = "https://github.com/stefanocaronia/qr2l";
    private const string SetLogoText = "Set the logo";
    private const string RemoveLogoText = "Remove the logo";
    private const string Version = "1.0.1";

    private readonly Timer debounceTimer;
    private byte[]? pngData;
    private string? svgData;

    private Color fgColor = Color.Black;
    private Color bgColor = Color.White;
    private Bitmap? logoBitmap;

    #endregion

    public Form1()
    {
        InitializeComponent();

        Text = $"qr2l v{Version} - QR Code Tool";

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

        logoPath.Text = SetLogoText;

        saveAsDialog.Filter = BuildFilterFromEnum();
        

        RefreshButtonStates();
    }

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

    private void OnDebounceTimerTick(object sender, EventArgs e)
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
            MessageBox.Show("QR Code copied to clipboard as an image!", "Copy Image to Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
            MessageBox.Show($"Error copying image to clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopySvgToClipboard()
    {
        if (svgData == null) {
            return;
        }

        try {
            Clipboard.SetText(svgData);
            MessageBox.Show("SVG QR Code copied to clipboard as text!", "Copy SVG to Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
            MessageBox.Show($"Error copying SVG to clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveAs()
    {
        using var dialog = new SaveFileDialog();
        dialog.Title = "Save As";
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
            MessageBox.Show("Please enter text to generate QR code.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try {
            QrCodeOptions options = CreateQrCodeOptions();
            byte[] data = QrGenerator.Generate(text, format, options);
            File.WriteAllBytes(path, data);
        } catch (Exception ex) {
            MessageBox.Show($"Error exporting {format}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        if (logoPath.Text == RemoveLogoText) {
            ClearLogo();
        } else {
            OpenFileDialog? dialog = openFileDialog;

            dialog.Title = SetLogoText;
            dialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif";

            if (dialog.ShowDialog() == DialogResult.OK) {
                logoBitmap = new Bitmap(dialog.FileName);
                logoPath.Text = RemoveLogoText;
            }
        }

        GenerateQrPreview();
    }

    private void ClearLogo()
    {
        logoBitmap = null;
        logoPath.Text = SetLogoText;
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
        string helpText = $"*** qr2l v{Version} - QR Code Tool ***\n\n" +
            "- Enter the text you want to encode in the QR code in the text box.\n" +
            "- Choose foreground and background colors by clicking on the color panels.\n" +
            "- Optionally, add a logo by clicking on the logo box. Click again to clear the logo.\n" +
            "\n" +
            "The QR code preview will update automatically.\n" +
            "Use the buttons to save the QR code or copy it to the clipboard as an image or SVG.\n\n" +
            "Supported Formats (auto-detected):\n" +
            "• URLs: http://, https://, ftp://, www., domain.com\n" +
            "• Email: user@domain.com;subject;body (subject and body optional)\n" +
            "• Phone: +1234567890 or (123) 456-7890\n" +
            "• SMS: 1234567890;message text\n" +
            "• WhatsApp: +1234567890;message text\n" +
            "• WiFi: WIFI:NetworkName;password or WIFI:T:WPA;S:SSID;P:password;\n" +
            "• Geolocation: 45.4642,9.1900 (latitude,longitude)\n" +
            "• Contact: FirstName;LastName;Phone;Email\n" +
            "• Event: Title;Description;Location;StartDate;EndDate\n" +
            "\n" +
            "Note: WiFi networks require the WIFI: prefix. You can use the simplified\n" +
            "format (WIFI:SSID;password) or the complete format for advanced options.";
        MessageBox.Show(helpText, "Help", MessageBoxButtons.OK, MessageBoxIcon.Question);
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
            MessageBox.Show("Cannot open the url.\n\n" + ex.Message);
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