using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Navod2.Core.Models;
using Navod2.Core.Parsers;
using Navod2.Core.Services;
using System.IO;

namespace Navod2.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly XmlDocumentParser _xmlParser = new();
    private readonly HtmlZipParser _zipParser = new();
    private readonly PdfDocumentParser _pdfParser = new();

    public ForbiddenWordCheckViewModel ForbiddenWordCheck { get; }
    public ForbiddenWordListViewModel ForbiddenWordList { get; }
    public GrammarCheckViewModel GrammarCheck { get; }
    public DiffViewModel Diff { get; }
    public NumberFormatViewModel NumberFormat { get; }

    [ObservableProperty] private DocumentNode? _currentDocument;
    [ObservableProperty] private string _documentTitle = "Žádný dokument";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _loadProgress;
    [ObservableProperty] private string _statusMessage = "Připraven. Načtěte dokument.";
    [ObservableProperty] private int _selectedTabIndex;

    public MainViewModel(ForbiddenWordService forbiddenWordService, GrammarCheckService grammarCheckService)
    {
        ForbiddenWordCheck = new ForbiddenWordCheckViewModel(forbiddenWordService, this);
        ForbiddenWordList = new ForbiddenWordListViewModel(forbiddenWordService);
        GrammarCheck = new GrammarCheckViewModel(grammarCheckService, this);
        Diff = new DiffViewModel();
        NumberFormat = new NumberFormatViewModel(new NumberFormatService(), this);
    }

    [RelayCommand]
    private async Task LoadDocumentAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Načíst dokument",
            Filter = "Podporované soubory|*.xml;*.zip;*.pdf|XML (COSIMA)|*.xml|ZIP (HTML)|*.zip|PDF|*.pdf|Všechny soubory|*.*"
        };

        if (dialog.ShowDialog() != true) return;
        await LoadFromPathAsync(dialog.FileName);
    }

    public async Task LoadFromPathAsync(string path)
    {
        if (!File.Exists(path)) return;

        IsLoading = true;
        LoadProgress = 0;
        StatusMessage = $"Načítám: {Path.GetFileName(path)}...";

        try
        {
            var progress = new Progress<int>(p => LoadProgress = p);
            var ext = Path.GetExtension(path).ToLowerInvariant();

            CurrentDocument = ext switch
            {
                ".xml" => await _xmlParser.ParseAsync(path, progress),
                ".zip" => await _zipParser.ParseAsync(path, progress),
                ".pdf" => await _pdfParser.ParseAsync(path, progress),
                _ => throw new NotSupportedException($"Formát '{ext}' není podporován.")
            };

            DocumentTitle = Path.GetFileName(path);
            int nodeCount = CurrentDocument.Descendants().Count();
            StatusMessage = $"Načteno: {DocumentTitle} ({nodeCount} uzlů)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Chyba: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            LoadProgress = 100;
        }
    }

    partial void OnCurrentDocumentChanged(DocumentNode? value)
    {
        ForbiddenWordCheck.OnDocumentLoaded();
        GrammarCheck.OnDocumentLoaded();
        NumberFormat.OnDocumentLoaded();
    }
}
