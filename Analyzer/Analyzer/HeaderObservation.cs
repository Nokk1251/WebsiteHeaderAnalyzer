namespace Analyzer
{
    // A descriptive observation about one header; it carries no judgement about security.
    // Findings describe present values; Recommendation is only set for a missing header.
    public record HeaderObservation(string Name, bool IsPresent, IReadOnlyList<string> Values, string Purpose, string Limitation)
    {
        public IReadOnlyList<HeaderFinding> FindingDetails { get; init; } = Array.Empty<HeaderFinding>();

        public IReadOnlyList<string> Findings => FindingDetails.Select(f => f.Text).ToArray();

        public bool NeedsAttention => FindingDetails.Any(f => f.NeedsAttention);

        public string? Recommendation { get; init; }
    }
}
