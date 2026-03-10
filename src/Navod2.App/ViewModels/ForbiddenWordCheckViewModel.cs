using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Navod2.Core.Models;
using Navod2.Core.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Navod2.App.ViewModels;

public partial class ForbiddenWordCheckViewModel : ObservableObject
{
    private readonly ForbiddenWordService _service;
    private readonly MainViewModel _main;

    public ObservableCollection<CheckResult> Results { get; } = [];

    [ObservableProperty] private CheckResult? _selectedResult;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusText = "Spusťte kontrolu.";
    [ObservableProperty] private string _filterText = string.Empty;

    public ForbiddenWordCheckViewModel(ForbiddenWordService service, MainViewModel main)
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
        StatusText = "Probíhá kontrola...";

        await Task.Run(() =>
        {
            var results = _service.Check(_main.CurrentDocument).ToList();
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var r in results) Results.Add(r);
                StatusText = results.Count == 0
                    ? "Žádná zakázaná slova nenalezena."
                    : $"Nalezeno: {results.Count} výskytů zakázaných slov.";
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
            FileName = "zakázaná-slova.csv"
        };
        if (dialog.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("Zakázané slovo;Návrh;Kapitola;Kontext");
        foreach (var r in Results)
            sb.AppendLine($"\"{r.MatchedText}\";\"{r.Suggestion}\";\"{r.NodePath}\";\"{r.Context}\"");

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        _main.StatusMessage = $"Exportováno: {dialog.FileName}";
    }

    partial void OnFilterTextChanged(string value)
    {
        // Filtrování se řeší ve View přes CollectionView
    }
}
