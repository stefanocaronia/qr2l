using Avalonia;

namespace qr2l.Avalonia;

internal static class Program
{
    // Nessuna API Avalonia prima di AppMain: il framework non è ancora inizializzato.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Usato anche dal designer: non rimuovere.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
