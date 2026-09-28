using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace qr2l.Tests;

/// <summary>
/// Esegue la CLI compilata insieme ai test, come la userebbe una persona dal terminale.
/// </summary>
public sealed class CliIntegrationTests : IDisposable
{
    #region Constants and Fields

    private readonly string cliPath;
    private readonly string workDir;

    #endregion

    public CliIntegrationTests()
    {
        cliPath = FindCli();
        workDir = Path.Combine(Path.GetTempPath(), "qr2l-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
    }

    public void Dispose()
    {
        Directory.Delete(workDir, recursive: true);
    }

    [Fact]
    public void Cli_NoArguments_ShouldShowUsageAndFail()
    {
        (string output, int exitCode) = RunCli("");

        Assert.Equal(1, exitCode);
        Assert.Contains("Usage:", output);
        Assert.Contains("qr2l", output);
    }

    [Fact]
    public void Cli_GeneratePng_ShouldCreateFile()
    {
        string outputFile = OutputFile("png");

        (string _, int exitCode) = RunCli($"\"Hello World\" \"{outputFile}\"");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile), "Output file should be created");
        Assert.True(new FileInfo(outputFile).Length > 100, "File should have content");
    }

    [Fact]
    public void Cli_GenerateSvg_ShouldCreateFile()
    {
        string outputFile = OutputFile("svg");

        (string _, int exitCode) = RunCli($"\"Test SVG\" \"{outputFile}\"");

        Assert.Equal(0, exitCode);
        Assert.Contains("<svg", File.ReadAllText(outputFile));
    }

    [Fact]
    public void Cli_GeneratePdf_ShouldCreateFile()
    {
        string outputFile = OutputFile("pdf");

        (string _, int exitCode) = RunCli($"\"Test PDF\" \"{outputFile}\"");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(File.ReadAllBytes(outputFile), 0, 5));
    }

    [Fact]
    public void Cli_WithErrorCorrectionOption_ShouldSucceed()
    {
        string outputFile = OutputFile("png");

        (string output, int exitCode) = RunCli($"\"Test\" \"{outputFile}\" --error-correction=high");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile));
        Assert.Contains("Error Correction: High", output);
    }

    [Fact]
    public void Cli_WithCustomColors_ShouldSucceed()
    {
        string outputFile = OutputFile("png");

        (string output, int exitCode) = RunCli($"\"Test\" \"{outputFile}\" --dark-color=FF0000 --light-color=00FF00");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile));
        Assert.Contains("Colors: #FF0000 / #00FF00", output);
    }

    [Fact]
    public void Cli_WithPixelsPerModule_ShouldSucceed()
    {
        string outputFile = OutputFile("png");

        (string _, int exitCode) = RunCli($"\"Test\" \"{outputFile}\" --pixels-per-module=10");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile));
    }

    [Fact]
    public void Cli_WithPayloadModeUrl_ShouldSucceed()
    {
        string outputFile = OutputFile("png");

        (string output, int exitCode) = RunCli($"\"example.com\" \"{outputFile}\" --payload-mode=url");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile));
        Assert.Contains("Payload Mode: Url", output);
    }

    [Fact]
    public void Cli_WithPayloadModeWifi_ShouldSucceed()
    {
        string outputFile = OutputFile("png");

        (string output, int exitCode) = RunCli($"\"MyNetwork;password123\" \"{outputFile}\" --payload-mode=wifi --wifi-auth=wpa");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile));
        Assert.Contains("WiFi Auth: WPA", output);
    }

    [Fact]
    public void Cli_InvalidFormat_ShouldFail()
    {
        string outputFile = OutputFile("invalid");

        (string output, int exitCode) = RunCli($"\"Test\" \"{outputFile}\"");

        Assert.Equal(1, exitCode);
        Assert.Contains("Error", output);
        Assert.False(File.Exists(outputFile));
    }

    private string OutputFile(string extension)
    {
        return Path.Combine(workDir, "qr." + extension);
    }

    private (string output, int exitCode) RunCli(string arguments)
    {
        var startInfo = new ProcessStartInfo {
            FileName = cliPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Cannot start {cliPath}");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (output + error, process.ExitCode);
    }

    /// <summary>
    /// La CLI compilata con la stessa configurazione dei test. Il progetto di test la fa compilare
    /// prima di sé; se non c'è, i test devono fallire e non passare in silenzio.
    /// </summary>
    private static string FindCli()
    {
        string testBin = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var framework = new DirectoryInfo(testBin);
        string configuration = framework.Parent!.Name;
        string cliBin = Path.GetFullPath(Path.Combine(testBin, "..", "..", "..", "..", "qr2l.CLI", "bin", configuration, framework.Name));
        string executable = OperatingSystem.IsWindows() ? "qr2l.exe" : "qr2l";

        // La CLI è self-contained, quindi l'uscita di build sta nella cartella del runtime
        string[] candidates = [
            Path.Combine(cliBin, RuntimeInformation.RuntimeIdentifier, executable),
            Path.Combine(cliBin, executable)
        ];

        foreach (string candidate in candidates) {
            if (File.Exists(candidate)) {
                return candidate;
            }
        }

        throw new FileNotFoundException($"CLI executable not found. Searched: {string.Join(", ", candidates)}");
    }
}
