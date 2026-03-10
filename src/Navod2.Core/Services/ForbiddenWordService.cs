using System.Text.Json;
using Navod2.Core.Models;

namespace Navod2.Core.Services;

public class ForbiddenWordService
{
    private readonly string _filePath;
    private List<ForbiddenWord> _words = [];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ForbiddenWordService(string dataDirectory)
    {
        _filePath = Path.Combine(dataDirectory, "forbidden-words.json");
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            _words = DefaultWords();
            await SaveAsync();
            return;
        }

        var json = await File.ReadAllTextAsync(_filePath);
        _words = JsonSerializer.Deserialize<List<ForbiddenWord>>(json) ?? [];
    }

    public IReadOnlyList<ForbiddenWord> GetAll() => _words.AsReadOnly();

    public void Add(ForbiddenWord word)
    {
        word.Id = Guid.NewGuid().ToString();
        _words.Add(word);
    }

    public void Update(ForbiddenWord updated)
    {
        var index = _words.FindIndex(w => w.Id == updated.Id);
        if (index >= 0) _words[index] = updated;
    }

    public void Delete(string id) => _words.RemoveAll(w => w.Id == id);

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(_words, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public IEnumerable<CheckResult> Check(DocumentNode rootNode)
    {
        foreach (var node in rootNode.DescendantsAndSelf())
        {
            if (string.IsNullOrWhiteSpace(node.Text)) continue;

            foreach (var forbidden in _words)
            {
                var comparison = forbidden.CaseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                int start = 0;
                while (true)
                {
                    int idx = node.Text.IndexOf(forbidden.Word, start, comparison);
                    if (idx < 0) break;

                    // Kontrola hranice slova
                    bool wordStart = idx == 0 || !char.IsLetter(node.Text[idx - 1]);
                    bool wordEnd = idx + forbidden.Word.Length >= node.Text.Length
                        || !char.IsLetter(node.Text[idx + forbidden.Word.Length]);

                    if (wordStart && wordEnd)
                    {
                        var contextStart = Math.Max(0, idx - 40);
                        var contextEnd = Math.Min(node.Text.Length, idx + forbidden.Word.Length + 40);
                        var context = node.Text[contextStart..contextEnd];

                        yield return new CheckResult
                        {
                            Type = CheckResultType.ForbiddenWord,
                            NodeId = node.Id,
                            NodePath = node.NodePath,
                            MatchedText = forbidden.Word,
                            Suggestion = forbidden.Suggestion,
                            Message = string.IsNullOrEmpty(forbidden.Suggestion)
                                ? $"Zakázané slovo: \u201e{forbidden.Word}\u201c"
                                : $"Zakázané slovo: \u201e{forbidden.Word}\u201c \u2192 použijte \u201e{forbidden.Suggestion}\u201c",
                            Context = context,
                            ContextOffset = idx - contextStart,
                            MatchLength = forbidden.Word.Length,
                            SourceNode = node
                        };
                    }

                    start = idx + 1;
                }
            }
        }
    }

    private static List<ForbiddenWord> DefaultWords() =>
    [
        new() { Word = "Plynový pedál", Suggestion = "Akcelerační pedál", Category = "terminologie" },
        new() { Word = "Zážehový motor", Suggestion = "Benzinový motor", Category = "terminologie" },
        new() { Word = "Za jízdy", Suggestion = "Během jízdy", Category = "terminologie" },
        new() { Word = "Čelní okno", Suggestion = "Čelní sklo", Category = "terminologie" },
        new() { Word = "Monochromatický displej", Suggestion = "Černobílý displej", Category = "terminologie" },
        new() { Word = "Vznětový motor", Suggestion = "Dieselový motor", Category = "terminologie" },
        new() { Word = "Webové stránky", Suggestion = "Internetové stránky", Category = "terminologie" },
        new() { Word = "Je možno", Suggestion = "Je možné / Můžete", Reason = "výjimka: právní texty", Category = "terminologie" },
        new() { Word = "V opačném případě", Suggestion = "Jinak", Category = "terminologie" },
        new() { Word = "Karosérie", Suggestion = "Karoserie", Category = "terminologie" },
        new() { Word = "Klíč na kola", Suggestion = "Klíč na šrouby kol", Category = "terminologie" },
        new() { Word = "Kontrolka", Suggestion = "Kontrolní světlo", Category = "terminologie" },
        new() { Word = "Kontrolní symbol", Suggestion = "Kontrolní světlo", Category = "terminologie" },
        new() { Word = "Ruční", Suggestion = "Manuální", Reason = "výjimka: ruční mytí", Category = "terminologie" },
        new() { Word = "Mechanismus", Suggestion = "Mechanizmus", Category = "terminologie" },
        new() { Word = "Diesel", Suggestion = "Nafta", Category = "terminologie" },
        new() { Word = "Běžící motor", Suggestion = "Nastartovaný motor", Category = "terminologie" },
        new() { Word = "Vyřazení z funkce", Suggestion = "Nefunkční", Category = "terminologie" },
        new() { Word = "Nevíste", Suggestion = "Nevíte", Category = "terminologie" },
        new() { Word = "Přístrojová deska", Suggestion = "Palubní deska", Category = "terminologie" },
        new() { Word = "Přístrojový panel", Suggestion = "Panel přístrojů", Category = "terminologie" },
        new() { Word = "Pohon 4x4", Suggestion = "Pohon všech kol", Reason = "výjimka: v tabulkách", Category = "terminologie" },
        new() { Word = "Řízení vpravo", Suggestion = "Pravostr. řízení", Category = "terminologie" },
        new() { Word = "Řízení vlevo", Suggestion = "Levostr. řízení", Category = "terminologie" },
        new() { Word = "Přiřazení", Suggestion = "Přidání", Reason = "výjimka: přiřazení loga stanice", Category = "terminologie" },
        new() { Word = "Přijmutí", Suggestion = "Přijetí", Category = "terminologie" },
        new() { Word = "Mód", Suggestion = "Režim", Category = "terminologie" },
        new() { Word = "Senzor", Suggestion = "Snímač", Category = "terminologie" },
        new() { Word = "Čidlo", Suggestion = "Snímač", Category = "terminologie" },
        new() { Word = "Totožný", Suggestion = "Stejný", Category = "terminologie" },
        new() { Word = "Střední konzole", Suggestion = "Středová konzola", Category = "terminologie" },
        new() { Word = "Textýlie", Suggestion = "Textilie", Category = "terminologie" },
        new() { Word = "Motorizace", Suggestion = "Typ motoru", Category = "terminologie" },
        new() { Word = "Neutrál", Suggestion = "Řadicí páka v neutrální poloze", Reason = "hovorové výraz", Category = "terminologie" },
        new() { Word = "Z výrobního závodu", Suggestion = "Z výroby", Category = "terminologie" },
        new() { Word = "Zvolený rychlostní stupeň", Suggestion = "Zařazený rychlostní stupeň", Category = "terminologie" },
        new() { Word = "Kromě toho", Suggestion = "Také", Category = "styl" },
        new() { Word = "Bezpodmínečně", Suggestion = "(vynechat – nadbytečné)", Category = "styl" },
        new() { Word = "Prosím", Suggestion = "(vynechat – nadbytečné)", Category = "styl" },
        new() { Word = "Přibližně asi", Suggestion = "Přibližně", Category = "styl" },
        new() { Word = "Zvolený rychlostní stupeň", Suggestion = "Zařazený rychlostní stupeň", Category = "terminologie" },
        new() { Word = "Zesílení", Suggestion = "Zvýšení", Reason = "pro intenzitu, hlučnost, riziko", Category = "terminologie" },
    ];
}
