using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Navod2.Core.Models;
using Navod2.Core.Services;
using System.Collections.ObjectModel;

namespace Navod2.App.ViewModels;

public partial class ForbiddenWordListViewModel : ObservableObject
{
    private readonly ForbiddenWordService _service;

    public ObservableCollection<ForbiddenWord> Words { get; } = [];

    [ObservableProperty] private ForbiddenWord? _selectedWord;
    [ObservableProperty] private string _editWord = string.Empty;
    [ObservableProperty] private string _editSuggestion = string.Empty;
    [ObservableProperty] private string _editReason = string.Empty;
    [ObservableProperty] private string _editCategory = string.Empty;
    [ObservableProperty] private bool _editCaseSensitive;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusText = string.Empty;

    public ForbiddenWordListViewModel(ForbiddenWordService service)
    {
        _service = service;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _service.LoadAsync();
        RefreshList();
    }

    private void RefreshList()
    {
        Words.Clear();
        foreach (var w in _service.GetAll().OrderBy(w => w.Category).ThenBy(w => w.Word))
            Words.Add(w);
    }

    [RelayCommand]
    private void NewWord()
    {
        SelectedWord = null;
        EditWord = string.Empty;
        EditSuggestion = string.Empty;
        EditReason = string.Empty;
        EditCategory = "terminologie";
        EditCaseSensitive = false;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedWord is null) return;
        EditWord = SelectedWord.Word;
        EditSuggestion = SelectedWord.Suggestion;
        EditReason = SelectedWord.Reason;
        EditCategory = SelectedWord.Category;
        EditCaseSensitive = SelectedWord.CaseSensitive;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (string.IsNullOrWhiteSpace(EditWord))
        {
            StatusText = "Slovo nesmí být prázdné.";
            return;
        }

        if (SelectedWord is null)
        {
            _service.Add(new ForbiddenWord
            {
                Word = EditWord.Trim(),
                Suggestion = EditSuggestion.Trim(),
                Reason = EditReason.Trim(),
                Category = EditCategory.Trim(),
                CaseSensitive = EditCaseSensitive
            });
        }
        else
        {
            _service.Update(new ForbiddenWord
            {
                Id = SelectedWord.Id,
                Word = EditWord.Trim(),
                Suggestion = EditSuggestion.Trim(),
                Reason = EditReason.Trim(),
                Category = EditCategory.Trim(),
                CaseSensitive = EditCaseSensitive
            });
        }

        await _service.SaveAsync();
        RefreshList();
        IsEditing = false;
        StatusText = "Uloženo.";
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedWord is null) return;

        var word = SelectedWord.Word;
        _service.Delete(SelectedWord.Id);
        await _service.SaveAsync();
        RefreshList();
        StatusText = $"Smazáno: {word}";
    }

    partial void OnSelectedWordChanged(ForbiddenWord? value)
    {
        IsEditing = false;
    }
}
