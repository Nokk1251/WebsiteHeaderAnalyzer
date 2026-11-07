using System.Text;

namespace Analyzer
{
    // Pure formatting: builds the report text only, so it can be unit tested without a console.
    public static class ConsoleReport
    {
        public const string Disclaimer =
            "Note: this is a passive check of a single response. It is not a security audit, " +
            "and the presence of a header does not prove that a website is secure.";

        public static string Build(HttpAnalysisResult result, IReadOnlyList<HeaderObservation> observations)
        {
            StringBuilder report = new StringBuilder();

            report.AppendLine("=== Website analysis report ===");
            report.AppendLine($"Initial URL: {ConsoleText.Sanitize(result.InitialUri.ToString())}");

            if (!result.Success || result.FinalUri is null || result.StatusCode is null)
            {
                report.AppendLine($"Analysis failed: {ConsoleText.Sanitize(result.ErrorMessage ?? "An unknown error occurred.")}");
                report.AppendLine();
                report.AppendLine(Disclaimer);
                return report.ToString();
            }

            report.AppendLine($"Final URL: {ConsoleText.Sanitize(result.FinalUri.ToString())}");
            report.AppendLine($"Status code: {(int)result.StatusCode} {result.StatusCode}");
            report.AppendLine($"Redirects followed: {result.RedirectCount}");
            report.AppendLine($"HTTPS (final URL): {DescribeHttps(result)}");
            report.AppendLine();
            report.AppendLine("Security headers (observations):");

            foreach (HeaderObservation observation in observations)
            {
                report.AppendLine(observation.IsPresent
                    ? $"  [present] {ConsoleText.Sanitize(observation.Name)}: {FormatValues(observation.Values)}"
                    : $"  [missing] {ConsoleText.Sanitize(observation.Name)}");
                report.AppendLine($"      Purpose: {observation.Purpose}");
                report.AppendLine($"      Limitation: {observation.Limitation}");

                foreach (string finding in observation.Findings)
                {
                    report.AppendLine($"      Finding: {ConsoleText.Sanitize(finding)}");
                }

                if (observation.Recommendation is not null)
                {
                    report.AppendLine($"      Recommendation: {observation.Recommendation}");
                }
            }

            report.AppendLine();
            report.AppendLine(CoverageSummary(observations));
            report.AppendLine();
            report.AppendLine(Disclaimer);
            return report.ToString();
        }

        // A count of what was observed, not a verdict about the site.
        public static string CoverageSummary(IReadOnlyList<HeaderObservation> observations) =>
            $"Coverage: {observations.Count(o => o.IsPresent)} of {observations.Count} checked headers are present.";

        public static string DescribeHttps(HttpAnalysisResult result)
        {
            if (result.HttpsDowngraded)
            {
                return "No - the request started on HTTPS but ended on plain HTTP";
            }

            return result.UsesHttps == true ? "Yes" : "No - the final URL uses plain HTTP";
        }

        private static string FormatValues(IReadOnlyList<string> values) =>
            values.Count == 0 ? "(empty value)" : ConsoleText.Sanitize(string.Join(" | ", values));
    }
}
