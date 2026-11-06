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
            report.AppendLine($"Initial URL: {result.InitialUri}");

            if (!result.Success || result.FinalUri is null || result.StatusCode is null)
            {
                report.AppendLine($"Analysis failed: {result.ErrorMessage ?? "An unknown error occurred."}");
                report.AppendLine();
                report.AppendLine(Disclaimer);
                return report.ToString();
            }

            report.AppendLine($"Final URL: {result.FinalUri}");
            report.AppendLine($"Status code: {(int)result.StatusCode} {result.StatusCode}");
            report.AppendLine($"Redirects followed: {result.RedirectCount}");
            report.AppendLine($"HTTPS (final URL): {DescribeHttps(result)}");
            report.AppendLine();
            report.AppendLine("Security headers (observations):");

            foreach (HeaderObservation observation in observations)
            {
                report.AppendLine(observation.IsPresent
                    ? $"  [present] {observation.Name}: {FormatValues(observation.Values)}"
                    : $"  [missing] {observation.Name}");
            }

            report.AppendLine();
            report.AppendLine(Disclaimer);
            return report.ToString();
        }

        private static string DescribeHttps(HttpAnalysisResult result)
        {
            if (result.HttpsDowngraded)
            {
                return "No - the request started on HTTPS but ended on plain HTTP";
            }

            return result.UsesHttps == true ? "Yes" : "No - the final URL uses plain HTTP";
        }

        private static string FormatValues(IReadOnlyList<string> values) =>
            values.Count == 0 ? "(empty value)" : string.Join(" | ", values);
    }
}
