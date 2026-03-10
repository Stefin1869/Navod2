namespace Navod2.Core.Models;

public class ForbiddenWord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Word { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; } = false;
    public string Category { get; set; } = string.Empty;
}
