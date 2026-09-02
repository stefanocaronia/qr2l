namespace qr2l.Core;

/// <summary>
/// Preferenze utente persistenti (lingua, tema) salvate in un semplice file chiave=valore
/// dentro la cartella dei dati applicazione.
/// </summary>
public static class UserSettings
{
    #region Constants and Fields

    private const string SettingsFolderName = "qr2l";
    private const string SettingsFileName = "settings.txt";

    private static readonly Lazy<Dictionary<string, string>> values = new(Load);

    #endregion

    #region Properties

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        SettingsFolderName,
        SettingsFileName);

    #endregion

    #region Public Methods

    public static string? Get(string key)
    {
        return values.Value.TryGetValue(key, out string? value) ? value : null;
    }

    public static void Set(string key, string value)
    {
        values.Value[key] = value;
        Save();
    }

    #endregion

    #region Private Methods

    private static Dictionary<string, string> Load()
    {
        var loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try {
            if (!File.Exists(SettingsPath)) {
                return loaded;
            }

            foreach (string line in File.ReadAllLines(SettingsPath)) {
                int separator = line.IndexOf('=');

                if (separator <= 0) {
                    continue;
                }

                loaded[line[..separator].Trim()] = line[(separator + 1)..].Trim();
            }
        } catch {
            // Un file illeggibile equivale a nessuna preferenza salvata.
        }

        return loaded;
    }

    private static void Save()
    {
        try {
            string? folder = Path.GetDirectoryName(SettingsPath);

            if (folder != null && !Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllLines(SettingsPath, values.Value.Select(pair => $"{pair.Key}={pair.Value}"));
        } catch {
            // La persistenza è un optional: se fallisce le preferenze restano valide per la sessione.
        }
    }

    #endregion
}
