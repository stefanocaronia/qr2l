using System.Runtime.InteropServices;

namespace qr2l.GUI;

public enum ThemeMode
{
    Light,
    Dark
}

/// <summary>
/// Tema chiaro/scuro della GUI: parte dal tema chiaro, salva la scelta manuale
/// e applica i colori all'intero albero dei controlli.
/// </summary>
public static class AppTheme
{
    #region Constants and Fields

    private const string SettingsKey = "theme";
    private const int DwmUseImmersiveDarkMode = 20;

    #endregion

    #region Properties

    public static ThemeMode Current { get; private set; } = ThemeMode.Light;

    public static bool IsDark => Current == ThemeMode.Dark;

    // Sfondo generale della finestra
    public static Color Background => IsDark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;

    // Barre superiore e inferiore
    public static Color Surface => IsDark ? Color.FromArgb(45, 45, 45) : SystemColors.Control;

    // Campi di input
    public static Color InputBackground => IsDark ? Color.FromArgb(27, 27, 27) : SystemColors.Window;

    public static Color Foreground => IsDark ? Color.FromArgb(230, 230, 230) : SystemColors.ControlText;

    public static Color Border => IsDark ? Color.FromArgb(70, 70, 70) : SystemColors.ControlDark;

    #endregion

    #region Public Methods

    /// <summary>
    /// Imposta il tema iniziale: quello salvato dall'utente, altrimenti il tema chiaro.
    /// </summary>
    public static void Initialize()
    {
        Current = UserSettings.Get(SettingsKey) == "dark" ? ThemeMode.Dark : ThemeMode.Light;
    }

    public static void Toggle()
    {
        Current = IsDark ? ThemeMode.Light : ThemeMode.Dark;
        UserSettings.Set(SettingsKey, IsDark ? "dark" : "light");
    }

    /// <summary>
    /// Applica ricorsivamente i colori del tema corrente a tutti i controlli del form.
    /// </summary>
    public static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Foreground;

        ApplyToChildren(form);
        ApplyTitleBar(form);
    }

    #endregion

    #region Private Methods

    private static void ApplyToChildren(Control parent)
    {
        foreach (Control control in parent.Controls) {
            switch (control) {
                // I pannelli colore mostrano la scelta dell'utente: non vanno tematizzati
                case Panel panel when panel.Name is "panelFg" or "panelBg":
                    continue;

                case TextBox textBox:
                    textBox.BackColor = InputBackground;
                    textBox.ForeColor = Foreground;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ComboBox comboBox:
                    comboBox.BackColor = InputBackground;
                    comboBox.ForeColor = Foreground;
                    comboBox.FlatStyle = IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                    break;

                case Button button:
                    button.BackColor = Surface;
                    button.ForeColor = Foreground;
                    button.FlatStyle = IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                    button.FlatAppearance.BorderColor = Border;
                    button.UseVisualStyleBackColor = !IsDark;
                    break;

                case Label label:
                    label.BackColor = Surface;
                    label.ForeColor = Foreground;
                    break;

                case FlowLayoutPanel or TableLayoutPanel or Panel:
                    control.BackColor = control.Parent is Form ? Background : Surface;
                    control.ForeColor = Foreground;
                    break;

                case PictureBox:
                    control.BackColor = Background;
                    break;
            }

            if (control.ContextMenuStrip != null) {
                ApplyToMenu(control.ContextMenuStrip);
            }

            if (control.HasChildren) {
                ApplyToChildren(control);
            }
        }
    }

    private static void ApplyToMenu(ContextMenuStrip menu)
    {
        menu.BackColor = Surface;
        menu.ForeColor = Foreground;
        menu.RenderMode = IsDark ? ToolStripRenderMode.System : ToolStripRenderMode.ManagerRenderMode;

        foreach (ToolStripItem item in menu.Items) {
            item.BackColor = Surface;
            item.ForeColor = Foreground;
        }
    }

    /// <summary>
    /// Adegua anche la barra del titolo, che Windows disegna fuori dal controllo di WinForms.
    /// </summary>
    private static void ApplyTitleBar(Form form)
    {
        try {
            int useDark = IsDark ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkMode, ref useDark, sizeof(int));

            // Forza il ridisegno della cornice, altrimenti il cambio si vede solo al prossimo resize
            if (form.IsHandleCreated && form.Visible) {
                form.Invalidate(true);
                form.Refresh();
            }
        } catch {
            // Attributo non supportato su versioni precedenti di Windows: la barra resta chiara.
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    #endregion
}
