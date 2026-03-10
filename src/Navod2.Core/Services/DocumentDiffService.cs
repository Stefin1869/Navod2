using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Navod2.Core.Models;

namespace Navod2.Core.Services;

public class DocumentDiffService
{
    public DiffSummary Compare(DocumentNode rootA, DocumentNode rootB)
    {
        // Indexovat uzly dle IoId (stabilní COSIMA ID), fallback na název
        var nodesA = IndexNodes(rootA);
        var nodesB = IndexNodes(rootB);

        var allIds = nodesA.Keys.Union(nodesB.Keys).ToList();
        var diffs = new List<NodeDiff>();

        foreach (var id in allIds)
        {
            nodesA.TryGetValue(id, out var nodeA);
            nodesB.TryGetValue(id, out var nodeB);

            NodeDiff diff;

            if (nodeA is null)
            {
                diff = new NodeDiff { ChangeType = DiffChangeType.Added, NodeB = nodeB, NodeId = id, Title = nodeB!.Title };
            }
            else if (nodeB is null)
            {
                diff = new NodeDiff { ChangeType = DiffChangeType.Deleted, NodeA = nodeA, NodeId = id, Title = nodeA.Title };
            }
            else
            {
                var textDiffs = BuildTextDiff(nodeA.Text, nodeB.Text);
                bool changed = textDiffs.Any(d => d.ChangeType != DiffChangeType.Unchanged);
                diff = new NodeDiff
                {
                    ChangeType = changed ? DiffChangeType.Modified : DiffChangeType.Unchanged,
                    NodeA = nodeA,
                    NodeB = nodeB,
                    NodeId = id,
                    Title = nodeB.Title,
                    TextDiffs = textDiffs
                };
            }

            diffs.Add(diff);
        }

        return new DiffSummary
        {
            Added = diffs.Count(d => d.ChangeType == DiffChangeType.Added),
            Deleted = diffs.Count(d => d.ChangeType == DiffChangeType.Deleted),
            Modified = diffs.Count(d => d.ChangeType == DiffChangeType.Modified),
            Unchanged = diffs.Count(d => d.ChangeType == DiffChangeType.Unchanged),
            NodeDiffs = diffs
        };
    }

    private static Dictionary<string, DocumentNode> IndexNodes(DocumentNode root)
    {
        var result = new Dictionary<string, DocumentNode>();
        foreach (var node in root.DescendantsAndSelf())
        {
            // Preferovat IoId (COSIMA), fallback na Id
            var key = !string.IsNullOrEmpty(node.IoId) ? node.IoId : node.Id;
            if (key != "root" && !result.ContainsKey(key))
                result[key] = node;
        }
        return result;
    }

    private static List<LineDiff> BuildTextDiff(string textA, string textB)
    {
        var result = InlineDiffBuilder.Diff(textA, textB);
        return result.Lines.Select(line => new LineDiff
        {
            ChangeType = line.Type switch
            {
                ChangeType.Inserted => DiffChangeType.Added,
                ChangeType.Deleted => DiffChangeType.Deleted,
                ChangeType.Modified => DiffChangeType.Modified,
                _ => DiffChangeType.Unchanged
            },
            Text = line.Text
        }).ToList();
    }
}
