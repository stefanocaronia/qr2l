using Avalonia;
using Avalonia.Controls;

namespace qr2l.GUI;

/// <summary>
/// Pannello quadrato: il lato è la larghezza disponibile, ridotto solo se manca l'altezza (schermi piccoli).
/// </summary>
public class SquarePanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var square = new Size(Side(availableSize), Side(availableSize));

        foreach (Control child in Children) {
            child.Measure(square);
        }

        return square;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double side = Side(finalSize);
        var bounds = new Rect((finalSize.Width - side) / 2, (finalSize.Height - side) / 2, side, side);

        foreach (Control child in Children) {
            child.Arrange(bounds);
        }

        return finalSize;
    }

    private static double Side(Size size)
    {
        double side = Math.Min(size.Width, size.Height);
        return double.IsInfinity(side) ? 0 : side;
    }
}
