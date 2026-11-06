namespace Analyzer
{
    // A descriptive observation about one header; it carries no judgement about security.
    public record HeaderObservation(string Name, bool IsPresent, IReadOnlyList<string> Values);
}
