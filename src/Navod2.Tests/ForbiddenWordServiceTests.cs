using Navod2.Core.Models;
using Navod2.Core.Services;

namespace Navod2.Tests;

public class ForbiddenWordServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ForbiddenWordService _sut;

    public ForbiddenWordServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _sut = new ForbiddenWordService(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // --- Načítání a výchozí data ---

    [Fact]
    public async Task LoadAsync_NoFile_CreatesDefaultWords()
    {
        await _sut.LoadAsync();

        var words = _sut.GetAll();
        Assert.NotEmpty(words);
    }

    [Fact]
    public async Task LoadAsync_NoFile_DefaultsContainMod()
    {
        await _sut.LoadAsync();

        Assert.Contains(_sut.GetAll(), w => w.Word == "Mód");
    }

    // --- CRUD ---

    [Fact]
    public async Task Add_NewWord_AppearsInList()
    {
        await _sut.LoadAsync();
        var word = new ForbiddenWord { Word = "TestSlovo", Suggestion = "SpravneSlovo" };

        _sut.Add(word);

        Assert.Contains(_sut.GetAll(), w => w.Word == "TestSlovo");
    }

    [Fact]
    public async Task Add_AssignsNewId()
    {
        await _sut.LoadAsync();
        var word = new ForbiddenWord { Word = "X", Id = "old-id" };

        _sut.Add(word);

        var added = _sut.GetAll().Single(w => w.Word == "X");
        Assert.NotEqual("old-id", added.Id);
    }

    [Fact]
    public async Task Delete_ExistingWord_RemovesFromList()
    {
        await _sut.LoadAsync();
        var word = new ForbiddenWord { Word = "TempSlovo" };
        _sut.Add(word);
        var id = _sut.GetAll().Single(w => w.Word == "TempSlovo").Id;

        _sut.Delete(id);

        Assert.DoesNotContain(_sut.GetAll(), w => w.Word == "TempSlovo");
    }

    [Fact]
    public async Task Update_ExistingWord_ChangesProperties()
    {
        await _sut.LoadAsync();
        var word = new ForbiddenWord { Word = "Stare" };
        _sut.Add(word);
        var id = _sut.GetAll().Single(w => w.Word == "Stare").Id;

        _sut.Update(new ForbiddenWord { Id = id, Word = "Nove", Suggestion = "Spravne" });

        var updated = _sut.GetAll().Single(w => w.Id == id);
        Assert.Equal("Nove", updated.Word);
        Assert.Equal("Spravne", updated.Suggestion);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesWords()
    {
        await _sut.LoadAsync();
        _sut.Add(new ForbiddenWord { Word = "Persistovane", Suggestion = "Ulozene", Category = "test" });
        await _sut.SaveAsync();

        var sut2 = new ForbiddenWordService(_tempDir);
        await sut2.LoadAsync();

        Assert.Contains(sut2.GetAll(), w => w.Word == "Persistovane" && w.Category == "test");
    }

    // --- Kontrola zakázaných slov ---

    [Fact]
    public async Task Check_FindsForbiddenWord_InNodeText()
    {
        await _sut.LoadAsync();
        var node = NodeWithText("Stiskněte plynový pedál naplno.");

        var results = _sut.Check(node).ToList();

        Assert.Contains(results, r => r.MatchedText.Equals("Plynový pedál", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Check_ReturnsSuggestion()
    {
        await _sut.LoadAsync();
        var node = NodeWithText("Stisnkete plynový pedál.");

        var result = _sut.Check(node).First(r => r.MatchedText.Equals("Plynový pedál", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Akcelerační pedál", result.Suggestion);
    }

    [Fact]
    public async Task Check_CaseInsensitive_FindsUppercaseVariant()
    {
        await _sut.LoadAsync();
        // "Senzor" je v defaultním seznamu; test ověřuje case-insensitive matching
        var node = NodeWithText("SENZOR otáček detekoval závadu.");

        var results = _sut.Check(node).ToList();

        Assert.Contains(results, r => r.MatchedText.Equals("Senzor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Check_DoesNotMatchPartialWord()
    {
        await _sut.LoadAsync();
        // "Mód" nesmí matchnout uvnitř slova "módní"
        var node = NodeWithText("Módní doplňky vozu.");

        var results = _sut.Check(node).ToList();

        Assert.DoesNotContain(results, r => r.MatchedText.Equals("Mód", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Check_NoMatchInCleanText_ReturnsEmpty()
    {
        await _sut.LoadAsync();
        var node = NodeWithText("Akcelerační pedál je zcela v pořádku.");

        var results = _sut.Check(node).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public async Task Check_MultipleOccurrences_ReturnsAll()
    {
        await _sut.LoadAsync();
        var node = NodeWithText("Plynový pedál vlevo, plynový pedál vpravo.");

        var results = _sut.Check(node)
            .Where(r => r.MatchedText.Equals("Plynový pedál", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Check_IncludesContextAroundMatch()
    {
        await _sut.LoadAsync();
        var node = NodeWithText("Stisnkete plynový pedál pro zrychlení.");

        var result = _sut.Check(node).First(r => r.MatchedText.Equals("Plynový pedál", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("pedál", result.Context);
    }

    [Fact]
    public async Task Check_SetsNodePath()
    {
        await _sut.LoadAsync();
        var node = NodeWithText("Přepněte mód.", path: "Kapitola 1 > Ovládání");

        var result = _sut.Check(node).FirstOrDefault();

        Assert.Equal("Kapitola 1 > Ovládání", result?.NodePath);
    }

    [Fact]
    public async Task Check_SearchesChildNodes()
    {
        await _sut.LoadAsync();
        var root = new DocumentNode { Id = "root", Title = "Root" };
        // "Senzor" je celé slovo – bude matchovat
        var child = new DocumentNode { Id = "child", Title = "Child", Text = "Senzor polohy sedadla.", Parent = root };
        root.Children.Add(child);

        var results = _sut.Check(root).ToList();

        Assert.Contains(results, r => r.NodeId == "child");
    }

    // --- Pomocné metody ---

    private static DocumentNode NodeWithText(string text, string path = "Test > Uzel") =>
        new() { Id = "test-node", Title = "Test", Text = text, NodePath = path };
}
