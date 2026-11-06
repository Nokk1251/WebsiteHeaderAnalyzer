namespace Analyzer
{
    // Describes one checked header: what it is for and what observing it does NOT guarantee.
    public record SecurityHeaderDefinition(string Name, string Purpose, string Limitation);
}
