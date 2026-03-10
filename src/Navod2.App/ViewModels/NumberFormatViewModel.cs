using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Navod2.Core.Models;
using Navod2.Core.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Navod2.App.ViewModels;

public partial class NumberFormatViewModel : ObservableObject
{
    private readonly NumberFormatService _service;
    private readonly MainViewModel _main;

    public ObservableCollection<CheckResult> Results { get; } = [];

    [ObservableProperty] private CheckResult? _selectedResult;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusText = "Spusťte kontrolu.";

    public NumberFormatViewModel(NumberFormatService service, MainViewModel main)
    {
        _service = service;
        _main = main;
    }

    public void OnDocumentLoaded() => StatusText = "Dokument načten. Spusťte kontrolu.";

    [RelayCommand]
    private async Task RunCheckAsync()
    {
        if (_main.CurrentDocument is null)
        {
            StatusText = "Nejprve načtěte dokument.";
            return;
        }

        IsRunning = true;
        Results.Clear();
        StatusText = "Probíhá kontrola formátu čísel...";

        await Task.Run(() =>
        {
            var results = _service.Check(_main.CurrentDocument).ToList();
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var r in results) Results.Add(r);
                StatusText = results.Count == 0
                    ? "Žádné problémy s formátem čísel."
                    : $"Nalezeno: {results.Count} porušení pravidel.";
            });
        });

        IsRunning = false;
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (Results.Count == 0) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exportovat výsledky",
            Filter = "CSV|*.csv",
            FileName = "formát-čísel.csv"
        };
        if (dialog.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("Problém;Návrh;Nalezeno;Kapitola;Kontext");
        foreach (var r in Results)
            sb.AppendLine($"\"{r.Message}\";\"{r.Suggestion}\";\"{r.MatchedText}\";\"{r.NodePath}\";\"{r.Context}\"");

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        _main.StatusMessage = $"Exportováno: {dialog.FileName}";
    }
}
