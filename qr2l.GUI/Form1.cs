using System.Diagnostics;
using System.Globalization;
using qr2l.Core;
using Timer = System.Windows.Forms.Timer;

namespace qr2l.GUI;

public partial class Form1 : Form
{
    #region Constants and Fields

    private const float RefreshDelay = 0.1f;
    private const string DonateUrl = "https://paypal.me/stefanocaronia";

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
        textLogoPath.Cursor = Cursors.Hand;

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
        
        RefreshButtonStates();
    }

    private QrCodeOptions CreateQrCodeOptions()
    {
        return new QrCodeOptions {
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
            MessageBox.Show("QR Code copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
            MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopySvgToClipboard()
    {
        if (svgData == null) {
            return;
        }

        try {
            Clipboard.SetText(svgData);
            MessageBox.Show("SVG QR Code copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            var options = CreateQrCodeOptions();
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

    private void textLogoPath_Click(object sender, EventArgs e)
    {
        if (textLogoPath.Text != string.Empty) {
            ClearLogo();
        } else {
            OpenFileDialog? dialog = openFileDialog;

            dialog.Title = "Select logo image";
            dialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif";

            if (dialog.ShowDialog() == DialogResult.OK) {
                logoBitmap = new Bitmap(dialog.FileName);
                textLogoPath.Text = Path.GetFileName(dialog.FileName);
            }
        }

        GenerateQrPreview();
    }

    private void ClearLogo()
    {
        logoBitmap = null;
        textLogoPath.Text = "";
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
        string helpText = "*** QR Code Generator Help ***\n\n" +
            "- Enter the text you want to encode in the QR code in the text box.\n" +
            "- Choose foreground and background colors by clicking on the color panels.\n" +
            "- Optionally, add a logo by clicking on the logo box. Click again to clear the logo.\n" +
            "\n" +
            "The QR code preview will update automatically.\n" +
            "Use the buttons to save the QR code or copy it to the clipboard as an image or SVG.";
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

    private void circleStyle_CheckedChanged(object sender, EventArgs e)
    {
        throw new System.NotImplementedException();
    }
}