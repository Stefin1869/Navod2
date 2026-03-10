namespace Navod2.Core.Models;

/// <summary>
/// Reprezentuje jeden uzel (kapitolu/odstavec) v načteném dokumentu.
/// </summary>
public class DocumentNode
{
    public string Id { get; init; } = string.Empty;
    public string IoId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string NodePath { get; init; } = string.Empty;
    public DocumentNode? Parent { get; init; }
    public List<DocumentNode> Children { get; init; } = [];

    public IEnumerable<DocumentNode> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var desc in child.Descendants())
                yield return desc;
        }
    }

    public IEnumerable<DocumentNode> DescendantsAndSelf()
    {
        yield return this;
        foreach (var desc in Descendants())
            yield return desc;
    }

    public override string ToString() => Title.Length > 0 ? Title : Id;
}
