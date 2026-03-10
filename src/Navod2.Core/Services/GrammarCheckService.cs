using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Navod2.Core.Models;

namespace Navod2.Core.Services;

public class GrammarCheckService
{
    private readonly HttpClient _http;
    private string _baseUrl = "http://localhost:8081";

    public GrammarCheckService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public void Configure(string baseUrl) => _baseUrl = baseUrl.TrimEnd('/');

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/v2/languages");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async IAsyncEnumerable<CheckResult> CheckAsync(DocumentNode rootNode)
    {
        foreach (var node in rootNode.DescendantsAndSelf())
        {
            if (string.IsNullOrWhiteSpace(node.Text)) continue;

            var results = await CheckTextAsync(node.Text, node);
            foreach (var r in results)
                yield return r;
        }
    }

    private async Task<IEnumerable<CheckResult>> CheckTextAsync(string text, DocumentNode node)
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["language"] = "cs",
                ["text"] = text
            });

            var response = await _http.PostAsync($"{_baseUrl}/v2/check", content);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync();
            var ltResponse = JsonSerializer.Deserialize<LanguageToolResponse>(json, JsonOptions);
            if (ltResponse?.Matches is null) return [];

            return ltResponse.Matches.Select(m => new CheckResult
            {
                Type = CheckResultType.Grammar,
                NodeId = node.Id,
                NodePath = node.NodePath,
                MatchedText = text.Substring(Math.Min(m.Offset, text.Length - 1),
                    Math.Min(m.Length, text.Length - m.Offset)),
                Suggestion = m.Replacements?.FirstOrDefault()?.Value ?? "",
                Message = m.Message ?? "",
                Context = m.Context?.Text ?? "",
                ContextOffset = m.Context?.Offset ?? 0,
                MatchLength = m.Context?.Length ?? m.Length,
                SourceNode = node
            });
        }
        catch
        {
            return [];
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record LanguageToolResponse(List<LtMatch>? Matches);
    private record LtMatch(string? Message, int Offset, int Length, List<LtReplacement>? Replacements, LtContext? Context);
    private record LtReplacement(string? Value);
    private record LtContext(string? Text, int Offset, int Length);
}
