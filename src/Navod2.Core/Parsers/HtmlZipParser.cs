using System.IO.Compression;
using HtmlAgilityPack;
using Navod2.Core.Models;

namespace Navod2.Core.Parsers;

/// <summary>
/// Parsuje ZIP archiv obsahující HTML soubory (TopicPilot export z COSIMA).
/// Každý soubor: {hash}_{version}_cs_cz.html
/// </summary>
public class HtmlZipParser
{
    public async Task<DocumentNode> ParseAsync(string zipPath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            var root = new DocumentNode { Id = "root", Title = "Dokument" };

            using var archive = ZipFile.OpenRead(zipPath);
            var htmlEntries = archive.Entries
                .Where(e => e.Name.EndsWith("_cs_cz.html", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Name)
                .ToList();

            int total = htmlEntries.Count;
            int i = 0;

            foreach (var entry in htmlEntries)
            {
                using var stream = entry.Open();
                using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                var html = reader.ReadToEnd();

                var node = ParseHtmlEntry(entry.Name, html, root);
                root.Children.Add(node);

                i++;
                progress?.Report((int)(i * 100.0 / total));
            }

            return root;
        });
    }

    private static DocumentNode ParseHtmlEntry(string fileName, string html, DocumentNode parent)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var titleNode = doc.DocumentNode.SelectSingleNode("//h1 | //h2 | //title");
        var title = titleNode?.InnerText.Trim() ?? fileName;

        var textParts = new List<string>();
        foreach (var p in doc.DocumentNode.SelectNodes("//p | //li | //td") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = p.InnerText.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                textParts.Add(text);
        }

        return new DocumentNode
        {
            Id = fileName,
            IoId = fileName,
            Title = title,
            Text = string.Join("\n", textParts),
            NodePath = title,
            Parent = parent
        };
    }
}
