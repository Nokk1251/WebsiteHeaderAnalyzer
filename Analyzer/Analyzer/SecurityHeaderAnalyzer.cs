namespace Analyzer
{
    public class SecurityHeaderAnalyzer
    {
        public static IReadOnlyList<string> CheckedHeaders { get; } = new[]
        {
            "Strict-Transport-Security",
            "Content-Security-Policy",
            "X-Content-Type-Options",
            "X-Frame-Options",
            "Referrer-Policy",
            "Permissions-Policy"
        };

        public IReadOnlyList<HeaderObservation> Analyze(IReadOnlyDictionary<string, string[]> headers)
        {
            List<HeaderObservation> observations = new List<HeaderObservation>();

            foreach (string name in CheckedHeaders)
            {
                // Lookup is done here so it stays case-insensitive whatever comparer the input dictionary uses.
                string[] values = headers
                    .Where(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(h => h.Value)
                    .ToArray();

                bool isPresent = headers.Keys.Any(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));

                observations.Add(new HeaderObservation(name, isPresent, values));
            }

            return observations;
        }
    }
}
