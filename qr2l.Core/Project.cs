using System.Reflection;

namespace qr2l.Core;

public static class Project
{
    #region Constants and Fields

    private static readonly Lazy<string> product = new(() =>
    {
        try {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var productAttribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            return productAttribute?.Product ?? "qr2l - QR Code Tool";
        } catch {
            return "qr2l - QR Code Tool";
        }
    });

    private static readonly Lazy<string> version = new(() =>
    {
        try {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            Version? assemblyVersion = assembly.GetName().Version;
            return assemblyVersion != null
                ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}"
                : "0.0.0";
        } catch {
            return "0.0.0";
        }
    });
    
    private static readonly Lazy<string> description = new(() =>
    {
        try {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var descriptionAttribute = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
            return descriptionAttribute?.Description ?? "QR Code Generator Tool";
        } catch {
            return "QR Code Generator Tool";
        }
    });

    private static readonly Lazy<string> title = new(() => $"{Product} - {Description} v{Version}");

    #endregion

    #region Properties

    public static string Version => version.Value;
    public static string Title => title.Value;
    public static string Product => product.Value;
    public static string Description => description.Value;

    #endregion
}