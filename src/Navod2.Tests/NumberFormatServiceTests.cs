using Navod2.Core.Models;
using Navod2.Core.Services;

namespace Navod2.Tests;

public class NumberFormatServiceTests
{
    private readonly NumberFormatService _sut = new();

    // --- Desetinná tečka ---

    [Fact]
    public void Check_DecimalDot_Detected()
    {
        var results = Check("Hodnota je 123.5 km.");
        AssertRule(results, "123.5", "desetinná");
    }

    [Theory]
    [InlineData("Hodnota je 123,5 km.")]
    [InlineData("Cena je 1 500 Kč.")]
    public void Check_ValidDecimal_NotFlagged(string text)
    {
        var results = Check(text);
        Assert.Empty(results.Where(r => r.Message.Contains("desetinn")));
    }

    // --- Pětimístná čísla ---

    [Fact]
    public void Check_FiveDigitNumber_WithoutSpaces_Detected()
    {
        var results = Check("Ujeté kilometry: 100000 km.");
        AssertRule(results, "100000", "Pětimístné");
    }

    [Fact]
    public void Check_FiveDigitNumber_WithSpace_NotFlagged()
    {
        var results = Check("Ujeté kilometry: 100\u00A0000 km.");
        Assert.Empty(results.Where(r => r.Message.Contains("Pětimístné")));
    }

    [Fact]
    public void Check_FourDigitNumber_NotFlagged()
    {
        var results = Check("Otáčky: 4500 ot/min.");
        Assert.Empty(results.Where(r => r.Message.Contains("Pětimístné")));
    }

    // --- Jednotka bez mezery ---

    [Theory]
    [InlineData("Rychlost 130km/h.")]
    [InlineData("Hmotnost 50kg.")]
    [InlineData("Objem 2l.")]
    [InlineData("Výkon 100kW.")]
    public void Check_UnitWithoutSpace_Detected(string text)
    {
        var results = Check(text);
        Assert.Contains(results, r => r.Message.Contains("mezery"));
    }

    [Theory]
    [InlineData("Rychlost 130 km/h.")]
    [InlineData("Hmotnost 50 kg.")]
    [InlineData("Výkon 100 kW.")]
    public void Check_UnitWithSpace_NotFlagged(string text)
    {
        var results = Check(text);
        Assert.Empty(results.Where(r => r.Message.Contains("Jednotka bez mezery")));
    }

    // --- Procenta ---

    [Fact]
    public void Check_PercentWithoutSpace_Detected()
    {
        var results = Check("Baterie nabitá na 80%.");
        AssertRule(results, "0%", "Procento");
    }

    [Fact]
    public void Check_PercentWithSpace_NotFlagged()
    {
        var results = Check("Baterie nabitá na 80 %.");
        Assert.Empty(results.Where(r => r.Message.Contains("Procento")));
    }

    // --- Rozsah se spojovníkem ---

    [Fact]
    public void Check_RangeWithHyphen_Detected()
    {
        var results = Check("Otáčky 1000-3000 ot/min.");
        AssertRule(results, "1000-3000", "spojovník");
    }

    [Fact]
    public void Check_RangeWithNdash_NotFlagged()
    {
        var results = Check("Otáčky 1000\u20133000 ot/min.");
        Assert.Empty(results.Where(r => r.Message.Contains("spojovník")));
    }

    // --- Rozsah s mezerami kolem pomlčky ---

    [Fact]
    public void Check_RangeWithSpacesAroundNdash_Detected()
    {
        var results = Check("Otáčky 1000 \u2013 3000 ot/min.");
        AssertRule(results, "1000 \u2013 3000", "mezerami");
    }

    // --- Stupeň bez mezery ---

    [Fact]
    public void Check_DegreeWithoutSpace_Detected()
    {
        var results = Check("Teplota je 20\u00B0C.");
        AssertRule(results, "20\u00B0", "Stupe\u0148");
    }

    [Fact]
    public void Check_DegreeWithSpace_NotFlagged()
    {
        var results = Check("Teplota je 20 \u00B0C.");
        Assert.Empty(results.Where(r => r.Message.Contains("Stupe\u0148")));
    }

    // --- Stromová struktura ---

    [Fact]
    public void Check_DetectsInChildNodes()
    {
        var root = new DocumentNode { Id = "root", Title = "Root" };
        var child = new DocumentNode { Id = "child", Title = "Child", Text = "Hodnota 99%.", Parent = root };
        root.Children.Add(child);

        var results = _sut.Check(root).ToList();

        Assert.Contains(results, r => r.NodeId == "child");
    }

    [Fact]
    public void Check_EmptyText_ReturnsNoResults()
    {
        var results = Check("   ");
        Assert.Empty(results);
    }

    [Fact]
    public void Check_ResultContainsContext()
    {
        var results = Check("Rychlost musí být 130km/h pro jízdu.");
        var result = results.FirstOrDefault(r => r.Message.Contains("mezery"));
        Assert.NotNull(result);
        Assert.NotEmpty(result.Context);
    }

    // --- Pomocné metody ---

    private List<CheckResult> Check(string text)
    {
        var node = new DocumentNode { Id = "n", Title = "T", Text = text, NodePath = "Test" };
        return _sut.Check(node).ToList();
    }

    private static void AssertRule(List<CheckResult> results, string matchedText, string messageContains)
    {
        Assert.Contains(results, r =>
            r.MatchedText.Contains(matchedText, StringComparison.OrdinalIgnoreCase) ||
            r.Message.Contains(messageContains, StringComparison.OrdinalIgnoreCase));
    }
}
