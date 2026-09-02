using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using qr2l.Core;

namespace qr2l.GUI;

public partial class App : Application
{
    private const string ThemeSettingsKey = "theme";

    public static bool IsDark => Current?.RequestedThemeVariant == ThemeVariant.Dark;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Localization.Initialize();

        // Tema chiaro di default; la scelta manuale viene ricordata tra le sessioni
        RequestedThemeVariant = UserSettings.Get(ThemeSettingsKey) == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

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
