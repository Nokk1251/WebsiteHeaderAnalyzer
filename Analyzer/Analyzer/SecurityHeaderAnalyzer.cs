namespace Analyzer
{
    public class SecurityHeaderAnalyzer
    {
        public static IReadOnlyList<SecurityHeaderDefinition> Definitions { get; } = new[]
        {
            new SecurityHeaderDefinition(
                "Strict-Transport-Security",
                "Tells browsers to use HTTPS only when connecting to this site for a period of time.",
                "It is only honored by browsers over HTTPS and does not show that the rest of the site is configured securely."),
            new SecurityHeaderDefinition(
                "Content-Security-Policy",
                "Lets a site restrict which sources of scripts, styles and other content a browser may load.",
                "Its presence does not show that the policy is strict enough; a permissive policy gives little protection."),
            new SecurityHeaderDefinition(
                "X-Content-Type-Options",
                "Asks browsers not to guess a response's content type and to use the declared one.",
                "It only affects content-type sniffing and does not protect against other kinds of issues."),
            new SecurityHeaderDefinition(
                "X-Frame-Options",
                "Controls whether the page may be displayed inside a frame on another site.",
                "It only covers framing; newer sites may use the Content-Security-Policy frame-ancestors directive instead."),
            new SecurityHeaderDefinition(
                "Referrer-Policy",
                "Controls how much of the page address is sent to other sites in the Referer header.",
                "It only limits referrer information and does not affect other data the site shares."),
            new SecurityHeaderDefinition(
                "Permissions-Policy",
                "Lets a site allow or block browser features such as camera, microphone or geolocation.",
                "Its presence does not show which features are actually in use or that the chosen settings are appropriate.")
        };

        public static IReadOnlyList<string> CheckedHeaders { get; } = Definitions.Select(d => d.Name).ToArray();

        public IReadOnlyList<HeaderObservation> Analyze(IReadOnlyDictionary<string, string[]> headers)
        {
            List<HeaderObservation> observations = new List<HeaderObservation>();

            foreach (SecurityHeaderDefinition definition in Definitions)
            {
                // Lookup is done here so it stays case-insensitive whatever comparer the input dictionary uses.
                string[] values = headers
                    .Where(h => string.Equals(h.Key, definition.Name, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(h => h.Value)
                    .ToArray();

                bool isPresent = headers.Keys.Any(k => string.Equals(k, definition.Name, StringComparison.OrdinalIgnoreCase));

                observations.Add(new HeaderObservation(definition.Name, isPresent, values, definition.Purpose, definition.Limitation)
                {
                    Findings = isPresent ? HeaderEvaluator.EvaluateValues(definition.Name, values) : Array.Empty<string>(),
                    Recommendation = isPresent ? null : HeaderEvaluator.Recommend(definition.Name)
                });
            }

            return observations;
        }
    }
}
