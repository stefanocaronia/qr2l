using System.Drawing.Drawing2D;

namespace qr2l.GUI;

/// <summary>
/// Icone disegnate a runtime: non richiedono asset esterni e possono essere ridisegnate
/// nei colori del tema corrente, cosa impossibile con i PNG a colore fisso.
/// </summary>
public static class Icons
{
    #region Constants and Fields

    private const int Size = 16;

    private static readonly Color SunColor = Color.FromArgb(240, 180, 40);
    private static readonly Color MoonColor = Color.FromArgb(140, 150, 235);

    #endregion

    #region Public Methods

    public static Bitmap Sun()
    {
        Bitmap bitmap = CreateCanvas(out Graphics graphics);

        using (graphics) {
            using var pen = new Pen(SunColor, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var brush = new SolidBrush(SunColor);

            graphics.FillEllipse(brush, 5.5f, 5.5f, 5f, 5f);

            // Otto raggi attorno al disco
            const float center = 8f;

            for (var i = 0; i < 8; i++) {
                double angle = i * Math.PI / 4d;
                var dx = (float)Math.Cos(angle);
                var dy = (float)Math.Sin(angle);

                graphics.DrawLine(pen,
                    center + (dx * 5.2f), center + (dy * 5.2f),
                    center + (dx * 7f), center + (dy * 7f));
            }
        }

        return bitmap;
    }

    public static Bitmap Moon()
    {
        Bitmap bitmap = CreateCanvas(out Graphics graphics);

        using (graphics) {
            using var brush = new SolidBrush(MoonColor);
            using var path = new GraphicsPath();

            // Falce ottenuta sottraendo un cerchio sfalsato al disco pieno
            path.AddEllipse(2f, 2f, 12f, 12f);

            using var cut = new GraphicsPath();
            cut.AddEllipse(5.5f, 0.5f, 11.5f, 11.5f);

            using var region = new Region(path);
            region.Exclude(cut);

            graphics.FillRegion(brush, region);
        }

        return bitmap;
    }

    /// <summary>
    /// Icona "immagine" per il pulsante del logo, tinta secondo il colore del testo richiesto.
    /// </summary>
    public static Bitmap Picture(Color color)
    {
        Bitmap bitmap = CreateCanvas(out Graphics graphics);

        using (graphics) {
            using var pen = new Pen(color, 1.3f);
            using var brush = new SolidBrush(color);

            // Cornice
            graphics.DrawRectangle(pen, 2f, 3f, 12f, 10f);

            // Sole nell'angolo
            graphics.FillEllipse(brush, 4.5f, 5f, 2.6f, 2.6f);

            // Montagna
            PointF[] mountain = [
                new PointF(3.2f, 12.2f),
                new PointF(7f, 8f),
                new PointF(10f, 12.2f)
            ];

            graphics.FillPolygon(brush, mountain);

            PointF[] hill = [
                new PointF(8.4f, 12.2f),
                new PointF(11f, 9.2f),
                new PointF(13f, 12.2f)
            ];

            graphics.FillPolygon(brush, hill);
        }

        return bitmap;
    }

    #endregion

    #region Private Methods

    private static Bitmap CreateCanvas(out Graphics graphics)
    {
        var bitmap = new Bitmap(Size, Size);
        graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        return bitmap;
    }

    #endregion
}
