using Navod2.Core.Models;
using Navod2.Core.Services;

namespace Navod2.Tests;

public class DocumentDiffServiceTests
{
    private readonly DocumentDiffService _sut = new();

    [Fact]
    public void Compare_IdenticalDocuments_AllUnchanged()
    {
        var docA = BuildDoc(("id1", "ioid1", "Kapitola 1", "Text první kapitoly."));
        var docB = BuildDoc(("id1", "ioid1", "Kapitola 1", "Text první kapitoly."));

        var diff = _sut.Compare(docA, docB);

        Assert.Equal(0, diff.Added);
        Assert.Equal(0, diff.Deleted);
        Assert.Equal(0, diff.Modified);
        Assert.True(diff.Unchanged > 0);
    }

    [Fact]
    public void Compare_AddedNode_DetectedAsAdded()
    {
        var docA = BuildDoc(("id1", "ioid1", "Kapitola 1", "Text."));
        var docB = BuildDoc(
            ("id1", "ioid1", "Kapitola 1", "Text."),
            ("id2", "ioid2", "Kapitola 2", "Nový text."));

        var diff = _sut.Compare(docA, docB);

        Assert.Equal(1, diff.Added);
        Assert.Contains(diff.NodeDiffs, d => d.ChangeType == DiffChangeType.Added && d.NodeId == "ioid2");
    }

    [Fact]
    public void Compare_DeletedNode_DetectedAsDeleted()
    {
        var docA = BuildDoc(
            ("id1", "ioid1", "Kapitola 1", "Text."),
            ("id2", "ioid2", "Kapitola 2", "Stará kapitola."));
        var docB = BuildDoc(("id1", "ioid1", "Kapitola 1", "Text."));

        var diff = _sut.Compare(docA, docB);

        Assert.Equal(1, diff.Deleted);
        Assert.Contains(diff.NodeDiffs, d => d.ChangeType == DiffChangeType.Deleted && d.NodeId == "ioid2");
    }

    [Fact]
    public void Compare_ModifiedText_DetectedAsModified()
    {
        var docA = BuildDoc(("id1", "ioid1", "Kapitola 1", "Starý text odstavce."));
        var docB = BuildDoc(("id1", "ioid1", "Kapitola 1", "Nový text odstavce."));

        var diff = _sut.Compare(docA, docB);

        Assert.Equal(1, diff.Modified);
        Assert.Contains(diff.NodeDiffs, d => d.ChangeType == DiffChangeType.Modified && d.NodeId == "ioid1");
    }

    [Fact]
    public void Compare_ModifiedNode_ContainsTextDiffs()
    {
        var docA = BuildDoc(("id1", "ioid1", "Kap", "Řádek první.\nŘádek druhý."));
        var docB = BuildDoc(("id1", "ioid1", "Kap", "Řádek první.\nŘádek upravený."));

        var diff = _sut.Compare(docA, docB);

        var modifiedDiff = diff.NodeDiffs.Single(d => d.ChangeType == DiffChangeType.Modified);
        Assert.Contains(modifiedDiff.TextDiffs, l => l.ChangeType == DiffChangeType.Added);
        Assert.Contains(modifiedDiff.TextDiffs, l => l.ChangeType == DiffChangeType.Deleted);
    }

    [Fact]
    public void Compare_MatchesByIoId_NotById()
    {
        // Uzly mají různé Id, ale stejné IoId – mají být považovány za stejné
        var docA = BuildDoc(("id-a", "shared-ioid", "Kapitola", "Text."));
        var docB = BuildDoc(("id-b", "shared-ioid", "Kapitola", "Text."));

        var diff = _sut.Compare(docA, docB);

        Assert.Equal(0, diff.Added);
        Assert.Equal(0, diff.Deleted);
    }

    [Fact]
    public void Compare_Summary_CountsCorrectly()
    {
        var docA = BuildDoc(
            ("id1", "ioid1", "Nezmenena", "Stejný text."),
            ("id2", "ioid2", "Zmenena",   "Starý text."),
            ("id3", "ioid3", "Smazana",   "Smazaný text."));

        var docB = BuildDoc(
            ("id1", "ioid1", "Nezmenena", "Stejný text."),
            ("id2", "ioid2", "Zmenena",   "Nový text."),
            ("id4", "ioid4", "Pridana",   "Přidaný text."));

        var diff = _sut.Compare(docA, docB);

        Assert.Equal(1, diff.Added);
        Assert.Equal(1, diff.Deleted);
        Assert.Equal(1, diff.Modified);
        Assert.Equal(1, diff.Unchanged);
    }

    [Fact]
    public void Compare_EmptyDocuments_ReturnsEmptySummary()
    {
        var docA = new DocumentNode { Id = "root", Title = "Root" };
        var docB = new DocumentNode { Id = "root", Title = "Root" };

        var diff = _sut.Compare(docA, docB);

        Assert.Equal(0, diff.Added + diff.Deleted + diff.Modified + diff.Unchanged);
    }

    // --- Pomocné metody ---

    private static DocumentNode BuildDoc(params (string id, string ioId, string title, string text)[] nodes)
    {
        var root = new DocumentNode { Id = "root", Title = "Root" };
        foreach (var (id, ioId, title, text) in nodes)
        {
            root.Children.Add(new DocumentNode
            {
                Id = id,
                IoId = ioId,
                Title = title,
                Text = text,
                NodePath = title,
                Parent = root
            });
        }
        return root;
    }
}
