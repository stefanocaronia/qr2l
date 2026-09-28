using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using Material.Icons;

namespace qr2l.GUI;

/// <summary>
/// Icona Material adattata ai contorni del simbolo, non al quadrato di 24 unità in cui è disegnata:
/// così icone diverse con la stessa altezza sembrano davvero alte uguali.
/// </summary>
public class Glyph : Avalonia.Controls.Shapes.Path
{
    public static readonly StyledProperty<MaterialIconKind> KindProperty =
        AvaloniaProperty.Register<Glyph, MaterialIconKind>(nameof(Kind));

    static Glyph()
    {
        StretchProperty.OverrideDefaultValue<Glyph>(Stretch.Uniform);
    }

    public Glyph()
    {
        // Senza un colore proprio prende quello del testo intorno, che stili e stati possono cambiare
        Bind(FillProperty, this.GetObservable(TextElement.ForegroundProperty), BindingPriority.Style);
    }

    public MaterialIconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == KindProperty) {
            Data = Geometry.Parse(MaterialIconDataProvider.GetData(Kind));
        }
    }
}
