namespace Navod2.Core.Models;

public enum CheckResultType
{
    ForbiddenWord,
    Grammar,
    NumberFormat,
    Style
}

public class CheckResult
{
    public CheckResultType Type { get; init; }
    public string NodeId { get; init; } = string.Empty;
    public string NodePath { get; init; } = string.Empty;
    public string MatchedText { get; init; } = string.Empty;
    public string Suggestion { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public int ContextOffset { get; init; }
    public int MatchLength { get; init; }
    public DocumentNode? SourceNode { get; init; }
}
