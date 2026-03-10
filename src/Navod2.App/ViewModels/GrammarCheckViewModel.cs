using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Navod2.Core.Models;
using Navod2.Core.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Navod2.App.ViewModels;

public partial class GrammarCheckViewModel : ObservableObject
{
    private readonly GrammarCheckService _service;
    private readonly MainViewModel _main;

    public ObservableCollection<CheckResult> Results { get; } = [];

    [ObservableProperty] private CheckResult? _selectedResult;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusText = "Spusťte kontrolu.";
    [ObservableProperty] private bool _languageToolAvailable;
    [ObservableProperty] private string _languageToolUrl = "http://localhost:8081";

    public GrammarCheckViewModel(GrammarCheckService service, MainViewModel main)
    {
        _service = service;
        _main = main;
        _ = CheckLanguageToolAsync();
    }

    public void OnDocumentLoaded() => StatusText = "Dokument načten. Spusťte kontrolu.";

    [RelayCommand]
    private async Task CheckLanguageToolAsync()
    {
        _service.Configure(LanguageToolUrl);
        LanguageToolAvailable = await _service.IsAvailableAsync();
        StatusText = LanguageToolAvailable
            ? "LanguageTool dostupný."
            : "LanguageTool není dostupný. Spusťte lokální instanci nebo nastavte URL.";
    }

    [RelayCommand]
    private async Task RunCheckAsync()
    {
        if (_main.CurrentDocument is null)
        {
            StatusText = "Nejprve načtěte dokument.";
            return;
        }
        if (!LanguageToolAvailable)
        {
            StatusText = "LanguageTool není dostupný.";
            return;
        }

        IsRunning = true;
        Results.Clear();
        StatusText = "Probíhá kontrola gramatiky...";
        int count = 0;

        await foreach (var result in _service.CheckAsync(_main.CurrentDocument))
        {
            Results.Add(result);
            count++;
            StatusText = $"Nalezeno: {count} problémů...";
        }

        IsRunning = false;
        StatusText = count == 0
            ? "Žádné gramatické problémy nenalezeny."
            : $"Nalezeno: {count} gramatických problémů.";
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (Results.Count == 0) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exportovat výsledky",
            Filter = "CSV|*.csv",
            FileName = "gramatika.csv"
        };
        if (dialog.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("Chyba;Návrh;Kapitola;Kontext");
        foreach (var r in Results)
            sb.AppendLine($"\"{r.Message}\";\"{r.Suggestion}\";\"{r.NodePath}\";\"{r.Context}\"");

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        _main.StatusMessage = $"Exportováno: {dialog.FileName}";
    }

    partial void OnLanguageToolUrlChanged(string value)
    {
        _service.Configure(value);
    }
}
