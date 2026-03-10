using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Navod2.Core.Models;
using Navod2.Core.Parsers;
using Navod2.Core.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace Navod2.App.ViewModels;

public partial class DiffViewModel : ObservableObject
{
    private readonly XmlDocumentParser _xmlParser = new();
    private readonly HtmlZipParser _zipParser = new();
    private readonly DocumentDiffService _diffService = new();

    private DocumentNode? _documentA;
    private DocumentNode? _documentB;

    public ObservableCollection<NodeDiff> NodeDiffs { get; } = [];

    [ObservableProperty] private string _pathA = string.Empty;
    [ObservableProperty] private string _pathB = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusText = "Načtěte dva dokumenty a spusťte porovnání.";
    [ObservableProperty] private NodeDiff? _selectedDiff;
    [ObservableProperty] private int _countAdded;
    [ObservableProperty] private int _countDeleted;
    [ObservableProperty] private int _countModified;
    [ObservableProperty] private int _countUnchanged;

    [RelayCommand]
    private async Task LoadDocumentAAsync()
    {
        var path = PickFile();
        if (path is null) return;
        PathA = path;
        _documentA = await ParseAsync(path);
        StatusText = _documentA is not null ? $"Dokument A: {Path.GetFileName(path)}" : "Chyba načítání dokumentu A.";
    }

    [RelayCommand]
    private async Task LoadDocumentBAsync()
    {
        var path = PickFile();
        if (path is null) return;
        PathB = path;
        _documentB = await ParseAsync(path);
        StatusText = _documentB is not null ? $"Dokument B: {Path.GetFileName(path)}" : "Chyba načítání dokumentu B.";
    }

    [RelayCommand]
    private async Task RunDiffAsync()
    {
        if (_documentA is null || _documentB is null)
        {
            StatusText = "Nejprve načtěte oba dokumenty.";
            return;
        }

        IsRunning = true;
        NodeDiffs.Clear();
        StatusText = "Porovnávám dokumenty...";

        await Task.Run(() =>
        {
            var summary = _diffService.Compare(_documentA, _documentB);
            App.Current.Dispatcher.Invoke(() =>
            {
                CountAdded = summary.Added;
                CountDeleted = summary.Deleted;
                CountModified = summary.Modified;
                CountUnchanged = summary.Unchanged;

                // Zobrazit jen změny
                foreach (var d in summary.NodeDiffs.Where(d => d.ChangeType != DiffChangeType.Unchanged))
                    NodeDiffs.Add(d);

                StatusText = $"Hotovo: +{summary.Added} přidáno, -{summary.Deleted} smazáno, ~{summary.Modified} změněno, ={summary.Unchanged} beze změny";
            });
        });

        IsRunning = false;
    }

    private async Task<DocumentNode?> ParseAsync(string path)
    {
        IsLoading = true;
        try
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".xml" => await _xmlParser.ParseAsync(path),
                ".zip" => await _zipParser.ParseAsync(path),
                _ => null
            };
        }
        catch { return null; }
        finally { IsLoading = false; }
    }

    private static string? PickFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Vybrat dokument",
            Filter = "Podporované soubory|*.xml;*.zip|XML|*.xml|ZIP|*.zip"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
