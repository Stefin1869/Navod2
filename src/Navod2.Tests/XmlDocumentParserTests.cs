using Navod2.Core.Parsers;

namespace Navod2.Tests;

public class XmlDocumentParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly XmlDocumentParser _sut = new();

    public XmlDocumentParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // --- Základní parsování ---

    [Fact]
    public async Task ParseAsync_ValidXml_ReturnsRootNode()
    {
        var path = WriteXml(SimpleXml);

        var root = await _sut.ParseAsync(path);

        Assert.NotNull(root);
    }

    [Fact]
    public async Task ParseAsync_ValidXml_ParsesTopLevelNodes()
    {
        var path = WriteXml(SimpleXml);

        var root = await _sut.ParseAsync(path);

        Assert.Equal(2, root.Children.Count);
    }

    [Fact]
    public async Task ParseAsync_ValidXml_ParsesNodeTitle()
    {
        var path = WriteXml(SimpleXml);

        var root = await _sut.ParseAsync(path);

        Assert.Contains(root.Children, n => n.Title == "Kapitola 1");
    }

    [Fact]
    public async Task ParseAsync_ValidXml_ExtractsTextFromP()
    {
        var path = WriteXml(SimpleXml);

        var root = await _sut.ParseAsync(path);
        var node = root.Children.First(n => n.Title == "Kapitola 1");

        Assert.Contains("Text první kapitoly", node.Text);
    }

    [Fact]
    public async Task ParseAsync_ValidXml_ExtractsTextFromTitel()
    {
        var path = WriteXml(SimpleXml);

        var root = await _sut.ParseAsync(path);
        var node = root.Descendants().First(n => n.Title == "Podkapitola");

        Assert.Contains("Titulek podkapitoly", node.Text);
    }

    [Fact]
    public async Task ParseAsync_ValidXml_IgnoresNbspOnlyParagraphs()
    {
        var path = WriteXml(XmlWithNbsp);

        var root = await _sut.ParseAsync(path);
        var texts = root.Descendants().Select(n => n.Text).Where(t => !string.IsNullOrEmpty(t));

        Assert.DoesNotContain(texts, t => t.Trim() == "\u00A0" || t.Trim() == "");
    }

    [Fact]
    public async Task ParseAsync_ValidXml_SetsNodePath()
    {
        var path = WriteXml(SimpleXml);

        var root = await _sut.ParseAsync(path);
        var subNode = root.Descendants().FirstOrDefault(n => n.Title == "Podkapitola");

        Assert.NotNull(subNode);
        Assert.Contains("Kapitola 1", subNode.NodePath);
        Assert.Contains("Podkapitola", subNode.NodePath);
    }

    [Fact]
    public async Task ParseAsync_ValidXml_SetsIoId()
    {
        var path = WriteXml(SimpleXml);

        var root = await _sut.ParseAsync(path);

        Assert.Contains(root.Children, n => n.IoId == "io-kap1");
    }

    [Fact]
    public async Task ParseAsync_NestedNodes_BuildsTree()
    {
        var path = WriteXml(SimpleXml);

        var root = await _sut.ParseAsync(path);
        var kap1 = root.Children.First(n => n.Title == "Kapitola 1");

        Assert.Single(kap1.Children);
        Assert.Equal("Podkapitola", kap1.Children[0].Title);
    }

    [Fact]
    public async Task ParseAsync_ReportsProgress()
    {
        var path = WriteXml(SimpleXml);
        var progressValues = new List<int>();

        await _sut.ParseAsync(path, new Progress<int>(p => progressValues.Add(p)));

        Assert.Contains(100, progressValues);
    }

    [Fact]
    public async Task ParseAsync_InvalidFile_ThrowsException()
    {
        var path = Path.Combine(_tempDir, "invalid.xml");
        await File.WriteAllTextAsync(path, "tohle není xml <<<");

        await Assert.ThrowsAnyAsync<Exception>(() => _sut.ParseAsync(path));
    }

    // --- Integrace s reálným souborem (přeskočit pokud není dostupný) ---

    private const string RealXmlPath =
        @"C:\Users\Zverec.Stefan\source\repos\Navod2\source\SK336_1 - Kodiaq iV - ROW (11.2025), 2, cs_CZ.xml";

    [Fact]
    public async Task ParseAsync_RealXml_ParsesWithoutException()
    {
        if (!File.Exists(RealXmlPath))
            return; // skip pokud soubor není přítomen

        var root = await _sut.ParseAsync(RealXmlPath);

        Assert.NotNull(root);
        Assert.NotEmpty(root.Children);
    }

    [Fact]
    public async Task ParseAsync_RealXml_ExtractsNonEmptyText()
    {
        if (!File.Exists(RealXmlPath))
            return;

        var root = await _sut.ParseAsync(RealXmlPath);
        var nodesWithText = root.Descendants().Where(n => !string.IsNullOrWhiteSpace(n.Text)).ToList();

        Assert.NotEmpty(nodesWithText);
    }

    [Fact]
    public async Task ParseAsync_RealXml_AllNodesHaveNodePath()
    {
        if (!File.Exists(RealXmlPath))
            return;

        var root = await _sut.ParseAsync(RealXmlPath);

        Assert.All(root.Descendants(), n => Assert.NotEmpty(n.NodePath));
    }

    // --- Pomocné metody ---

    private string WriteXml(string xml)
    {
        var path = Path.Combine(_tempDir, "test.xml");
        File.WriteAllText(path, xml, System.Text.Encoding.UTF8);
        return path;
    }

    private const string SimpleXml = """
        <?xml version='1.0' encoding='utf-8'?>
        <!DOCTYPE root [<!ELEMENT root ANY>]>
        <root y.io.id="root-io" y.io.language="cs" y.io.variant="CZ">
          <node name="Kapitola 1" id="kap1" y.io.id="io-kap1">
            <p y.id="p1">Text první kapitoly.</p>
            <node name="Podkapitola" id="sub1" y.io.id="io-sub1">
              <titel y.id="t1">Titulek podkapitoly</titel>
              <p y.id="p2">Text podkapitoly.</p>
            </node>
          </node>
          <node name="Kapitola 2" id="kap2" y.io.id="io-kap2">
            <p y.id="p3">Text druhé kapitoly.</p>
          </node>
        </root>
        """;

    private const string XmlWithNbsp = """
        <?xml version='1.0' encoding='utf-8'?>
        <!DOCTYPE root [<!ELEMENT root ANY>]>
        <root y.io.language="cs" y.io.variant="CZ">
          <node name="Test" id="n1" y.io.id="io-n1">
            <p y.id="p1">&#160;</p>
            <p y.id="p2">Skutečný text odstavce.</p>
            <p y.id="p3">&#160;</p>
          </node>
        </root>
        """;
}
