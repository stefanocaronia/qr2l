using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using qr2l.Core;

namespace qr2l.GUI;

public partial class App : Application
{
    private const string ThemeSettingsKey = "theme";

    /// <summary>
    /// Vero se l'interfaccia è scura: una scelta esplicita dell'utente ha la precedenza,
    /// altrimenti conta il tema effettivo, che con "Default" è quello del sistema.
    /// </summary>
    public static bool IsDark
    {
        get
        {
            ThemeVariant? requested = Current?.RequestedThemeVariant;

            if (requested == ThemeVariant.Dark) {
                return true;
            }

            if (requested == ThemeVariant.Light) {
                return false;
            }

            return Current?.ActualThemeVariant == ThemeVariant.Dark;
        }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Localization.Initialize();

        // Si segue il tema di sistema finché l'utente non ne sceglie uno: da lì vale la scelta salvata
        RequestedThemeVariant = UserSettings.Get(ThemeSettingsKey) switch {
            "dark" => ThemeVariant.Dark,
            "light" => ThemeVariant.Light,
            var _ => ThemeVariant.Default
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void ToggleTheme()
    {
        if (Current == null) {
            return;
        }

        bool toDark = !IsDark;
        Current.RequestedThemeVariant = toDark ? ThemeVariant.Dark : ThemeVariant.Light;
        UserSettings.Set(ThemeSettingsKey, toDark ? "dark" : "light");
    }
}
