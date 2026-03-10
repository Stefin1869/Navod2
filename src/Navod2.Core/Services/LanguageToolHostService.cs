using System.Diagnostics;

namespace Navod2.Core.Services;

/// <summary>
/// Spravuje životní cyklus lokální LanguageTool instance (Java JAR).
/// </summary>
public class LanguageToolHostService : IDisposable
{
    private Process? _process;
    private bool _disposed;

    public int Port { get; private set; } = 8081;
    public bool IsRunning => _process is { HasExited: false };
    public string BaseUrl => $"http://localhost:{Port}";

    /// <summary>
    /// Spustí LanguageTool server ze zadané cesty k JAR souboru.
    /// </summary>
    public async Task<bool> StartAsync(string jarPath, int port = 8081)
    {
        if (IsRunning) return true;
        if (!File.Exists(jarPath)) return false;

        Port = port;

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-cp \"{jarPath}\" org.languagetool.server.HTTPServer --port {port} --allow-origin \"*\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        _process.Start();

        // Počkat na spuštění (max 15 sekund)
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(500);
            if (await IsResponding()) return true;
        }

        return false;
    }

    public void Stop()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill();
            _process.Dispose();
            _process = null;
        }
    }

    private async Task<bool> IsResponding()
    {
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await http.GetAsync($"{BaseUrl}/v2/languages");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
