namespace Analyzer
{
    // A descriptive observation about one header; it carries no judgement about security.
    // Findings describe present values; Recommendation is only set for a missing header.
    public record HeaderObservation(string Name, bool IsPresent, IReadOnlyList<string> Values, string Purpose, string Limitation)
    {
        public IReadOnlyList<string> Findings { get; init; } = Array.Empty<string>();

        public string? Recommendation { get; init; }
    }
}
