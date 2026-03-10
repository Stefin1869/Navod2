namespace Navod2.Core.Models;

public enum DiffChangeType { Unchanged, Added, Deleted, Modified }

public class NodeDiff
{
    public DiffChangeType ChangeType { get; init; }
    public DocumentNode? NodeA { get; init; }
    public DocumentNode? NodeB { get; init; }
    public string NodeId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public List<LineDiff> TextDiffs { get; init; } = [];
}

public class LineDiff
{
    public DiffChangeType ChangeType { get; init; }
    public string Text { get; init; } = string.Empty;
}

public class DiffSummary
{
    public int Added { get; init; }
    public int Deleted { get; init; }
    public int Modified { get; init; }
    public int Unchanged { get; init; }
    public List<NodeDiff> NodeDiffs { get; init; } = [];
}
