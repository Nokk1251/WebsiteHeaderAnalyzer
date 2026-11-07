namespace Analyzer
{
    // One descriptive observation about a header value. NeedsAttention is a hint for presentation only.
    public record HeaderFinding(string Text, bool NeedsAttention);
}
