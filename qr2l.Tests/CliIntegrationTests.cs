using System.Diagnostics;
using Xunit;

namespace qr2l.Tests;

public class CliIntegrationTests
{
    #region Constants and Fields

    private const string CliExecutable = "qr2l.CLI.exe";
    private readonly string cliPath;

    #endregion

    public CliIntegrationTests()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        cliPath = Path.Combine(baseDir, "..", "..", "..", "..", "qr2l.CLI", "bin", "Debug", "net9.0", CliExecutable);
        cliPath = Path.GetFullPath(cliPath);
    }

    [Fact]
    public void Cli_NoArguments_ShouldShowUsage()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        (string output, int exitCode) process = RunCli("");

        Assert.Contains("Usage:", process.output);
        Assert.Contains("qr2l", process.output);
    }

    [Fact]
    public void Cli_GeneratePng_ShouldCreateFile()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        string outputFile = Path.GetTempFileName() + ".png";

        try {
            (string output, int exitCode) process = RunCli($"\"Hello World\" \"{outputFile}\"");

            Assert.True(File.Exists(outputFile), "Output file should be created");

            var fileInfo = new FileInfo(outputFile);
            Assert.True(fileInfo.Length > 100, "File should have content");
        } finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Cli_GenerateSvg_ShouldCreateFile()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        string outputFile = Path.GetTempFileName() + ".svg";

        try {
            (string output, int exitCode) process = RunCli($"\"Test SVG\" \"{outputFile}\"");

            Assert.True(File.Exists(outputFile), "Output file should be created");

            string content = File.ReadAllText(outputFile);
            Assert.Contains("<svg", content);
        } finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Cli_WithErrorCorrectionOption_ShouldSucceed()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        string outputFile = Path.GetTempFileName() + ".png";

        try {
            (string output, int exitCode) process = RunCli($"\"Test\" \"{outputFile}\" --error-correction=high");

            Assert.True(File.Exists(outputFile));
            Assert.Contains("Error Correction: High", process.output);
        } finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Cli_WithCustomColors_ShouldSucceed()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        string outputFile = Path.GetTempFileName() + ".png";

        try {
            (string output, int exitCode) process = RunCli($"\"Test\" \"{outputFile}\" --dark-color=FF0000 --light-color=00FF00");

            Assert.True(File.Exists(outputFile));
            Assert.Contains("Colors:", process.output);
        } finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Cli_WithPixelsPerModule_ShouldSucceed()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        string outputFile = Path.GetTempFileName() + ".png";

        try {
            (string output, int exitCode) process = RunCli($"\"Test\" \"{outputFile}\" --pixels-per-module=10");

            Assert.True(File.Exists(outputFile));
        } finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Cli_WithPayloadModeUrl_ShouldSucceed()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        string outputFile = Path.GetTempFileName() + ".png";

        try {
            (string output, int exitCode) process = RunCli($"\"example.com\" \"{outputFile}\" --payload-mode=url");

            Assert.True(File.Exists(outputFile));
            Assert.Contains("Payload Mode: Url", process.output);
        } finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Cli_WithPayloadModeWifi_ShouldSucceed()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        string outputFile = Path.GetTempFileName() + ".png";

        try {
            (string output, int exitCode) process = RunCli($"\"MyNetwork;password123\" \"{outputFile}\" --payload-mode=wifi --wifi-auth=wpa");

            Assert.True(File.Exists(outputFile));
            Assert.Contains("WiFi Auth: WPA", process.output);
        } finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public void Cli_InvalidFormat_ShouldFail()
    {
        if (!File.Exists(cliPath)) {
            return;
        }

        string outputFile = Path.GetTempFileName() + ".invalid";

        try {
            (string output, int exitCode) process = RunCli($"\"Test\" \"{outputFile}\"");

            Assert.Contains("Error", process.output);
        } finally {
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
        }
    }

    private (string output, int exitCode) RunCli(string arguments)
    {
        var startInfo = new ProcessStartInfo {
            FileName = cliPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(startInfo);

        if (process == null) {
            return (string.Empty, -1);
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        string combinedOutput = output + error;
        return (combinedOutput, process.ExitCode);
    }
}