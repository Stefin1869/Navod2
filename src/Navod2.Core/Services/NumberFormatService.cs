using System.Text.RegularExpressions;
using Navod2.Core.Models;

namespace Navod2.Core.Services;

/// <summary>
/// Kontroluje formát čísel a typografická pravidla dle Redaktorské příručky 1.53.
/// </summary>
public partial class NumberFormatService
{
    private record Rule(string Id, string Message, string Suggestion, Regex Pattern);

    private static readonly List<Rule> Rules =
    [
        new("decimal-dot",
            "Desetinná tečka místo čárky",
            "Použijte desetinnou čárku: např. 123,5",
            DecimalDotRegex()),

        new("missing-space-5digit",
            "Pětimístné (a větší) číslo bez oddělení skupin",
            "Oddělte skupiny po třech číslicích tvrdou mezerou: např. 100 000",
            FiveDigitRegex()),

        new("unit-no-space",
            "Jednotka bez mezery od číselné hodnoty",
            "Oddělte číslo od jednotky mezerou: např. 10 km, 30 min",
            UnitNoSpaceRegex()),

        new("percent-no-space",
            "Procento bez mezery",
            "Použijte mezeru před %: např. 15 %",
            PercentNoSpaceRegex()),

        new("range-hyphen",
            "Číselný rozsah se spojovníkem místo pomlčky",
            "Použijte pomlčku (–) bez mezer pro rozsahy: např. 110–6000",
            RangeHyphenRegex()),

        new("range-spaces",
            "Číselný rozsah s mezerami kolem pomlčky",
            "Rozsah hodnot pište bez mezer: např. 110–6000",
            RangeSpacesRegex()),

        new("degree-no-space",
            "Stupeň bez mezery od čísla",
            "Oddělte číslo od °C / °F mezerou: např. 5 °C",
            DegreeNoSpaceRegex()),
    ];

    public IEnumerable<CheckResult> Check(DocumentNode rootNode)
    {
        foreach (var node in rootNode.DescendantsAndSelf())
        {
            if (string.IsNullOrWhiteSpace(node.Text)) continue;

            foreach (var rule in Rules)
            {
                foreach (Match match in rule.Pattern.Matches(node.Text))
                {
                    var contextStart = Math.Max(0, match.Index - 30);
                    var contextEnd = Math.Min(node.Text.Length, match.Index + match.Length + 30);

                    yield return new CheckResult
                    {
                        Type = CheckResultType.NumberFormat,
                        NodeId = node.Id,
                        NodePath = node.NodePath,
                        MatchedText = match.Value,
                        Suggestion = rule.Suggestion,
                        Message = rule.Message,
                        Context = node.Text[contextStart..contextEnd],
                        ContextOffset = match.Index - contextStart,
                        MatchLength = match.Length,
                        SourceNode = node
                    };
                }
            }
        }
    }

    // Desetinná tečka v čísle (vynechat URL, verze softwaru)
    [GeneratedRegex(@"(?<!\w)\d+\.\d+(?!\w)", RegexOptions.Compiled)]
    private static partial Regex DecimalDotRegex();

    // 5+ místné číslo bez mezery uvnitř (a není část většího výrazu)
    [GeneratedRegex(@"(?<!\d)\d{5,}(?!\d)", RegexOptions.Compiled)]
    private static partial Regex FiveDigitRegex();

    // Jednotka hned za číslem bez mezery
    [GeneratedRegex(@"\d(km|m|cm|mm|kg|g|l|ml|kW|kWh|V|A|rpm|°C|°F|bar|Pa|kPa|MPa|Nm|min|ms|h|s)(?!\w)", RegexOptions.Compiled)]
    private static partial Regex UnitNoSpaceRegex();

    // Procento bez mezery
    [GeneratedRegex(@"\d%", RegexOptions.Compiled)]
    private static partial Regex PercentNoSpaceRegex();

    // Rozsah se spojovníkem (ASCII hyphen)
    [GeneratedRegex(@"\d+-\d+", RegexOptions.Compiled)]
    private static partial Regex RangeHyphenRegex();

    // Rozsah s mezerami kolem pomlčky (– = U+2013)
    [GeneratedRegex(@"\d+ – \d+", RegexOptions.Compiled)]
    private static partial Regex RangeSpacesRegex();

    // Stupeň bez mezery
    [GeneratedRegex(@"\d°", RegexOptions.Compiled)]
    private static partial Regex DegreeNoSpaceRegex();
}
