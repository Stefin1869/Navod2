using System.Xml.Linq;
using Navod2.Core.Models;

namespace Navod2.Core.Parsers;

/// <summary>
/// Parsuje XML soubor ve formátu VW DOCUFY K4 TREE (COSIMA export).
/// </summary>
public class XmlDocumentParser
{
    private static readonly XName[] TextElements =
    [
        XName.Get("p"), XName.Get("titel"), XName.Get("warnung"),
        XName.Get("vorsicht"), XName.Get("hinweis")
    ];

    public async Task<DocumentNode> ParseAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            progress?.Report(0);
            var doc = XDocument.Load(filePath);
            progress?.Report(50);
            var root = doc.Root ?? throw new InvalidDataException("Prázdný XML soubor.");
            var rootNode = new DocumentNode { Id = "root", Title = "Dokument", IoId = root.Attribute("y.io.id")?.Value ?? "" };
            int nodeIndex = 0;
            int totalNodes = root.Descendants("node").Count();

            foreach (var xmlNode in root.Elements("node"))
                ParseNode(xmlNode, rootNode, ref nodeIndex, totalNodes, progress);

            progress?.Report(100);
            return rootNode;
        });
    }

    private static void ParseNode(XElement element, DocumentNode parent, ref int index, int total, IProgress<int>? progress)
    {
        var nodeId = element.Attribute("id")?.Value ?? Guid.NewGuid().ToString();
        var nodeName = element.Attribute("name")?.Value ?? string.Empty;
        var path = parent.NodePath.Length > 0 ? $"{parent.NodePath} > {nodeName}" : nodeName;

        var text = ExtractText(element);

        var node = new DocumentNode
        {
            Id = nodeId,
            IoId = element.Attribute("y.io.id")?.Value ?? "",
            Title = nodeName,
            Text = text,
            NodePath = path,
            Parent = parent
        };
        parent.Children.Add(node);

        index++;
        if (total > 0)
            progress?.Report(50 + (int)(index * 50.0 / total));

        foreach (var child in element.Elements("node"))
            ParseNode(child, node, ref index, total, progress);
    }

    private static string ExtractText(XElement element)
    {
        var parts = new List<string>();

        foreach (var descendant in element.Descendants())
        {
            if (!TextElements.Contains(descendant.Name))
                continue;

            var text = descendant.Value.Trim();
            // Přeskočit prázdné odstavce (jen &nbsp; = \u00A0)
            if (string.IsNullOrWhiteSpace(text) || text == "\u00A0")
                continue;

            parts.Add(text);
        }

        return string.Join("\n", parts);
    }
}
